using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;
using Array = System.Array;

namespace KScr.Lib.Bytecode
{
    public sealed class Class : AbstractPackageMember, IClass, IEvaluable
    {
        public static readonly Package LibClassPackage = Package.RootPackage.GetOrCreatePackage("org")
            .GetOrCreatePackage("comroid").GetOrCreatePackage("kscr").GetOrCreatePackage("core");

        public static readonly Class VoidType = 
            new(LibClassPackage, "void", true, MemberModifier.Public, ClassType.Interface);
        public static readonly Class TypeType =
            new(LibClassPackage, "type", true, MemberModifier.Public | MemberModifier.Final);
        public static readonly Class ObjectType = 
            new(LibClassPackage, "object", true, MemberModifier.Public | MemberModifier.Native);
        public static readonly Class EnumType =
            new(LibClassPackage, "enum", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
                { TypeParameters = { new TypeParameter("T") } };
        public static readonly Class ArrayType =
            new(LibClassPackage, "array", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
                { TypeParameters = { new TypeParameter("T") } };
        public static readonly Class TupleType =
            new(LibClassPackage, "tuple", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
                { TypeParameters = { new TypeParameter("T") } };

        public static readonly Class StringType = new(LibClassPackage, "str", true,
            MemberModifier.Public | MemberModifier.Final | MemberModifier.Native);
        public static readonly Class NumericType =
            new(LibClassPackage, "num", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)
                { TypeParameters = { new TypeParameter("T") } };
        public static readonly Class RangeType = new(LibClassPackage, "range", true,
            MemberModifier.Public | MemberModifier.Final | MemberModifier.Native);

        public static readonly Class IteratorType =
            new(LibClassPackage, "Iterator", false, MemberModifier.Public, ClassType.Interface)
                { TypeParameters = { new TypeParameter("T") } };
        public static readonly Class IterableType =
            new(LibClassPackage, "Iterable", false, MemberModifier.Public, ClassType.Interface)
                { TypeParameters = { new TypeParameter("T") } };
        public static readonly Class ThrowableType =
            new(LibClassPackage, "Throwable", false, MemberModifier.Public, ClassType.Interface);

        public static readonly Instance NumericByteType = new(NumericType, (ITypeInfo)
            new Class(LibClassPackage, "byte", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)){ObjectId = 0x9};
        public static readonly Instance NumericShortType = new(NumericType, (ITypeInfo)
            new Class(LibClassPackage, "short", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)){ObjectId = 0x8};
        public static readonly Instance NumericIntType = new(NumericType, (ITypeInfo)
            new Class(LibClassPackage, "int", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)){ObjectId = 0x7};
        public static readonly Instance NumericLongType = new(NumericType, (ITypeInfo)
            new Class(LibClassPackage, "long", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)){ObjectId = 0x6};
        public static readonly Instance NumericFloatType = new(NumericType, (ITypeInfo)
            new Class(LibClassPackage, "float", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)){ObjectId = 0x5};
        public static readonly Instance NumericDoubleType = new(NumericType, (ITypeInfo)
            new Class(LibClassPackage, "double", true, MemberModifier.Public | MemberModifier.Final | MemberModifier.Native)){ObjectId = 0x4};

        private bool _initialized;
        private bool _lateInitialized;

        public Class(Package package, string name, bool primitive, MemberModifier modifier = MemberModifier.Protected,
            ClassType type = ClassType.Class) : base(package, name, modifier)
        {
            Primitive = primitive;
            ClassType = type;
        }

        public Instance DefaultInstance { get; private set; } = null!;

        protected override IEnumerable<AbstractBytecode> BytecodeMembers =>
            (this as IClass).ClassMembers.Where(it => it is AbstractBytecode).Cast<AbstractBytecode>();

        public IList<string> Imports { get; } =
            new List<string>();

        public TypeParameter.Instance[] TypeParameterInstances { get; } = Array.Empty<TypeParameter.Instance>();

        public IObjectRef SelfRef => DefaultInstance.SelfRef;


        public IDictionary<string, IClassMember> DeclaredMembers { get; } =
            new ConcurrentDictionary<string, IClassMember>();

        public IList<IClassInstance> Superclasses { get; } = new List<IClassInstance>();
        public IList<IClassInstance> Interfaces { get; } = new List<IClassInstance>();

        public ClassType ClassType { get; private set; }

        public Instance GetInstance(RuntimeBase vm, params ITypeInfo[] typeParameters)
        {
            return CreateInstance(vm, null, typeParameters);
        }

        public Class BaseClass => this;
        public List<ITypeInfo> TypeParameters { get; } = new();

        public string CanonicalName => FullName;
        public string DetailedName => Name + (TypeParameters.Count == 0 
            ? string.Empty : '<' + string.Join(", ", TypeParameters) + '>');

        public bool CanHold(IClass? type)
        {
            return Name == "void"
                   || type?.BaseClass.Name == "void"
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
            stack.StepInto(vm, new SourcefilePosition(), stack.Alp, icm, stack => icm.Evaluate(vm, stack));
            return stack;
        }

        public void Initialize(RuntimeBase vm)
        {
            if (_initialized) return;
            if (!Primitive) switch (ClassType)
            {
                case ClassType.Class:
                    Superclasses.Add(ObjectType.DefaultInstance);
                    break;
                case ClassType.Enum:
                    Superclasses.Add(EnumType.DefaultInstance);
                    break;
            } else if (Name == "void") ;
            else if (Name == "object") Interfaces.Add(VoidType.DefaultInstance);
            else if (Name != "object") Superclasses.Add(ObjectType.DefaultInstance);
            

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

        public void LateInitialization(RuntimeBase vm, Stack stack)
        {
            if (_lateInitialized) return;
            Evaluate(vm, stack);
            _lateInitialized = true;
        }

        public Instance CreateInstance(RuntimeBase vm, Class? owner = null, params ITypeInfo[] typeParameters)
        {
            if (typeParameters.Length != TypeParameters.Count)
                throw new ArgumentException("Invalid typeParameter count");
            var instance = new Instance(vm, this, owner, typeParameters);
            instance.Initialize(vm);
            return instance;
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

        public void Write(FileInfo file)
        {
            var write = file.OpenWrite();
            Write(write);
            write.Close();
        }

        public override void Write(Stream stream)
        {
            byte[] buf = RuntimeBase.Encoding.GetBytes(Name);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);
            stream.Write(new[] { (byte)ClassType });
            stream.Write(BitConverter.GetBytes((uint)Modifier));

            // imports
            stream.Write(NewLineBytes);
            stream.Write(BitConverter.GetBytes(Imports.Count));
            foreach (string clsName in Imports)
            {
                buf = RuntimeBase.Encoding.GetBytes(clsName);
                stream.Write(BitConverter.GetBytes(buf.Length));
                stream.Write(buf);
            }

            // superclasses
            stream.Write(NewLineBytes);
            stream.Write(BitConverter.GetBytes(Superclasses.Count));
            foreach (var superclass in Superclasses)
            {
                buf = RuntimeBase.Encoding.GetBytes(superclass.FullName);
                stream.Write(BitConverter.GetBytes(buf.Length));
                stream.Write(buf);
            }

            // interfaces
            stream.Write(NewLineBytes);
            stream.Write(BitConverter.GetBytes(Interfaces.Count));
            foreach (var iface in Interfaces)
            {
                buf = RuntimeBase.Encoding.GetBytes(iface.FullName);
                stream.Write(BitConverter.GetBytes(buf.Length));
                stream.Write(buf);
            }

            // members
            stream.Write(NewLineBytes);
            stream.Write(BitConverter.GetBytes(BytecodeMembers.Count()));
            stream.Flush();
            foreach (var member in BytecodeMembers)
            {
                stream.Write(NewLineBytes);
                member.Write(stream);
                stream.Write(NewLineBytes);
                stream.Flush();
            }
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int index)
        {
            DeclaredMembers.Clear();

            int len;
            len = BitConverter.ToInt32(data, index);
            index += 4;
            _name = RuntimeBase.Encoding.GetString(data, index, len);
            index += len;
            ClassType = (ClassType)data[index];
            index += 1;
            Modifier = (MemberModifier)BitConverter.ToUInt32(data, index);
            index += 4;

            Initialize(vm);

            // imports
            index += NewLineBytes.Length;
            len = BitConverter.ToInt32(data, index);
            index += 4;
            for (; len > 0; len--)
            {
                var len2 = BitConverter.ToInt32(data, index);
                index += 4;
                Imports.Add(RuntimeBase.Encoding.GetString(data, index, len2));
                index += len2;
            }
            
            // todo: load imported classes first

            // superclasses
            index += NewLineBytes.Length;
            len = BitConverter.ToInt32(data, index);
            index += 4;
            for (; len > 0; len--)
            {
                var len2 = BitConverter.ToInt32(data, index);
                index += 4;
                Superclasses.Add(vm.FindType(RuntimeBase.Encoding.GetString(data, index, len2), owner: this)!);
                index += len2;
            }

            // interfaces
            index += NewLineBytes.Length;
            len = BitConverter.ToInt32(data, index);
            index += 4;
            for (; len > 0; len--)
            {
                var len2 = BitConverter.ToInt32(data, index);
                index += 4;
                Interfaces.Add(vm.FindType(RuntimeBase.Encoding.GetString(data, index, len2), owner: this)!);
                index += len2;
            }

            // members
            index += NewLineBytes.Length;
            len = BitConverter.ToInt32(data, index);
            index += 4;
            for (var i = 0; i < len; i++)
            {
                index += NewLineBytes.Length;
                var member = AbstractClassMember.Read(vm, this, data, ref index);
                DeclaredMembers[member.Name] = member;
                index += NewLineBytes.Length;
            }

            LateInitialization(vm, RuntimeBase.MainStack);
        }

        public override string ToString()
        {
            return FullName;
        }

        public static Class Read(RuntimeBase vm, FileInfo file, Package package)
        {
            var cls = new Class(package, file.Name.Substring(0, file.Name.IndexOf(".kbin", StringComparison.Ordinal)),
                false);
            cls.Load(vm, File.ReadAllBytes(file.FullName));
            return cls;
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
            ObjectType.Interfaces.Add(VoidType.DefaultInstance);

            #endregion

            #region Type Class

            AddToClass(TypeType, toString);
            AddToClass(TypeType, equals);
            AddToClass(TypeType, getType);
            TypeType.Superclasses.Add(ObjectType.DefaultInstance);

            #endregion

            #region Enum Class

            var name = new Property(EnumType, "name", StringType, MemberModifier.Public);
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
            EnumType.Superclasses.Add(ObjectType.DefaultInstance);

            #endregion

            #region Array Class

            var length = new Property(ArrayType, "length", NumericIntType, MemberModifier.Public);

            AddToClass(ArrayType, toString);
            AddToClass(ArrayType, equals);
            AddToClass(ArrayType, getType);
            AddToClass(ArrayType, length);

            #endregion

            #region Tuple Class

            var size = new Property(TupleType, "size", NumericIntType, MemberModifier.Public);

            AddToClass(TupleType, toString);
            AddToClass(TupleType, equals);
            AddToClass(TupleType, getType);
            AddToClass(TupleType, size);

            #endregion

            #region Numeric Class

            AddToClass(NumericType, toString);
            AddToClass(NumericType, equals);
            AddToClass(NumericType, getType);
            NumericType.Interfaces.Add(ThrowableType.DefaultInstance);

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
            RangeType.Interfaces.Add(IterableType.CreateInstance(vm, RangeType, NumericIntType));

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
                        (TypeParameters.First(it => it.Name == BaseClass.TypeParameters[i].Name) as TypeParameter)!,
                        args[i]);
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (vm != null)
                    ObjectId = vm.NextObjId(GetKey());
            }

            public Class BaseClass { get; }
            public IObjectRef SelfRef { get; internal set; } = null!;
            public TypeParameter.Instance[] TypeParameterInstances { get; }
            public string Name => BaseClass.Name;
            public string FullName => BaseClass.FullName;
            public string CanonicalName => BaseClass.CanonicalName;
            public string FullDetailedName => BaseClass.Parent?.FullName + '.' + DetailedName;
            public string DetailedName
            {
                get
                {
                    int indexOf = BaseClass.Name.IndexOf('<');
                    return BaseClass.Name.Substring(0, indexOf == -1 ? BaseClass.Name.Length : indexOf)
                           + (TypeParameters.Count == 0
                               ? string.Empty
                               : '<' + string.Join(", ", TypeParameterInstances.Select(t => t.TargetType.FullName)) + '>');
                }
            }
            public IDictionary<string, IClassMember> DeclaredMembers => BaseClass.DeclaredMembers;
            public IList<IClassInstance> Superclasses => BaseClass.Superclasses;
            public IList<IClassInstance> Interfaces => BaseClass.Interfaces;
            public Instance DefaultInstance => BaseClass.DefaultInstance;

            public List<ITypeInfo> TypeParameters => BaseClass.TypeParameters;
            public MemberModifier Modifier => BaseClass.Modifier;
            public ClassType ClassType => BaseClass.ClassType;

            public Instance GetInstance(RuntimeBase vm, params ITypeInfo[] typeParameters) 
                => BaseClass.GetInstance(vm, typeParameters);

            public Instance CreateInstance(RuntimeBase vm, Class? owner = null, params ITypeInfo[] typeParameters) 
                => BaseClass.CreateInstance(vm, owner, typeParameters);

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

            public IObjectRef? Invoke(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
            {
                // try invoke static method
                if (DeclaredMembers.TryGetValue(member, out var icm))
                {
                    if (!icm.IsStatic())
                        throw new FatalException("Cannot invoke non-static method from static context");
                    var param = (icm as IMethod)?.Parameters;
                    for (var i = 0; i < param.Count; i++)
                        vm.PutLocal(stack, param[i].Name, args.Length - 1 < i ? IObject.Null : args[i]);
                    icm.Evaluate(vm, stack);
                    return stack.Alp;
                }

                throw new FatalException("Method not implemented: " + member);
            }

            public string GetKey()
            {
                return "class-instance:" + (_owner != null
                    ? _owner.FullName + '<' + FullName + '>'
                    : FullName);
            }

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
            Specialization = name == "n"
                ? TypeParameterSpecializationType.N
                : specialization ?? TypeParameterSpecializationType.Extends;
            SpecializationTarget = specializationTarget ?? Class.VoidType;
        }

        public string Name { get; }
        public string FullName => Name;
        public List<ITypeInfo> TypeParameters { get; } = new();

        public TypeParameterSpecializationType Specialization
        {
            get => _specialization;
            set
            {
                if (_specialization == TypeParameterSpecializationType.N)
                    throw new InvalidOperationException("Cannot change n specialization");
                _specialization = value;
            }
        }

        public IClass SpecializationTarget { get; }

        public ITypeInfo? TargetType => null;

        public override string ToString()
        {
            string str = FullName;
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
            public string FullName => (TargetType as Class.Instance)?.FullDetailedName ?? TypeParameter.FullName;
            public List<ITypeInfo> TypeParameters { get; } = new();

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
}