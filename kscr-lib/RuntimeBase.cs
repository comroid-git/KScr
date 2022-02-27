﻿using System;
using System.Globalization;
using System.Linq;
using System.Text;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;
using String = KScr.Lib.Core.String;

namespace KScr.Lib
{
    public enum State : uint
    {
        Normal = 0,
        Return = 1,
        Throw = 2
    }

    public abstract class RuntimeBase
    {
        public static Encoding Encoding = Encoding.ASCII;

        public bool Initialized = false;
        private uint _lastObjId = 0xF;

        static RuntimeBase()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }

        public void Initialize()
        {
            if (Initialized) return;
            Class.TypeType.Initialize(this);
            Class.VoidType.Initialize(this);
            Class.ArrayType.Initialize(this);
            Class.StringType.Initialize(this);
            Class.RangeType.Initialize(this);
            Class.NumericType.Initialize(this);
            Class.NumericByteType.Initialize(this);
            Class.NumericShortType.Initialize(this);
            Class.NumericIntType.Initialize(this);
            Class.NumericLongType.Initialize(this);
            Class.NumericFloatType.Initialize(this);
            Class.NumericDoubleType.Initialize(this);
            Initialized = true;
        }

        public abstract ObjectStore ObjectStore { get; }
        public abstract ClassStore ClassStore { get; }
        public Stack Stack { get; } = new();

        public ObjectRef? this[VariableContext varctx, string name]
        {
            get => ObjectStore[Stack, varctx, name];
            set => ObjectStore[Stack, varctx, name] = value;
        }

        public abstract ITokenizer Tokenizer { get; }
        public abstract ICompiler Compiler { get; }

        public ObjectRef ConstantVoid =>
            ComputeObject(VariableContext.Absolute, Numeric.CreateKey(-1), () => IObject.Null);

        public ObjectRef ConstantFalse =>
            ComputeObject(VariableContext.Absolute, Numeric.CreateKey(0), () => Numeric.Zero);

        public ObjectRef ConstantTrue =>
            ComputeObject(VariableContext.Absolute, Numeric.CreateKey(1), () => Numeric.One);

        public ObjectRef StdioRef { get; } = new StandardIORef();

        public bool StdIoMode { get; set; } = false;

        public uint NextObjId()
        {
            return ++_lastObjId;
        }

        public long NextObjId(string name)
        {
            return CombineHash(NextObjId(), name);
        }

        public static long GetHashCode64(string input)
        {
            // inspired by https://stackoverflow.com/questions/8820399/c-sharp-4-0-how-to-get-64-bit-hash-code-of-given-string
            return CombineHash((uint)input.Substring(0, input.Length / 2).GetHashCode(),
                input.Substring(input.Length / 2));
        }

        public static long CombineHash(uint objId, string name)
        {
            return CombineHash(objId, name.GetHashCode());
        }

        public static long CombineHash(uint objId, int hash)
        {
            return ((long)hash << 0x20) | objId;
        }

        public static long UnixTime()
        {
            var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (DateTime.UtcNow - epochStart).Ticks / 10;
        }

        public void Clear()
        {
            ObjectStore.Clear();
            ClassStore.Clear();
        }

        public ObjectRef ComputeObject(VariableContext varctx, string key, Func<IObject> func)
        {
            return this[varctx, key] ?? PutObject(varctx, key, func());
        }

        public ObjectRef PutObject(VariableContext varctx, string key, IObject? value)
        {
            return this[varctx, key] = new ObjectRef(value?.Type ?? Class.VoidType.DefaultInstance, value ?? IObject.Null);
        }

        public IObject? Execute(ref State state, out long timeµs)
        {
            timeµs = UnixTime();
            var yield = Execute(ref state);
            timeµs = UnixTime() - timeµs;
            return yield;
        }

        public IObject? Execute(ref State state)
        {
            var site = Package.RootPackage.FindEntrypoint();
            ObjectRef? rev = null;

            while (site != null)
                site = site.Evaluate(this, ref state, ref rev);

            return rev?.Value;
        }

        public IClassInstance FindType(RuntimeBase vm, string name, Package? package = null)
        {
            switch (name)
            {
                case "num":
                    return Class.NumericType.DefaultInstance;
                case "byte":
                    return Class.NumericByteType;
                case "short":
                    return Class.NumericShortType;
                case "int":
                    return Class.NumericIntType;
                case "long":
                    return Class.NumericLongType;
                case "float":
                    return Class.NumericFloatType;
                case "double":
                    return Class.NumericDoubleType;
                case "str":
                    return Class.StringType.DefaultInstance;
                case "void":
                    return Class.VoidType.DefaultInstance;
            }

            return ClassStore.FindType(vm, package!, name);
        }

        public ITypeInfo FindTypeInfo(RuntimeBase vm, string identifier, Class inClass, Package inPackage)
        {
            return (ITypeInfo?)inClass.TypeParameters.FirstOrDefault(tp => tp.FullName == identifier)
                   ?? FindType(vm, identifier, inPackage)!;
        }

        public sealed class StandardIORef : ObjectRef
        {
            public StandardIORef() : base(Class.StringType.DefaultInstance)
            {
            }

            public override IEvaluable? ReadAccessor
            {
                get => new StdioReader();
                set => throw new InternalException("Cannot reassign stdio ReadAccessor");
            }

            public override IEvaluable? WriteAccessor
            {
                get => new StdioWriter();
                set => throw new InternalException("Cannot reassign stdio WriteAccessor");
            }

            private sealed class StdioWriter : IEvaluable
            {
                public State Evaluate(RuntimeBase vm, IEvaluable? prev, ref ObjectRef rev)
                {
                    var txt = rev.Value!.ToString(IObject.ToString_ShortName);
                    Console.WriteLine(txt);
                    return State.Normal;
                }
            }

            private sealed class StdioReader : IEvaluable
            {
                public State Evaluate(RuntimeBase vm, IEvaluable? prev, ref ObjectRef rev)
                {
                    if (rev.Length != 1 || !rev.Type.CanHold(Class.StringType) && !rev.Type.CanHold(Class.NumericType))
                        throw new InternalException("Invalid reference to write string into: " + rev);
                    string txt = Console.ReadLine()!;
                    if (rev.Type.CanHold(Class.NumericType))
                        rev.Value = Numeric.Compile(vm, txt).Value;
                    else rev.Value = String.Instance(vm, txt).Value;
                    return State.Normal;
                }
            }
        }
    }
}