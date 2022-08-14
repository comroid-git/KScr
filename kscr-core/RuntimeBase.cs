using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Store;
using KScr.Core.Util;
using String = KScr.Core.Std.String;

namespace KScr.Core;

public enum CompressionType
{
    None,
    GZip,
    ZLib
}

public enum State : uint
{
    Normal = 0,
    Return = 1,
    Throw = 2
}

public abstract class RuntimeBase : IBytecodePort
{
    public const string SourceFileExt = ".kscr";
    public const string BinaryFileExt = ".kbin";
    public const string ModuleFileExt = ".kmod";
    public static Encoding Encoding = Encoding.ASCII;

    public static readonly SourcefilePosition
        SystemSrcPos = new() { SourcefilePath = "<native>" };

    public static readonly DirectoryInfo SdkHome = GetSdkHome();
    public static readonly Stack MainStack = new();
    public static readonly Assembly Assembly;
    public static bool Initialized;

    private uint _lastObjId = 0xF;

    static RuntimeBase()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        Assembly = typeof(RuntimeBase).Assembly;
    }

    public abstract ObjectStore ObjectStore { get; }
    public abstract ClassStore ClassStore { get; }
    public abstract INativeRunner? NativeRunner { get; }
    public abstract IDictionary<BytecodeVersion, IBytecodePort> BytecodePorts { get; }

    public IObjectRef? this[Stack stack, VariableContext varctx, string name]
    {
        get => ObjectStore[stack.KeyGen, varctx, name];
        set => ObjectStore[stack.KeyGen, varctx, name] = value as ObjectRef;
    }

    public IObjectRef ConstantVoid =>
        ComputeObject(MainStack, VariableContext.Absolute, IObject.Null.GetKey(), () => IObject.Null);

    public IObjectRef ConstantFalse =>
        ComputeObject(MainStack, VariableContext.Absolute, Numeric.Zero.GetKey(), () => Numeric.Zero);

    public IObjectRef ConstantTrue =>
        ComputeObject(MainStack, VariableContext.Absolute, Numeric.One.GetKey(), () => Numeric.One);

    public ObjectRef StdioRef { get; private set; }

    public bool StdIoMode { get; set; } = false;
    public static bool ConfirmExit { get; set; }
    public static bool DebugMode { get; set; }
    public static string[] ExtraArgs { get; set; }
    public static bool CompileSystem { get; set; }
    public CompressionType CompressionType { get; set; } = CompressionType.None;
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;
    public static int ExitCode { get; set; } = 0;
    public static string? ExitMessage { get; set; } = null;
    public BytecodeVersion BytecodeVersion => BytecodeVersion.Current;
    public readonly List<CompilerException> CompilerErrors = new();

    public void Write(StringCache strings, Stream stream, IBytecode bytecode)
    {
        stream.Write(BitConverter.GetBytes(BytecodeVersion.Version.Major));
        stream.Write(BitConverter.GetBytes(BytecodeVersion.Version.Minor));
        BytecodePorts[BytecodeVersion].Write(strings, stream, bytecode);
    }

    public T Load<T>(RuntimeBase vm, StringCache strings, Stream stream, Package pkg, Class? cls)
    {
        byte[] buf;
        stream.Read(buf = new byte[4], 0, 4);
        var maj = BitConverter.ToInt32(buf);
        stream.Read(buf = new byte[4], 0, 4);
        var min = BitConverter.ToInt32(buf);
        var ver = BytecodeVersion.Find(maj, min, $"Incompatible BytecodeVersion: {maj}.{min:2}");
        return BytecodePorts[ver].Load<T>(vm, strings, stream, pkg, cls);
    }

    public Stream WrapStream(Stream stream, CompressionMode mode, CompressionLevel? level = null)
    {
        return (CompressionType, mode) switch
        {
            (CompressionType.None, _) => stream,
            (CompressionType.GZip, _) => new GZipStream(stream, mode),
            (CompressionType.ZLib, CompressionMode.Compress) => new ZLibStream(stream, level ?? CompressionLevel),
            (CompressionType.ZLib, CompressionMode.Decompress) => new ZLibStream(stream, CompressionMode.Decompress),
            _ => throw new ArgumentOutOfRangeException(nameof(CompressionType), CompressionType,
                "Invalid CompressionType")
        };
    }

    public void Initialize()
    {
        if (Initialized) return;
        
        Numeric.Zero = new(this, true)
        {
            Bytes = BitConverter.GetBytes((byte)0),
            Mode = NumericMode.Byte
        };
        Numeric.One = new(this, true)
        {
            Bytes = BitConverter.GetBytes((byte)1),
            Mode = NumericMode.Byte
        };
        
        Class.NumericByteType = new Class.Instance(this, Class.NumericType,
            new Class(Class.LibCorePackage, "byte", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native));
        Class.NumericShortType = new Class.Instance(this, Class.NumericType,
            new Class(Class.LibCorePackage, "short", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native));
        Class.NumericIntType = new Class.Instance(this, Class.NumericType, Class.IntType);
        Class.NumericLongType = new Class.Instance(this, Class.NumericType,
            new Class(Class.LibCorePackage, "long", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native));
        Class.NumericFloatType = new Class.Instance(this, Class.NumericType,
            new Class(Class.LibCorePackage, "float", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native));
        Class.NumericDoubleType = new Class.Instance(this, Class.NumericType,
            new Class(Class.LibCorePackage, "double", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native));
        
        Class.VoidType.Initialize(this);
        Class.ObjectType.Initialize(this);
        Class.TypeType.Initialize(this);
        Class.EnumType.Initialize(this);
        Class.PipeType.Initialize(this);
        Class.ArrayType.Initialize(this);
        Class.TupleType.Initialize(this);
        Class.StringType.Initialize(this);
        Class.RangeType.Initialize(this);
        Class.IterableType.Initialize(this);
        Class.IteratorType.Initialize(this);
        Class.ThrowableType.Initialize(this);
        Class.NumericType.Initialize(this);
        Class.NumericByteType.Initialize(this);
        Class.NumericShortType.Initialize(this);
        Class.IntType.Initialize(this);
        Class.NumericIntType.Initialize(this);
        Class.NumericLongType.Initialize(this);
        Class.NumericFloatType.Initialize(this);
        Class.NumericDoubleType.Initialize(this);

        Class.InitializePrimitives(this);

        Class.VoidType.LateInitialize(this, MainStack);
        Class.ObjectType.LateInitialize(this, MainStack);
        Class.TypeType.LateInitialize(this, MainStack);
        Class.EnumType.LateInitialize(this, MainStack);
        Class.PipeType.LateInitialize(this, MainStack);
        Class.ArrayType.LateInitialize(this, MainStack);
        Class.TupleType.LateInitialize(this, MainStack);
        Class.StringType.LateInitialize(this, MainStack);
        Class.RangeType.LateInitialize(this, MainStack);
        Class.IterableType.LateInitialize(this, MainStack);
        Class.IteratorType.LateInitialize(this, MainStack);
        Class.ThrowableType.LateInitialize(this, MainStack);
        Class.IntType.LateInitialize(this, MainStack);
        Class.NumericType.LateInitialize(this, MainStack);

        StdioRef = new StandardIORef(this);

        Initialized = true;
    }

    public uint NextObjId()
    {
        return ++_lastObjId;
    }

    public long NextObjId(string name)
    {
        return CombineHash(NextObjId(), name);
    }

    public static long UnixTime()
    {
        var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (DateTime.UtcNow - epochStart).Ticks / 10;
    }

    private static DirectoryInfo GetSdkHome()
    {
        return Environment.GetEnvironmentVariable("PATH")!
            .Split(Path.PathSeparator)
            .Select(path => new DirectoryInfo(path))
            .Where(dir => dir.Exists)
            .FirstOrDefault(dir => dir.EnumerateFiles("*.exe")
                .Any(f => f.Name == "kscr.exe")) ?? throw new System.Exception("KScr Home not found in PATH");
    }

    public void Clear()
    {
        ObjectStore.Clear();
    }

    public IObjectRef ComputeObject(Stack stack, VariableContext varctx, string key, Func<IObject> func)
    {
        return this[stack, varctx, key] ?? PutObject(stack, varctx, func());
    }

    public IObjectRef PutLocal(Stack stack, string name, IObject? value)
    {
        return PutObject(stack, VariableContext.Local, value ?? IObject.Null, name);
    }

    public IObjectRef PutObject(Stack stack, VariableContext varctx, IObject value, string? key = null)
    {
        return this[stack, varctx, key ?? value.GetKey()] =
            new ObjectRef(value.Type == null && !Initialized ? value as IClassInstance : value.Type, value);
    }

    public Stack Execute()
    {
        var method = Package.RootPackage.FindEntrypoint();
        var stack = MainStack;

        try
        {
            stack = method.Invoke(this, stack,
                args: ExtraArgs.Select(str => String.Instance(this, str)[this, stack, 0]).ToArray());
        }
        catch (StackTraceException stc)
        {
            stc.PrintStackTrace();
        }

        return stack;
    }

    public IClassInstance? FindType(string name, Package? package = null, IClass? owner = null)
    {
        if (name.EndsWith("object"))
            return Class.ObjectType.DefaultInstance;
        if (name.EndsWith("type"))
            return Class.TypeType.DefaultInstance;
        if (name.EndsWith("enum"))
            return Class.EnumType.DefaultInstance;
        if (name.EndsWith("array"))
            return Class.ArrayType.DefaultInstance;
        if (name.EndsWith("tuple"))
            return Class.TupleType.DefaultInstance;
        if (name.EndsWith("range"))
            return Class.RangeType.DefaultInstance;
        if (name.EndsWith("pipe"))
            return Class.PipeType.DefaultInstance;
        if (name == "num")
            return Class.NumericType.DefaultInstance;
        if (name.Contains("num"))
            if (name.EndsWith("byte>"))
                return Class.NumericByteType;
            else if (name.EndsWith("short>"))
                return Class.NumericShortType;
            else if (name.EndsWith("int>"))
                return Class.NumericIntType;
            else if (name.EndsWith("long>"))
                return Class.NumericLongType;
            else if (name.EndsWith("float>"))
                return Class.NumericFloatType;
            else if (name.EndsWith("double>"))
                return Class.NumericDoubleType;
            else return Class.NumericType.DefaultInstance;
        if (name == "byte")
            return Class.NumericByteType;
        if (name == "short")
            return Class.NumericShortType;
        if (name == "int")
            return Class.NumericIntType;
        if (name == "long")
            return Class.NumericLongType;
        if (name == "float")
            return Class.NumericFloatType;
        if (name == "double")
            return Class.NumericDoubleType;
        if (name.EndsWith("str"))
            return Class.StringType.DefaultInstance;
        if (name.EndsWith("void") || name.EndsWith("object") || name == "object")
            return Class.VoidType.DefaultInstance;

        if (name.Contains('<'))
        {
            // create instance
            var canonicalName = name.Substring(0, name.IndexOf('<'));
            var kls = ClassStore.FindType(this,
                canonicalName.Contains('.') ? package ?? Package.RootPackage : Package.RootPackage, canonicalName);
            var tParams = new List<TypeParameter>();
            kls = ClassStore.FindType(this,
                canonicalName.Contains('.') ? package ?? Package.RootPackage : Package.RootPackage, canonicalName);
            var split = name.Substring(name.IndexOf('<') + 1, name.IndexOf('>') - name.IndexOf('<') - 1).Split(", ");
            for (var i = 0; i < split.Length; i++)
                tParams.Add(new TypeParameter(split[i]));
            return kls!.CreateInstance(this, owner as Class, tParams.Cast<ITypeInfo>().ToArray());
        }

        return ClassStore.FindType(this, package ?? Package.RootPackage, name)?.DefaultInstance;
    }

    public ITypeInfo FindTypeInfo(string identifier, Class inClass, Package inPackage)
    {
        return inClass.TypeParameters.FirstOrDefault(tp => tp.FullName == identifier)
            .ResolveType(inClass.DefaultInstance) ?? FindType(identifier, inPackage)!;
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

    public sealed class StandardIORef : ObjectRef
    {
        public StandardIORef(RuntimeBase vm) : base(Class.PipeType.CreateInstance(vm, Class.PipeType, Class.StringType))
        {
        }

        public override IEvaluable? ReadAccessor
        {
            get => new StdioReader();
            set => throw new FatalException("Cannot reassign stdio ReadAccessor");
        }

        public override IEvaluable? WriteAccessor
        {
            get => new StdioWriter();
            set => throw new FatalException("Cannot reassign stdio WriteAccessor");
        }

        private sealed class StdioWriter : IEvaluable
        {
            public Stack Evaluate(RuntimeBase vm, Stack stack)
            {
                var txt = stack.Alp!.Value.ToString(IObject.ToString_ParseableName);
                Console.Write(txt);
                return stack;
            }
        }

        private sealed class StdioReader : IEvaluable
        {
            public Stack Evaluate(RuntimeBase vm, Stack stack)
            {
                if (stack.Alp!.Length != 1 || !stack.Alp.Type.CanHold(Class.StringType) &&
                    !stack.Alp.Type.CanHold(Class.NumericType))
                    throw new FatalException("Invalid reference to write string into: " + stack.Alp);
                var txt = Console.ReadLine()!;
                if (stack.Alp.Type.CanHold(Class.NumericType))
                    stack.Alp.Value = Numeric.Compile(vm, txt).Value;
                else stack.Alp.Value = String.Instance(vm, txt).Value;
                return stack;
            }
        }
    }
}