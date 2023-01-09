using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Std;

public sealed class Class : AbstractPackageMember, IClass
{
    public static readonly Package LibRootPackage = Package.RootPackage.GetOrCreatePackage("org")
        .GetOrCreatePackage("comroid").GetOrCreatePackage("kscr");

    public static readonly Package LibCorePackage = LibRootPackage.GetOrCreatePackage("core");
    public static readonly Package LibErrorPackage = LibRootPackage.GetOrCreatePackage("error");

    public static readonly Class VoidType;
    public static readonly Class TypeType;
    public static readonly Class ObjectType;
    public static readonly Class EnumType;
    public static readonly Class PipeType;
    public static readonly Class ArrayType;
    public static readonly Class TupleType;
    public static readonly Class StringType;
    public static readonly Class NumericType;
    public static readonly Class RangeType;
    public static readonly Class Sequence;
    public static readonly Class Sequencable;
    public static readonly Class CloseableType;
    public static readonly Class ThrowableType;
    public static readonly Class ExceptionType;
    public static readonly Class NullPointerExceptionType;
    public static readonly Class IntType;
    public static Instance NumericByteType;
    public static Instance NumericShortType;
    public static Instance NumericIntType;
    public static Instance NumericLongType;
    public static Instance NumericFloatType;
    public static Instance NumericDoubleType;

    public readonly IList<IClassInstance> DeclaredInterfaces = new List<IClassInstance>();
    public readonly IList<IClassInstance> DeclaredSuperclasses = new List<IClassInstance>();

    private bool _initialized;
    internal bool _lateInitialized;

