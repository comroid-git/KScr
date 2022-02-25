using System.Collections.Generic;
using KScr.Lib.Bytecode;

namespace KScr.Lib.Model
{
    public interface IClassInfo
    {
        MemberModifier Modifier { get; }
        ClassType ClassType { get; }
        string FullName { get; }
    }

    public interface IClassInstance : IClassInfo
    {
        List<TypeParameter> TypeParameters { get; }
        List<TypeParameter.Instance>? TypeParameterInstances { get; }
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

    public interface ITypeParameterDeclaration
    {
        string Name { get; }

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