using System;
using System.Collections.Generic;
using KScr.Lib.Bytecode;

namespace KScr.Lib.Model
{
    public interface ITypeInfo
    {
        string Name { get; }
        string FullName { get; }
    }

    public interface IClassInfo : ITypeInfo
    {
        MemberModifier Modifier { get; }
        ClassType ClassType { get; }

        bool CanHold(IClassInstance? type);
    }

    public interface IClassInstance : IClassInfo
    {
        Class BaseClass { get; }
        List<TypeParameter> TypeParameters { get; }
        TypeParameter.Instance[] TypeParameterInstances { get; }
        Class.Instance CreateInstance(params IClassInstance[] typeParameters);
    }

    public struct ClassInfo : IClassInfo
    {
        public ClassInfo(MemberModifier modifier, ClassType classType, string name, string fullName)
        {
            Modifier = modifier;
            ClassType = classType;
            Name = name;
            FullName = fullName;
        }

        public MemberModifier Modifier { get; }
        public ClassType ClassType { get; }
        public string Name { get; }
        public string FullName { get; }

        public bool CanHold(IClassInstance? type)
        {
            throw new InvalidOperationException("Cannot check inheritance of info struct");
        }
    }

    public interface IClass : IClassInstance
    {
        IDictionary<string, IClassMember> DeclaredMembers { get; }
    }

    public enum ClassType : byte
    {
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
        IClassInstance? TargetType { get; }
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