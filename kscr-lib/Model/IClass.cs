using System;
using System.Collections.Generic;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Store;

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

        bool CanHold(IClass? type);
        bool Primitive { get; }
    }

    public interface IClass : IClassInfo
    {
        Class BaseClass { get; }
        ObjectRef SelfRef { get; }
        IDictionary<string, IClassMember> DeclaredMembers { get; }
        Class.Instance CreateInstance(RuntimeBase vm, params IClass[] typeParameters);
    }

    public interface IClassInstance : IClass, IObject
    {
        List<TypeParameter> TypeParameters { get; }
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
        }

        public MemberModifier Modifier { get; }
        public ClassType ClassType { get; }
        public string Name { get; }
        public string FullName { get; }

        public bool CanHold(IClass? type)
        {
            throw new InvalidOperationException("Cannot check inheritance of info struct");
        }

        public bool Primitive => false;
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
        IClass? TargetType { get; }
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