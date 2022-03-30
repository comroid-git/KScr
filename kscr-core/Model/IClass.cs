using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Core.Bytecode;
using KScr.Core.Core;
using KScr.Core.Store;

namespace KScr.Core.Model
{
    public static class TypeInfoExt
    {
        public static IClassInstance ResolveType(this ITypeInfo typeInfo, IClassInstance classInstance)
        {
            if (typeInfo is IClassInstance ici)
                return ici;
            if (typeInfo is Class cls)
                return cls.DefaultInstance;
            return classInstance.TypeParameterInstances.First(it => it.TypeParameter.Name == typeInfo.Name)
                .TargetType.ResolveType(classInstance);
        }
    }

    public interface ITypeInfo
    {
        List<TypeParameter> TypeParameters { get; }
        string Name { get; }
        string FullName { get; }
        string CanonicalName { get; }
        string FullDetailedName { get; }
        string DetailedName { get; }
    }

    public struct TypeInfo : ITypeInfo
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string CanonicalName { get; set; }
        public string FullDetailedName { get; set; }
        public string DetailedName { get; set; }
        public List<TypeParameter> TypeParameters { get; set; }
    }

    public interface IClassInfo : ITypeInfo
    {
        MemberModifier Modifier { get; }
        ClassType ClassType { get; }
        bool Primitive { get; }

        bool CanHold(IClass? type);
    }

    public interface IClass : IClassInfo, IClassMember, IPackageMember
    {
        new string Name { get; }
        new string FullName { get; }
        new MemberModifier Modifier { get; }
        Class BaseClass { get; }
        IObjectRef SelfRef { get; }
        IEnumerable<IClassMember> ClassMembers => DeclaredMembers.Values.Concat(InheritedMembers);

        IEnumerable<IClassMember> InheritedMembers =>
            Inheritors.Where(it => it != null).SelectMany(it => it.ClassMembers);

        IDictionary<string, IClassMember> DeclaredMembers { get; }
        IEnumerable<IClassInstance> Inheritors => Superclasses.Concat(Interfaces);
        IEnumerable<IClassInstance> Superclasses { get; }
        IEnumerable<IClassInstance> Interfaces { get; }
        Class.Instance DefaultInstance { get; }
        Class.Instance GetInstance(RuntimeBase vm, params ITypeInfo[] typeParameters);
        Class.Instance CreateInstance(RuntimeBase vm, Class? owner = null, params ITypeInfo[] typeParameters);
    }

    public interface IClassInstance : IClass, IObject
    {
        TypeParameter.Instance[] TypeParameterInstances { get; }
    }

    public struct ClassInfo : IClassInfo
    {
        public ClassInfo(MemberModifier modifier, ClassType classType, string name)
        {
            Modifier = modifier;
            ClassType = classType;
            Name = name;
            TypeParameters = new List<TypeParameter>();
        }

        public MemberModifier Modifier { get; }
        public ClassType ClassType { get; }
        public string Name { get; }
        public string FullName { get; init; } = null!;
        public string CanonicalName { get; init; } = null!;
        public string FullDetailedName { get; init; } = null!;
        public string DetailedName { get; init; } = null!;
        public List<TypeParameter> TypeParameters { get; set; }

        public bool CanHold(IClass? type)
        {
            throw new InvalidOperationException("Cannot check inheritance of info struct");
        }

        public bool Primitive => false;
    }

    public enum ClassType : byte
    {
        Unknown = 0,
        Class,
        Enum,
        Interface,
        Annotation
    }

    public interface ITypeParameterInfo : ITypeInfo
    {
    }

    public interface ITypeParameterInstance : ITypeParameterInfo
    {
        ITypeInfo? TargetType { get; }
    }

    public interface ITypeParameter : ITypeParameterInstance
    {
        TypeParameterSpecializationType Specialization { get; }
        IClass SpecializationTarget { get; }
        ITypeInfo? DefaultValue { get; init; } 
    }

    public enum TypeParameterSpecializationType
    {
        Extends,
        Super,
        List,
        N
    }
}