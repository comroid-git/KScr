using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KScr.Lib.Core;
using KScr.Lib.Model;
using KScr.Lib.Store;
using Array = System.Array;

namespace KScr.Lib.Bytecode
{
    public sealed class Class : AbstractPackageMember, IClass, IRuntimeSite
    {
        private const MemberModifier LibClassModifier =
            MemberModifier.Public | MemberModifier.Static | MemberModifier.Final;

        public const string StaticInitializer = "initializer_static";
        public static readonly Class VoidType = new(Package.RootPackage, "void", LibClassModifier);
        public static readonly Class StringType = new(Package.RootPackage, "str", LibClassModifier);

        [Obsolete] public static readonly Class ArrayType = new(Package.RootPackage, "array", LibClassModifier);

        public static readonly Class NumericType = new(Package.RootPackage, "num", LibClassModifier)
            { TypeParameters = { new TypeParameter("T") } };

        public static readonly IClassInstance NumericByteType =
            NumericType.CreateInstance(new Class(Package.RootPackage, "byte", LibClassModifier));

        public static readonly IClassInstance NumericShortType =
            NumericType.CreateInstance(new Class(Package.RootPackage, "short", LibClassModifier));

        public static readonly IClassInstance NumericIntegerType =
            NumericType.CreateInstance(new Class(Package.RootPackage, "int", LibClassModifier));

        public static readonly IClassInstance NumericLongType =
            NumericType.CreateInstance(new Class(Package.RootPackage, "long", LibClassModifier));

        public static readonly IClassInstance NumericFloatType =
            NumericType.CreateInstance(new Class(Package.RootPackage, "float", LibClassModifier));

        public static readonly IClassInstance NumericDoubleType =
            NumericType.CreateInstance(new Class(Package.RootPackage, "double", LibClassModifier));

        public Class(Package package, string name, MemberModifier modifier = MemberModifier.Protected,
            ClassType type = ClassType.Class) : base(package, name, modifier)
        {
            ClassType = type;
            DefaultInstance = CreateInstance(TypeParameters.Select(tp => tp.SpecializationTarget)
                .Cast<IClassInstance>().ToArray());
        }

        public Instance DefaultInstance { get; }

        public IDictionary<string, IClassMember> DeclaredMembers { get; } =
            new ConcurrentDictionary<string, IClassMember>();

        public ClassType ClassType { get; }

        public Instance CreateInstance(params IClassInstance[] typeParameters)
        {
            if (typeParameters.Length != TypeParameters.Count)
                throw new ArgumentException("Invalid typeParameter count");
            return new Instance(this, typeParameters);
        }

        public Class BaseClass => this;
        public List<TypeParameter> TypeParameters { get; } = new();
        public TypeParameter.Instance[] TypeParameterInstances { get; } = Array.Empty<TypeParameter.Instance>();

        public override string Name => base.Name +
                                       (TypeParameters.Count == 0
                                           ? string.Empty
                                           : '<' + string.Join(", ", TypeParameters) + '>');

        public bool CanHold(IClassInstance? type)
        {
            return Name == "void" || type?.BaseClass == BaseClass;
        }

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
                NumericMode.Int => NumericIntegerType,
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

        public static Class Read(RuntimeBase vm, FileInfo file, Package package)
        {
            var cls = new Class(package, file.Name);
            cls.Load(vm, File.ReadAllBytes(file.FullName));
            return cls;
        }

        public sealed class Instance : IClassInstance
        {
#pragma warning disable CS0628
            protected internal Instance(Class baseClass, IClassInstance[] typeParameters)
#pragma warning restore CS0628
            {
                BaseClass = baseClass;
                var list = new TypeParameter.Instance[typeParameters.Length];
                for (var i = 0; i < typeParameters.Length; i++)
                    list[i] = new TypeParameter.Instance(TypeParameters[i], typeParameters[i]);
                TypeParameterInstances = list;
            }

            public Class BaseClass { get; }
            public TypeParameter.Instance[] TypeParameterInstances { get; }

            public List<TypeParameter> TypeParameters => BaseClass.TypeParameters;
            public MemberModifier Modifier => BaseClass.Modifier;
            public ClassType ClassType => BaseClass.ClassType;
            public string FullName => BaseClass.FullName;

            public Instance CreateInstance(params IClassInstance[] typeParameters)
            {
                return BaseClass.CreateInstance(typeParameters);
            }

            public string Name => BaseClass.Name.Substring(0, BaseClass.Name.IndexOf('<'))
                                  + (TypeParameters.Count == 0
                                      ? string.Empty
                                      : '<' + string.Join(", ", TypeParameters) + '>');

            public bool CanHold(IClassInstance? type)
            {
                return BaseClass.CanHold(type);
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

        public IClassInstance? TargetType => null;

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

            public Instance(TypeParameter typeParameter, IClassInstance targetType)
            {
                TypeParameter = typeParameter;
                TargetType = targetType;
            }

            public TypeParameterSpecializationType Specialization => TypeParameter.Specialization;
            public IClass SpecializationTarget => TypeParameter.SpecializationTarget;

            public IClassInstance TargetType { get; }
            public string Name => TypeParameter.Name;
            public string FullName => TypeParameter.FullName;
        }
    }
}