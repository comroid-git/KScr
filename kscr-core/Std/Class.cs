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

    public static readonly Class VoidType =
        new(LibCorePackage, "void", true, MemberModifier.Public, ClassType.Interface);

    public static readonly Class TypeType =
        new(LibCorePackage, "type", true, MemberModifier.Public | MemberModifier.Final);

    public static readonly Class ObjectType =
        new(LibCorePackage, "object", true, MemberModifier.Public | MemberModifier.Native);

    public static readonly Class EnumType =
        new(LibCorePackage, "enum", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
            { TypeParameters = { new TypeParameter("T") } };

    public static readonly Class PipeType =
        new(LibCorePackage, "pipe", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native,
                ClassType.Interface)
            { TypeParameters = { new TypeParameter("T") } };

    public static readonly Class ArrayType =
        new(LibCorePackage, "array", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
            { TypeParameters = { new TypeParameter("T") } };

    public static readonly Class TupleType =
        new(LibCorePackage, "tuple", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
            { TypeParameters = { new TypeParameter("T", TypeParameterSpecializationType.List) } };

    public static readonly Class StringType = new(LibCorePackage, "str", true,
        MemberModifier.Public | MemberModifier.Final | MemberModifier.Native);

    public static readonly Class NumericType =
        new(LibCorePackage, "num", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
            { TypeParameters = { new TypeParameter("T") } };

    public static readonly Class RangeType = new(LibCorePackage, "range", true,
        MemberModifier.Public | MemberModifier.Final | MemberModifier.Native);

    public static readonly Class IteratorType =
        new(LibCorePackage, "Iterator", false, MemberModifier.Public, ClassType.Interface)
            { TypeParameters = { new TypeParameter("T") } };

    public static readonly Class IterableType =
        new(LibCorePackage, "Iterable", false, MemberModifier.Public, ClassType.Interface)
            { TypeParameters = { new TypeParameter("T") } };

    public static readonly Class ThrowableType =
        new(LibCorePackage, "Throwable", false, MemberModifier.Public, ClassType.Interface);

    public static readonly Instance NumericByteType = new(NumericType,
            new Class(LibCorePackage, "byte", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native))
        { ObjectId = 0x9 };

    public static readonly Instance NumericShortType = new(NumericType,
            new Class(LibCorePackage, "short", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native))
        { ObjectId = 0x8 };

    public static readonly Class IntType =
        new(LibCorePackage, "int", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
        {
            TypeParameters =
            {
                new TypeParameter("n", TypeParameterSpecializationType.N)
                    { DefaultValue = new TypeInfo { Name = "32" } }
            }
        };

    public static readonly Instance NumericIntType = new(NumericType, IntType) { ObjectId = 0x7 };

    public static readonly Instance NumericLongType = new(NumericType,
            new Class(LibCorePackage, "long", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native))
        { ObjectId = 0x6 };

    public static readonly Instance NumericFloatType = new(NumericType,
            new Class(LibCorePackage, "float", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native))
        { ObjectId = 0x5 };

    public static readonly Instance NumericDoubleType = new(NumericType,
            new Class(LibCorePackage, "double", true,
                MemberModifier.Public | MemberModifier.Final | MemberModifier.Native))
        { ObjectId = 0x4 };

    public readonly IList<IClassInstance> _interfaces = new List<IClassInstance>();
    public readonly IList<IClassInstance> _superclasses = new List<IClassInstance>();

    private bool _initialized;
    private bool _lateInitialized;

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
    public ClassMemberType MemberType { get; }
    public SourcefilePosition SourceLocation { get; init; }

    public BytecodeElementType ElementType => BytecodeElementType.Class;

    public Instance DefaultInstance { get; private set; } = null!;

    public ClassRef SelfRef => DefaultInstance.SelfRef;


    public IDictionary<string, IClassMember> DeclaredMembers { get; } =
        new ConcurrentDictionary<string, IClassMember>();

    public IEnumerable<IClassInstance> Superclasses => _superclasses.SelectMany(ExpandSuperclasses);

    public IEnumerable<IClassInstance> Interfaces =>
        _interfaces.Concat(Superclasses.SelectMany(x => x.Interfaces)).SelectMany(ExpandInterfaces);

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
               || Name == "int" && (type?.DetailedName.StartsWith("num<int") ?? false)
               || type?.BaseClass == BaseClass
               || ((type?.BaseClass as IClass)?.Inheritors
                   .Where(x => x != null)
                   .Select(x => x.BaseClass)
                   .Any(super => super.FullName == FullName) ?? true);
    }

    public bool Primitive { get; }

    public Stack Evaluate(RuntimeBase vm, Stack stack)
    {
        var icm = DeclaredMembers.Values.FirstOrDefault(x => x.Name == Method.StaticInitializerName);
        if (icm == null)
            return stack;
        stack.StepInto(vm, new SourcefilePosition(), SelfRef, icm, stack => icm.Evaluate(vm, stack));
        return stack;
    }

    public Instance CreateInstance(RuntimeBase vm, Class? owner = null, params ITypeInfo[] typeParameters)
    {
        if (typeParameters.Length != TypeParameters.Count)
            throw new ArgumentException("Invalid typeParameter count");
        var instance = new Instance(vm, this, owner, typeParameters);
        instance.Initialize(vm);
        return instance;
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
                    _superclasses.Add(ObjectType.DefaultInstance);
                    break;
                case ClassType.Enum:
                    _superclasses.Add(EnumType.DefaultInstance);
                    break;
            }
        else if (Name == "void") ;
        else if (Name == "object") _interfaces.Add(VoidType.DefaultInstance);
        else if (Name != "object") _superclasses.Add(ObjectType.DefaultInstance);


        vm.ClassStore.Add(this);
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
        ObjectType._interfaces.Add(VoidType.DefaultInstance);

        #endregion

        #region Type Class

        AddToClass(TypeType, toString);
        AddToClass(TypeType, equals);
        AddToClass(TypeType, getType);
        TypeType._superclasses.Add(ObjectType.DefaultInstance);

        #endregion

        #region Enum Class

        var name = new Property(RuntimeBase.SystemSrcPos, EnumType, "name", StringType, MemberModifier.Public);
        var values = new DummyMethod(
            EnumType,
            "values",
            MemberModifier.Public | MemberModifier.Static,
            ArrayType.CreateInstance(vm, EnumType, EnumType.TypeParameters[0]));

        AddToClass(EnumType, toString);
        AddToClass(EnumType, equals);
        AddToClass(EnumType, getType);
        AddToClass(EnumType, name);
        AddToClass(EnumType, values);
        EnumType._superclasses.Add(ObjectType.DefaultInstance);

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

        AddToClass(PipeType, toString);
        AddToClass(PipeType, equals);
        AddToClass(PipeType, getType);
        AddToClass(PipeType, name);
        AddToClass(PipeType, values);

        #endregion

        #region Array Class

        var length = new Property(RuntimeBase.SystemSrcPos, ArrayType, "length", NumericIntType, MemberModifier.Public);

        AddToClass(ArrayType, toString);
        AddToClass(ArrayType, equals);
        AddToClass(ArrayType, getType);
        AddToClass(ArrayType, length);

        #endregion

        #region Tuple Class

        var size = new Property(RuntimeBase.SystemSrcPos, TupleType, "size", NumericIntType, MemberModifier.Public);

        AddToClass(TupleType, toString);
        AddToClass(TupleType, equals);
        AddToClass(TupleType, getType);
        AddToClass(TupleType, size);

        #endregion

        #region Numeric Class

        AddToClass(NumericType, toString);
        AddToClass(NumericType, equals);
        AddToClass(NumericType, getType);
        NumericType._interfaces.Add(ThrowableType.DefaultInstance);

        #endregion

        #region String Class

        var strlen = new DummyMethod(StringType, "length", MemberModifier.Public | MemberModifier.Final,
            NumericIntType);

        AddToClass(StringType, toString);
        AddToClass(StringType, equals);
        AddToClass(StringType, strlen);
        AddToClass(StringType, getType);

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
        var iterator = new DummyMethod(IterableType, "iterator", MemberModifier.Public | MemberModifier.Abstract,
            IteratorType.CreateInstance(vm, IterableType, IterableType.TypeParameters[0]));

        AddToClass(RangeType, toString);
        AddToClass(RangeType, equals);
        AddToClass(RangeType, start);
        AddToClass(RangeType, end);
        AddToClass(RangeType, test);
        AddToClass(RangeType, accumulate);
        AddToClass(RangeType, decremental);
        AddToClass(RangeType, getType);
        AddToClass(RangeType, iterator);
        RangeType._interfaces.Add(IterableType.CreateInstance(vm, RangeType, NumericIntType));

        #endregion

        #region Iterator Class

        var current = new DummyMethod(IteratorType, "current", MemberModifier.Public | MemberModifier.Abstract,
            IteratorType.TypeParameters[0]);
        var next = new DummyMethod(IteratorType, "next", MemberModifier.Public | MemberModifier.Abstract,
            IteratorType.TypeParameters[0]);
        var hasNext = new DummyMethod(IteratorType, "hasNext", MemberModifier.Public | MemberModifier.Abstract,
            NumericByteType);

        AddToClass(IteratorType, toString);
        AddToClass(IteratorType, equals);
        AddToClass(IteratorType, getType);
        AddToClass(IteratorType, current);
        AddToClass(IteratorType, next);
        AddToClass(IteratorType, hasNext);

        #endregion

        #region Iterable Class

        AddToClass(IterableType, toString);
        AddToClass(IterableType, equals);
        AddToClass(IterableType, getType);
        AddToClass(IterableType, iterator);

        #endregion

        #region Throwable Class

        AddToClass(ThrowableType, toString);
        AddToClass(ThrowableType, equals);
        AddToClass(ThrowableType, getType);

        #endregion
    }

    private static void AddToClass(Class type, IClassMember dummyMethod)
    {
        type.DeclaredMembers[dummyMethod.Name] = dummyMethod;
    }

    public sealed class Instance : IClassInstance
    {
        private readonly Class? _owner;
        private bool _initialized;
#pragma warning disable CS0628
        protected internal Instance(Class baseClass, params ITypeInfo[] args) : this(null!, baseClass, null, args)
#pragma warning restore CS0628
        {
        }
#pragma warning disable CS0628
        protected internal Instance(RuntimeBase vm, Class baseClass, Class? owner = null, params ITypeInfo[] args)
#pragma warning restore CS0628
        {
            _owner = owner;
            BaseClass = baseClass;
            TypeParameterInstances = new TypeParameter.Instance[args.Length];
            for (var i = 0; i < args.Length; i++)
                TypeParameterInstances[i] = new TypeParameter.Instance(
                    TypeParameters.First(it => it.Name == BaseClass.TypeParameters[i].Name)!,
                    args[i]);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (vm != null)
                ObjectId = vm.NextObjId(GetKey());
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
        public long ObjectId { get; init; }
        public IClassInstance Type => Name == "type" ? this : TypeType.DefaultInstance;

        public string ToString(short variant)
        {
            return variant switch
            {
                IObject.ToString_ShortName => Name,
                IObject.ToString_LongName => FullName,
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
            };
        }

        public Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
        {
            // try invoke static method
            if (DeclaredMembers.TryGetValue(member, out var icm))
            {
                if (!icm.IsStatic())
                    throw new FatalException($"Cannot invoke non-static method {icm.FullName} from static context");
                var param = (icm as IMethod)?.Parameters;
                for (var i = 0; i < param.Count; i++)
                    vm.PutLocal(stack, param[i].Name, args.Length - 1 < i ? IObject.Null : args[i]);
                icm.Evaluate(vm, stack);
                return stack;
            }

            throw new FatalException("Method not implemented: " + member);
        }

        public string GetKey()
        {
            return "class-instance:" + (_owner != null
                ? _owner.FullName + '<' + FullName + '>'
                : FullName);
        }

        public Stack Evaluate(RuntimeBase vm, Stack stack)
        {
            throw new NotSupportedException();
        }

        public BytecodeElementType ElementType => throw new NotSupportedException();

        public void Initialize(RuntimeBase vm)
        {
            if (_initialized) return;
            SelfRef = vm.ClassStore.Add(this)!;
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