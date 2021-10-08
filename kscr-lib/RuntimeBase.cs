using System;
using System.Collections.Generic;
using System.Globalization;
using KScr.Lib.Core;
using KScr.Lib.Model;
using KScr.Lib.Store;

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
        private uint _lastObjId = 0xF;

        static RuntimeBase()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }

        public abstract ObjectStore ObjectStore { get; }
        public abstract TypeStore TypeStore { get; }
        public Context Context { get; internal set; } = new Context();

        public ObjectRef? this[VariableContext varctx, string name]
        {
            get => ObjectStore[Context, varctx, name];
            set => ObjectStore[Context, varctx, name] = value;
        }

        public abstract ITokenizer Tokenizer { get; }
        public abstract ICompiler Compiler { get; }

        public ObjectRef ConstantVoid => ComputeObject(VariableContext.Absolute, Numeric.CreateKey(-1), () => IObject.Null);
        public ObjectRef ConstantFalse => ComputeObject(VariableContext.Absolute, Numeric.CreateKey(0), () => Numeric.Zero);
        public ObjectRef ConstantTrue => ComputeObject(VariableContext.Absolute, Numeric.CreateKey(1), () => Numeric.One);

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
            TypeStore.Clear();
        }

        public ObjectRef ComputeObject(VariableContext varctx, string key, Func<IObject> func)
        {
            return this[varctx, key] ?? PutObject(varctx, key, func());
        }

        public ObjectRef PutObject(VariableContext varctx, string key, IObject? value)
        {
            return this[varctx, key] = new ObjectRef(value?.Type ?? TypeRef.VoidType, value ?? IObject.Null);
        }

        public IList<Token> Tokenize(string source)
        {
            return Tokenizer.Tokenize(source);
        }

        public IEvaluable Compile(IList<Token> tokens)
        {
            return Compiler.Compile(this, tokens);
        }

        public IObject? Execute(IEvaluable bytecode, out State state, out long timeµs)
        {
            timeµs = UnixTime();
            var yield = Execute(bytecode, out state);
            timeµs = UnixTime() - timeµs;
            return yield;
        }

        public IObject? Execute(IEvaluable bytecode, out State state)
        {
            ObjectRef? nil = null;
            state = bytecode.Evaluate(this, null, ref nil);
            return nil?.Value;
        }

        public TypeRef? FindType(string name)
        {
            switch (name)
            {
                case "byte":
                    return TypeRef.NumericByteType;
                case "short":
                    return TypeRef.NumericShortType;
                case "int":
                    return TypeRef.NumericIntegerType;
                case "long":
                    return TypeRef.NumericLongType;
                case "float":
                    return TypeRef.NumericFloatType;
                case "double":
                    return TypeRef.NumericDoubleType;
                case "str":
                    return TypeRef.StringType;
                case "void":
                    return TypeRef.VoidType;
            }

            return null; // todo;
        }
    }
}