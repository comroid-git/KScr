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
        long TypeId { get; }
        IDictionary<string, IClassMember> DeclaredMembers { get; }

        bool CanHold(IClass? type)
        {
            return FullName == "void" || Equals(type);
        }
    }

    public enum ClassType : byte
    {
        Class,
        Enum,
        Interface,
        Annotation
    }
}