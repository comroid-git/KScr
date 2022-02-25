using System.Collections.Generic;
using KScr.Lib.Bytecode;

namespace KScr.Lib.Model
{
    public interface ITypeInfo
    {
        string FullName { get; }
    }
    
    public interface IClassInfo : ITypeInfo
    {
        MemberModifier Modifier { get; }
        ClassType ClassType { get; }
    }

    public interface IClassInstance : IClassInfo
    {
        List<TypeParameter> TypeParameters { get; }
        TypeParameter.Instance[] TypeParameterInstances { get; }
        Class.Instance CreateInstance(params IClassInstance[] typeParameters);
    }

    public struct ClassInfo : IClassInfo
    {
        public ClassInfo(MemberModifier modifier, ClassType classType, string fullName)
        {
            Modifier = modifier;
            ClassType = classType;
            FullName = fullName;
        }

        public MemberModifier Modifier { get; }
        public ClassType ClassType { get; }
        public string FullName { get; }
    }
    
    public interface IClass : IClassInstance
    {
        string Name { get; }
        IDictionary<string, IClassMember> DeclaredMembers { get; }

        bool CanHold(IClass? type) => FullName == "void" || (type?.Name.StartsWith(Name) ?? false); // todo fixme
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