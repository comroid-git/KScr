using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Store;

namespace KScr.Lib.Model
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
        string Name { get; }
        string FullName { get; }
        List<ITypeInfo> TypeParameters { get; }
    }

    public struct TypeInfo : ITypeInfo
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public List<ITypeInfo> TypeParameters { get; set; }
    }

    public interface IClassInfo : ITypeInfo
    {
        MemberModifier Modifier { get; }
        ClassType ClassType { get; }
        bool Primitive { get; }

        bool CanHold(IClass? type);
    }

    public interface IClass : IClassInfo
    {
        Class BaseClass { get; }
        IObjectRef SelfRef { get; }
        IEnumerable<IClassMember> ClassMembers => DeclaredMembers.Values.Concat(InheritedMembers);

        IEnumerable<IClassMember> InheritedMembers =>
            Inheritors.Where(it => it != null).SelectMany(it => it.ClassMembers);
        string CanonicalName { get; }
        string DetailedName { get; }

        IDictionary<string, IClassMember> DeclaredMembers { get; }
        IEnumerable<IClassInstance> Inheritors => Superclasses.Concat(Interfaces);
        IList<IClassInstance> Superclasses { get; }
        IList<IClassInstance> Interfaces { get; }
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
        public ClassInfo(MemberModifier modifier, ClassType classType, string name, string fullName)
        {
            Modifier = modifier;
            ClassType = classType;
            Name = name;
            FullName = fullName;
            TypeParameters = null!;
        }

        public MemberModifier Modifier { get; }
        public ClassType ClassType { get; }
        public string Name { get; }
        public string FullName { get; }
        public List<ITypeInfo> TypeParameters { get; set; }

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
    }

    public enum TypeParameterSpecializationType
    {
        Extends,
        Super,
        List,
        N
    }
}