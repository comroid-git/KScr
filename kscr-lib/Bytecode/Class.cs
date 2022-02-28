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
    public sealed class Class : AbstractPackageMember, IClass, IRuntimeSite
    {
        private const MemberModifier LibClassModifier =
            MemberModifier.Public | MemberModifier.Static | MemberModifier.Final;
        private static readonly Package LibClassPackage = Package.RootPackage.GetOrCreatePackage("org")
            .GetOrCreatePackage("comroid").GetOrCreatePackage("kscr").GetOrCreatePackage("core");

        public const string StaticInitializer = "initializer_static";
        public static readonly Class VoidType = new(LibClassPackage, "void", true, LibClassModifier);
        public static readonly Class TypeType = new(LibClassPackage, "type", true, LibClassModifier);
        public static readonly Class ArrayType = new(LibClassPackage, "array", true, LibClassModifier);
        public static readonly Class StringType = new(LibClassPackage, "str", true, LibClassModifier);
        public static readonly Class RangeType = new(LibClassPackage, "range", true, LibClassModifier);
        public static readonly Class NumericType = new(LibClassPackage, "num", true, LibClassModifier) { TypeParameters = { new TypeParameter("T") } };
        public static readonly Class ThrowableType = new(LibClassPackage, "Throwable", true, LibClassModifier, ClassType.Interface);
        public static readonly Instance NumericByteType = new(NumericType, new Class(LibClassPackage, "byte", true, LibClassModifier));
        public static readonly Instance NumericShortType = new(NumericType, new Class(LibClassPackage, "short", true, LibClassModifier));
        public static readonly Instance NumericIntType = new(NumericType, new Class(LibClassPackage, "int", true, LibClassModifier));
        public static readonly Instance NumericLongType = new(NumericType, new Class(LibClassPackage, "long", true, LibClassModifier));
        public static readonly Instance NumericFloatType = new(NumericType, new Class(LibClassPackage, "float", true, LibClassModifier));
        public static readonly Instance NumericDoubleType = new(NumericType, new Class(LibClassPackage, "double", true, LibClassModifier));
        
        public Class(Package package, string name, bool primitive, MemberModifier modifier = MemberModifier.Protected,
            ClassType type = ClassType.Class) : base(package, name, modifier)
        {
            Primitive = primitive;
            ClassType = type;
        }

        private bool _initialized = false;
        
        public void Initialize(RuntimeBase vm) {
            if (_initialized) return;
            DefaultInstance = CreateInstance(vm, TypeParameters.Select(tp => tp.SpecializationTarget).ToArray());
            _initialized = true;
        }

        public Instance DefaultInstance { get; private set; } = null!;

        public ObjectRef SelfRef => DefaultInstance.SelfRef;

        public IDictionary<string, IClassMember> DeclaredMembers { get; } =
            new ConcurrentDictionary<string, IClassMember>();

        public ClassType ClassType { get; }

        public Instance CreateInstance(RuntimeBase vm, params IClass[] typeParameters)
        {
            if (typeParameters.Length != TypeParameters.Count)
                throw new ArgumentException("Invalid typeParameter count");
            var instance = new Instance(this, typeParameters);
            instance.Initialize(vm);
            return instance;
        }

        public Class BaseClass => this;
        public List<TypeParameter> TypeParameters { get; } = new();
        public TypeParameter.Instance[] TypeParameterInstances { get; } = Array.Empty<TypeParameter.Instance>();

        public override string Name => base.Name +
                                       (TypeParameters.Count == 0
                                           ? string.Empty
                                           : '<' + string.Join(", ", TypeParameters) + '>');

        public bool CanHold(IClass? type)
        {
            return Name == "void" || type?.BaseClass == BaseClass;
        }

        public bool Primitive { get; }

        public IRuntimeSite? Evaluate(RuntimeBase vm, ref State state, ref ObjectRef? rev, byte alt = 0)
        {
            return DeclaredMembers[StaticInitializer].Evaluate(vm, ref state, ref rev, alt);
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
            Write(file.OpenWrite());
        }

        public override void Write(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(Name.Length));
            stream.Write(RuntimeBase.Encoding.GetBytes(Name));
            stream.Write(BitConverter.GetBytes((uint)Modifier));

            stream.Write(BitConverter.GetBytes(DeclaredMembers.Count));
            foreach (var member in BytecodeMembers)
            {
                member.Write(stream);
                stream.Write(NewLineBytes);
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
            Modifier = (MemberModifier)BitConverter.ToInt32(data, index);
            index += 4;
            len = BitConverter.ToInt32(data, index);
            index += 4;
            for (var i = 0; i < len; i++)
            {
                var member = AbstractClassMember.Read(vm, this, data, ref index);
                DeclaredMembers[member.Name] = member;
                index += NewLineBytes.Length;
            }
        }

        public override string ToString() => FullName;

        public static Class Read(RuntimeBase vm, FileInfo file, Package package)
        {
            var cls = new Class(package, file.Name, false);
            cls.Load(vm, File.ReadAllBytes(file.FullName));
            return cls;
        }

        public sealed class Instance : IClassInstance
        {
            private bool _initialized = false;
#pragma warning disable CS0628
            protected internal Instance(Class baseClass, params IClass[] typeParameters)
#pragma warning restore CS0628
            {
                BaseClass = baseClass;
                TypeParameterInstances = new TypeParameter.Instance[typeParameters.Length];
                for (var i = 0; i < typeParameters.Length; i++)
                    TypeParameterInstances[i] = new TypeParameter.Instance(TypeParameters[i], typeParameters[i]);
            }

            public void Initialize(RuntimeBase vm)
            {
                if (_initialized) return;
                SelfRef = vm.PutObject(VariableContext.Absolute, "static-classInstance:" + FullName, this);
                _initialized = true;
            }

            public Class BaseClass { get; }
            public ObjectRef SelfRef { get; internal set; } = null!;
            public TypeParameter.Instance[] TypeParameterInstances { get; }
            public IDictionary<string, IClassMember> DeclaredMembers => BaseClass.DeclaredMembers;

            public List<TypeParameter> TypeParameters => BaseClass.TypeParameters;
            public MemberModifier Modifier => BaseClass.Modifier;
            public ClassType ClassType => BaseClass.ClassType;
            public string FullName => BaseClass.FullName;

            public Instance CreateInstance(RuntimeBase vm, params IClass[] typeParameters)
            {
                return BaseClass.CreateInstance(vm, typeParameters);
            }

            public string Name
            {
                get
                {
                    int indexOf = BaseClass.Name.IndexOf('<');
                    return BaseClass.Name.Substring(0, indexOf == -1 ? BaseClass.Name.Length : indexOf)
                           + (TypeParameters.Count == 0 ? string.Empty : '<' + string.Join(", ", TypeParameters.Select(t => t.TargetType?.Name ?? t.Name)) + '>');
                }
            }

            public bool CanHold(IClass? type)
            {
                return BaseClass.CanHold(type);
            }

            public bool Primitive => BaseClass.Primitive;

            public override string ToString() => FullName;
            public long ObjectId { get; }
            public IClassInstance Type => Name == "type" ? this : TypeType.DefaultInstance;

            public string ToString(short variant) => variant switch
            {
                IObject.ToString_ShortName => Name,
                IObject.ToString_LongName => FullName,
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
            };

            public ObjectRef? Invoke(RuntimeBase vm, string member, ref ObjectRef? rev, params IObject?[] args)
            {
                // try invoke static method
                if (DeclaredMembers.TryGetValue(member, out var icm))
                {
                    if (!icm.IsStatic())
                        throw new InternalException("Cannot invoke non-static method from static context");
                    IRuntimeSite? site = icm;
                    State state = State.Normal;
                    ObjectRef? output = new ObjectRef(VoidType.DefaultInstance);
                    do
                    {
                        site = site.Evaluate(vm, ref state, ref output);
                    } while (state == State.Normal && site != null);

                    return output;
                }
                throw new InternalException("Method not implemented: " + member);
            }
        }

        public static void InitializePrimitives(RuntimeBase runtimeBase)
        {
            #region Void Class
            
            var toString = new DummyMethod(
                VoidType, 
                "toString",
                LibClassModifier,
                StringType,
                new List<MethodParameter> {
                new()
                {
                    Name = "variant",
                    Type = NumericShortType
                }
            });
            var equals = new DummyMethod(
                VoidType, 
                "equals",
                LibClassModifier, 
                NumericByteType,
                new List<MethodParameter> {
                new()
                {
                    Name = "other",
                    Type = VoidType.DefaultInstance
                }
            });
            var getType = new DummyMethod(VoidType, "getType", LibClassModifier, TypeType);
            
            AddToClass(VoidType, toString);
            AddToClass(VoidType, equals);
            AddToClass(VoidType, getType);
            
            #endregion
  
            #region Type Class
            
            AddToClass(TypeType, toString);
            AddToClass(TypeType, equals);
            AddToClass(TypeType, getType);
            
            #endregion

            #region Numeric Class
            
            AddToClass(NumericType, toString);
            AddToClass(NumericType, equals);
            AddToClass(NumericType, getType);
            
            #endregion

            #region String Class

            var length = new DummyMethod(StringType, "length", LibClassModifier, NumericIntType);
            
            AddToClass(StringType, toString);
            AddToClass(StringType, equals);
            AddToClass(StringType, length);
            AddToClass(StringType, getType);
            
            #endregion

            #region Range Class
            
            var start = new DummyMethod(VoidType, "start", LibClassModifier, NumericIntType);
            var end = new DummyMethod(VoidType, "end", LibClassModifier, NumericIntType);
            var test = new DummyMethod(
                VoidType, 
                "test", 
                LibClassModifier,
                NumericByteType,
                new List<MethodParameter> {
                new()
                {
                    Name = "i",
                    Type = NumericIntType
                }
            });
            var accumulate = new DummyMethod(
                VoidType, 
                "accumulate",
                LibClassModifier,
                NumericIntType,
                new List<MethodParameter> {
                new()
                {
                    Name = "other",
                    Type = NumericIntType
                }
            });
            var decremental = new DummyMethod(VoidType, "decremental", LibClassModifier, NumericByteType);

            AddToClass(RangeType, toString);
            AddToClass(RangeType, equals);
            AddToClass(RangeType, start);
            AddToClass(RangeType, end);
            AddToClass(RangeType, test);
            AddToClass(RangeType, accumulate);
            AddToClass(RangeType, decremental);
            AddToClass(RangeType, getType);
            
            #endregion

            #region Throwable Class

            AddToClass(ThrowableType, toString);
            AddToClass(ThrowableType, equals);
            AddToClass(ThrowableType, getType);
            
            #endregion
        }

        private static void AddToClass(Class type, DummyMethod dummyMethod) => type.DeclaredMembers[dummyMethod.Name] = dummyMethod;
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

        public IClass? TargetType => null;

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

            public Instance(TypeParameter typeParameter, IClass targetType)
            {
                TypeParameter = typeParameter;
                TargetType = targetType;
            }

            public TypeParameterSpecializationType Specialization => TypeParameter.Specialization;
            public IClass SpecializationTarget => TypeParameter.SpecializationTarget;

            public IClass TargetType { get; }
            public string Name => TypeParameter.Name;
            public string FullName => TypeParameter.FullName;
        }
    }
}