    static Class()
    {
        VoidType = new Class(LibCorePackage, "void", true, MemberModifier.Public, ClassType.Interface);
        TypeType = new Class(LibCorePackage, "type", true, MemberModifier.Public | MemberModifier.Final);
        ObjectType = new Class(LibCorePackage, "object", true, MemberModifier.Public | MemberModifier.Native);
        EnumType = new Class(LibCorePackage, "enum", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
            { TypeParameters = { new TypeParameter("T") } };
        PipeType = new Class(LibCorePackage, "pipe", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native,
                ClassType.Interface)
            { TypeParameters = { new TypeParameter("T") } };
        ArrayType = new Class(LibCorePackage, "array", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
            { TypeParameters = { new TypeParameter("T") } };
        TupleType = new Class(LibCorePackage, "tuple", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
            { TypeParameters = { new TypeParameter("T", TypeParameterSpecializationType.List) } };
        StringType = new Class(LibCorePackage, "str", true,
            MemberModifier.Public | MemberModifier.Final | MemberModifier.Native);
        NumericType = new Class(LibCorePackage, "num", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
            { TypeParameters = { new TypeParameter("T") } };
        RangeType = new Class(LibCorePackage, "range", true,
            MemberModifier.Public | MemberModifier.Final | MemberModifier.Native);
        Sequence = new Class(LibCorePackage, "Sequence", false, MemberModifier.Public, ClassType.Interface)
            { TypeParameters = { new TypeParameter("T") } };
        Sequencable = new Class(LibCorePackage, "Sequencable", false, MemberModifier.Public, ClassType.Interface)
            { TypeParameters = { new TypeParameter("T") } };
        ThrowableType = new Class(LibCorePackage, "Throwable", false, MemberModifier.Public, ClassType.Interface);
        ExceptionType = new Class(LibErrorPackage, "Exception", false, MemberModifier.Public);
        NullPointerExceptionType = new Class(LibErrorPackage, "NullPointerException", false, MemberModifier.Public);
        CloseableType = new Class(LibCorePackage, "Closeable", false, MemberModifier.Public, ClassType.Interface);
        IntType = new Class(LibCorePackage, "int", true,
            MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
        {
            TypeParameters =
            {
                new TypeParameter("n", TypeParameterSpecializationType.N)
                    { DefaultValue = new TypeInfo { Name = "32" } }
            }
        };
    }

    public Class(Package package, string name, bool primitive, MemberModifier modifier = MemberModifier.Protected,
        ClassType type = ClassType.Class) : base(package, name, modifier)
    {
        Primitive = primitive;
        ClassType = type;
    }

    public override IEnumerable<IBytecode> Header => new IBytecode[]
    {
        IBytecode.Byte((byte)MemberType),
        IBytecode.UInt((uint)Modifier),
        IBytecode.String(Name)
    };

    public IList<string> Imports { get; } =
        new List<string>();

    public TypeParameter.Instance[] TypeParameterInstances { get; } = Array.Empty<TypeParameter.Instance>();

    public Class? Parent { get; init; } = null;
    public ClassMemberType MemberType => ClassMemberType.Class;
    public StatementComponent CatchFinally { get; set; }
    public SourcefilePosition SourceLocation { get; init; }

    public BytecodeElementType ElementType => BytecodeElementType.Class;

    public Instance DefaultInstance { get; private set; } = null!;

    public ClassRef SelfRef => DefaultInstance.SelfRef;


    public IDictionary<string, IClassMember> DeclaredMembers { get; } =
        new ConcurrentDictionary<string, IClassMember>();

    public IEnumerable<IClassInstance> Superclasses => DeclaredSuperclasses.SelectMany(ExpandSuperclasses);

    public IEnumerable<IClassInstance> Interfaces =>
        DeclaredInterfaces.Concat(Superclasses.SelectMany(x => x.Interfaces)).SelectMany(ExpandInterfaces);

    public ClassType ClassType { get; }

    public Instance GetInstance(RuntimeBase vm, params ITypeInfo[] typeParameters)
    {
        return CreateInstance(vm, null, typeParameters);
    }

    public Class BaseClass => this;
    public List<TypeParameter> TypeParameters { get; } = new();

    public string CanonicalName => FullName;

    public string FullDetailedName => FullName + (TypeParameters.Count == 0
        ? string.Empty
        : '<' + string.Join(", ", TypeParameters.Select(x => x.FullDetailedName)) + '>');

    public string DetailedName => Name + (TypeParameters.Count == 0
        ? string.Empty
        : '<' + string.Join(", ", TypeParameters) + '>');

    public bool CanHold(IClass? type)
    {
        return Name == "void"
               || type?.BaseClass.Name == "void"
               || (Name == "int" && (type?.DetailedName.StartsWith("num") ?? false))
               || type?.BaseClass == BaseClass
               || ((type?.BaseClass as IClass)?.Inheritors
                   .Where(x => x != null)
                   .Select(x => x.BaseClass)
                   .Any(super => super.FullName == FullName) ?? true);
    }

    public bool Primitive { get; }

    public Instance CreateInstance(RuntimeBase vm, Class? owner = null, params ITypeInfo[] typeParameters)
    {
        if (typeParameters.Length != TypeParameters.Count)
            throw new ArgumentException("Invalid typeParameter count");
        var instance = new Instance(vm, this, owner, typeParameters);
        instance.Initialize(vm);
        return instance;
    }

    public Stack Invoke(RuntimeBase vm, Stack stack, IObject? target = null, StackOutput maintain = StackOutput.Omg,
        params IObject?[] args)
    {
        throw new NotSupportedException("Cannot invoke class");
    }

    public Stack Evaluate(RuntimeBase vm, Stack stack)
    {
        var cctor = DeclaredMembers.Values.FirstOrDefault(x => x.Name == Method.StaticInitializerName) as IMethod;
        if (cctor == null)
            return stack;
        cctor.Invoke(vm, stack);
        return stack;
    }

    private IEnumerable<IClassInstance> ExpandSuperclasses(IClassInstance arg)
    {
        return arg == null ? Enumerable.Empty<IClassInstance>() : new[] { arg }.Concat(arg.Superclasses);
    }

    private IEnumerable<IClassInstance> ExpandInterfaces(IClassInstance arg)
    {
        return arg == null ? Enumerable.Empty<IClassInstance>() : new[] { arg }.Concat(arg.Interfaces);
    }

    public void Initialize(RuntimeBase vm)
    {
        if (_initialized) return;
        if (!Primitive)
            switch (ClassType)
            {
                case ClassType.Class:
                    DeclaredSuperclasses.Add(ObjectType.DefaultInstance);
                    break;
                case ClassType.Enum:
                    DeclaredSuperclasses.Add(EnumType.DefaultInstance);
                    break;
            }
        else if (Name == "void") ;
        else if (Name == "object") DeclaredInterfaces.Add(VoidType.DefaultInstance);
        else if (Name != "object") DeclaredSuperclasses.Add(ObjectType.DefaultInstance);
        
        DefaultInstance = GetInstance(vm, TypeParameters
            .Cast<ITypeParameter?>()
            .Select(tp => tp?.SpecializationTarget)
            .Where(it => it != null)
            // ReSharper disable once SuspiciousTypeConversion.Global
            .Cast<ITypeInfo>()
            .ToArray());
        _initialized = true;
    }

    public void LateInitialize(RuntimeBase vm, Stack stack)
    {
        if (_lateInitialized) return;
        Evaluate(vm, stack);
        _lateInitialized = true;
    }

    public static IClassInstance _NumericType(NumericMode mode)
    {
        return mode switch
        {
            NumericMode.Byte => NumericByteType,
            NumericMode.Short => NumericShortType,
            NumericMode.Int => NumericIntType,
            NumericMode.Long => NumericLongType,
            NumericMode.Float => NumericFloatType,
            NumericMode.Double => NumericDoubleType,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public override string ToString()
    {
        return FullName;
    }

    public static void InitializePrimitives(RuntimeBase vm)
    {
        #region Void Class

        var toString = new DummyMethod(
            ObjectType,
            "toString",
            MemberModifier.Public,
            StringType,
            new List<MethodParameter>
            {
                new()
                {
                    Name = "variant",
                    Type = NumericShortType
                }
            });
        var equals = new DummyMethod(
            ObjectType,
            "equals",
            MemberModifier.Public,
            NumericByteType,
            new List<MethodParameter>
            {
                new()
                {
                    Name = "other",
                    Type = ObjectType.DefaultInstance
                }
            });
        var getType = new DummyMethod(ObjectType, "getType", MemberModifier.Public | MemberModifier.Final, TypeType);

        AddToClass(VoidType, toString);
        AddToClass(VoidType, equals);
        AddToClass(VoidType, getType);

        #endregion

        #region Object Class

        ObjectType.DeclaredInterfaces.Add(VoidType.DefaultInstance);

        #endregion

        #region Type Class

        TypeType.DeclaredSuperclasses.Add(ObjectType.DefaultInstance);

        #endregion

        #region Enum Class

        var name = new Property(RuntimeBase.SystemSrcPos, EnumType, "name", StringType, MemberModifier.Public);
        var values = new DummyMethod(
            EnumType,
            "values",
            MemberModifier.Public | MemberModifier.Static,
            ArrayType.CreateInstance(vm, EnumType, EnumType.TypeParameters[0]));

        AddToClass(EnumType, name);
        AddToClass(EnumType, values);
        EnumType.DeclaredSuperclasses.Add(ObjectType.DefaultInstance);

        #endregion

        #region Pipe Class

        var read = new DummyMethod(
            PipeType,
            "read",
            MemberModifier.Public,
            PipeType.TypeParameters[0],
            new List<MethodParameter> { new() { Name = "length", Type = NumericIntType } });
        var write = new DummyMethod(
            PipeType,
            "write",
            MemberModifier.Public,
            NumericIntType,
            new List<MethodParameter> { new() { Name = "data", Type = PipeType.TypeParameters[0] } });

        AddToClass(PipeType, name);
        AddToClass(PipeType, values);
        PipeType.DeclaredInterfaces.Add(VoidType.DefaultInstance);

        #endregion

        #region Array Class

        //var length = new Property(RuntimeBase.SystemSrcPos, ArrayType, "length", NumericIntType, MemberModifier.Public);
        var length = new DummyMethod(ArrayType, "length", MemberModifier.Public, NumericIntType);

        AddToClass(ArrayType, length);
        ArrayType.DeclaredSuperclasses.Add(ObjectType.DefaultInstance);

        #endregion

        #region Tuple Class

        var size = new Property(RuntimeBase.SystemSrcPos, TupleType, "size", NumericIntType, MemberModifier.Public);

        AddToClass(TupleType, size);
        TupleType.DeclaredSuperclasses.Add(ObjectType.DefaultInstance);

        #endregion

        #region Numeric Class

        NumericType.DeclaredInterfaces.Add(ThrowableType.DefaultInstance);

        #endregion

        #region String Class

        var strlen = new DummyMethod(StringType, "length", MemberModifier.Public | MemberModifier.Final,
            NumericIntType);

        AddToClass(StringType, strlen);
        StringType.DeclaredSuperclasses.Add(ObjectType.DefaultInstance);

        #endregion

        #region Range Class

        var start = new DummyMethod(RangeType, "start", MemberModifier.Public | MemberModifier.Final,
            NumericIntType);
        var end = new DummyMethod(RangeType, "end", MemberModifier.Public | MemberModifier.Final, NumericIntType);
        var test = new DummyMethod(
            RangeType,
            "test",
            MemberModifier.Public | MemberModifier.Final,
            NumericByteType,
            new List<MethodParameter>
            {
                new()
                {
                    Name = "i",
                    Type = NumericIntType
                }
            });
        var accumulate = new DummyMethod(
            RangeType,
            "accumulate",
            MemberModifier.Public | MemberModifier.Final,
            NumericIntType,
            new List<MethodParameter>
            {
                new()
                {
                    Name = "other",
                    Type = NumericIntType
                }
            });
        var decremental = new DummyMethod(RangeType, "decremental", MemberModifier.Public | MemberModifier.Final,
            NumericByteType);

        // iterable methods
        var sequence = new DummyMethod(Sequencable, "sequence", MemberModifier.Public | MemberModifier.Abstract,
            Sequence.CreateInstance(vm, Sequencable, Sequencable.TypeParameters[0]));

        AddToClass(RangeType, start);
        AddToClass(RangeType, end);
        AddToClass(RangeType, test);
        AddToClass(RangeType, accumulate);
        AddToClass(RangeType, decremental);
        AddToClass(RangeType, sequence);
        RangeType.DeclaredInterfaces.Add(Sequencable.CreateInstance(vm, RangeType, NumericIntType));

        #endregion

        #region Sequence Class

        var current = new DummyMethod(Sequence, "current", MemberModifier.Public | MemberModifier.Abstract,
            Sequence.TypeParameters[0]);
        var next = new DummyMethod(Sequence, "next", MemberModifier.Public | MemberModifier.Abstract,
            Sequence.TypeParameters[0]);
        var hasNext = new DummyMethod(Sequence, "hasNext", MemberModifier.Public | MemberModifier.Abstract,
            NumericByteType);
        var finite = new DummyMethod(Sequence, "finite", MemberModifier.Public, NumericByteType);
        var seqLength = new DummyMethod(Sequence, "length", MemberModifier.Public, NumericIntType);

        AddToClass(Sequence, current);
        AddToClass(Sequence, next);
        AddToClass(Sequence, hasNext);
        AddToClass(Sequence, finite);
        AddToClass(Sequence, seqLength);
        Sequence.DeclaredInterfaces.Add(VoidType.DefaultInstance);

        #endregion

        #region Sequencable Class

        AddToClass(Sequencable, sequence);
        Sequencable.DeclaredInterfaces.Add(VoidType.DefaultInstance);

        #endregion

        #region Closeable Class

        var close = new DummyMethod(CloseableType, "close", MemberModifier.Public, VoidType);

        AddToClass(CloseableType, close);
        CloseableType.DeclaredInterfaces.Add(VoidType.DefaultInstance);

        #endregion

        #region Throwable Class

        var message = new Property(RuntimeBase.SystemSrcPos, ThrowableType, "Message", StringType,
            MemberModifier.Public);
        var exitCode = new Property(RuntimeBase.SystemSrcPos, ThrowableType, "ExitCode", NumericIntType,
            MemberModifier.Public);

        AddToClass(ThrowableType, message);
        AddToClass(ThrowableType, exitCode);
        ExceptionType.DeclaredInterfaces.Add(ThrowableType.DefaultInstance);
        NullPointerExceptionType.DeclaredSuperclasses.Add(ExceptionType.DefaultInstance);

        #endregion
    }

    private static void AddToClass(Class type, IClassMember dummyMethod)
    {
        type.DeclaredMembers[dummyMethod.Name] = dummyMethod;
    }

    public sealed class Instance : NativeObj, IClassInstance
    {
        private readonly Class? _owner;
        private bool _initialized;
#pragma warning disable CS0628
        protected internal Instance(RuntimeBase vm, Class baseClass, params ITypeInfo[] args) : this(vm, baseClass,
            null, args)
#pragma warning restore CS0628
        {
        }
#pragma warning disable CS0628
        protected internal Instance(RuntimeBase vm, Class baseClass, Class? owner = null, params ITypeInfo[] args) :
            base(vm)
#pragma warning restore CS0628
        {
            _owner = owner;
            BaseClass = baseClass;
            TypeParameterInstances = new TypeParameter.Instance[args.Length];
            for (var i = 0; i < args.Length; i++)
                TypeParameterInstances[i] = new TypeParameter.Instance(
                    TypeParameters.First(it => it.Name == BaseClass.TypeParameters[i].Name)!,
                    args[i]);
        }

        public Class BaseClass { get; }
        public ClassRef SelfRef { get; internal set; } = null!;
        public TypeParameter.Instance[] TypeParameterInstances { get; }
        public Class Parent { get; }
        public IDictionary<string, IPackageMember> PackageMembers { get; }
        public bool IsRoot { get; }
        public Package? Package { get; }
        public string Name => BaseClass.Name;
        public string FullName => BaseClass.FullName;
        public ClassMemberType MemberType => BaseClass.MemberType;
        public StatementComponent CatchFinally { get; set; }

        public IPackageMember GetMember(string name)
        {
            throw new NotImplementedException();
        }

        public IPackageMember Add(IPackageMember member)
        {
            throw new NotImplementedException();
        }

        public SourcefilePosition SourceLocation { get; }
        public string CanonicalName => BaseClass.CanonicalName;
        public string FullDetailedName => BaseClass.Package?.FullName + '.' + DetailedName;

        public string DetailedName
        {
            get
            {
                var indexOf = BaseClass.Name.IndexOf('<');
                return BaseClass.Name.Substring(0, indexOf == -1 ? BaseClass.Name.Length : indexOf)
                       + (TypeParameters.Count == 0
                           ? string.Empty
                           : '<' + string.Join(", ", TypeParameterInstances.Select(t => t.TargetType.DetailedName)) +
                             '>');
            }
        }

        public IDictionary<string, IClassMember> DeclaredMembers => BaseClass.DeclaredMembers;
        public IEnumerable<IClassInstance> Superclasses => BaseClass.Superclasses;
        public IEnumerable<IClassInstance> Interfaces => BaseClass.Interfaces;
        public Instance DefaultInstance => BaseClass.DefaultInstance;

        public List<TypeParameter> TypeParameters => BaseClass.TypeParameters;
        public MemberModifier Modifier => BaseClass.Modifier;
        public ClassType ClassType => BaseClass.ClassType;

        public Instance GetInstance(RuntimeBase vm, params ITypeInfo[] typeParameters)
        {
            return BaseClass.GetInstance(vm, typeParameters);
        }

        public Instance CreateInstance(RuntimeBase vm, Class? owner = null, params ITypeInfo[] typeParameters)
        {
            return BaseClass.CreateInstance(vm, owner, typeParameters);
        }

        public bool CanHold(IClass? type)
        {
            return BaseClass.CanHold(type);
        }

        public bool Primitive => BaseClass.Primitive;
        public override IClassInstance Type => Name == "type" ? this : TypeType.DefaultInstance;

        public override string ToString(short variant)
        {
            return variant switch
            {
                IObject.ToString_ShortName => Name,
                IObject.ToString_LongName => FullName,
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
            };
        }

        public override Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
        {
            // try invoke static method
            if (DeclaredMembers.TryGetValue(member, out var icm))
            {
                if (!icm.IsStatic())
                    throw new FatalException($"Cannot invoke non-static method {icm.FullName} from static context");
                var param = (icm as IMethod)?.Parameters;
                for (var i = 0; i < param.Count; i++)
                    vm.PutLocal(stack, param[i].Name, args.Length - 1 < i ? IObject.Null : args[i]);
                icm.Invoke(vm, stack);
                return stack;
            }

            throw new FatalException("Method not implemented: " + member);
        }

        public override string GetKey()
        {
            return "class-instance:" + (_owner != null
                ? _owner.FullName + '<' + FullName + '>'
                : FullName);
        }

        public BytecodeElementType ElementType => throw new NotSupportedException();

        public Stack Invoke(RuntimeBase vm, Stack stack, IObject? target = null, StackOutput maintain = StackOutput.Omg,
            params IObject?[] args)
        {
            return BaseClass.Invoke(vm, stack, target, maintain, args);
        }

        public Stack Evaluate(RuntimeBase vm, Stack stack)
        {
            throw new NotSupportedException();
        }

        public void Initialize(RuntimeBase vm)
        {
            if (_initialized) return;
            SelfRef = new ClassRef(this);
            _initialized = true;
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}

public sealed class TypeParameter : ITypeParameter
{
    private TypeParameterSpecializationType _specialization;

    public TypeParameter(string name, TypeParameterSpecializationType? specialization = null!,
        IClass? specializationTarget = null!)
    {
        Name = name;
        Specialization = specialization ?? TypeParameterSpecializationType.Extends;
        SpecializationTarget = specializationTarget ?? Class.VoidType;
    }

    public string Name { get; }
    public string FullName => Name;
    public string CanonicalName => Name;
    public string FullDetailedName => Name;
    public string DetailedName => Name;
    public List<TypeParameter> TypeParameters { get; } = new();

    public TypeParameterSpecializationType Specialization
    {
        get => Name == "n" ? TypeParameterSpecializationType.N : _specialization;
        set
        {
            if (_specialization == TypeParameterSpecializationType.N)
                throw new InvalidOperationException("Cannot change n specialization");
            _specialization = value;
        }
    }

    public IClass SpecializationTarget { get; }
    public ITypeInfo? DefaultValue { get; init; }

    public ITypeInfo? TargetType => null;

    public override string ToString()
    {
        var str = FullName;
        switch (Specialization)
        {
            case TypeParameterSpecializationType.Extends:
            case TypeParameterSpecializationType.Super:
                if (!SpecializationTarget.Equals(Class.VoidType))
                    str += ' ' + Specialization.ToString().ToLower() + ' ' + SpecializationTarget;
                break;
            case TypeParameterSpecializationType.List:
                str += "...";
                break;
            case TypeParameterSpecializationType.N:
                break;
        }

        return str;
    }

    public sealed class Instance : ITypeParameterInstance
    {
        public readonly TypeParameter TypeParameter;

        public Instance(TypeParameter typeParameter, ITypeInfo targetType)
        {
            TypeParameter = typeParameter;
            TargetType = targetType;
        }

        public TypeParameterSpecializationType Specialization => TypeParameter.Specialization;
        public IClass SpecializationTarget => TypeParameter.SpecializationTarget;

        public ITypeInfo TargetType { get; }
        public string Name => TypeParameter.Name;
        public string CanonicalName => Name;
        public string FullName => (TargetType as Class.Instance)?.FullName ?? TypeParameter.FullName;

        public string FullDetailedName =>
            (TargetType as Class.Instance)?.FullDetailedName ?? TypeParameter.FullDetailedName;

        public string DetailedName => (TargetType as Class.Instance)?.DetailedName ?? TypeParameter.DetailedName;
        public List<TypeParameter> TypeParameters { get; } = new();

        public IClassInstance ResolveType(RuntimeBase vm, IClassInstance usingClass)
        {
            if (usingClass.TypeParameterInstances.Length == 0
                || usingClass.TypeParameterInstances.All(x => x.Name != Name))
                throw new ArgumentException("Invalid resolver class");
            if (usingClass.TypeParameterInstances.FirstOrDefault(x => x.Name == Name) is var tpi
                && tpi?.TargetType is IClassInstance ici)
                return ici;
            return vm.FindType(usingClass.TypeParameterInstances
                .First(x => x.TypeParameter.Name == TypeParameter.Name).TargetType.FullName, owner: usingClass)!;
        }
    }
}