﻿using System.Collections.Generic;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Store;

namespace KScr.Core.Bytecode;

public enum ClassMemberType : byte
{
    Method = 0x1,
    Property = 0x2,
    Class = 0x4
}

public interface IClassMember : IEvaluable, IModifierContainer, IBytecode
{
    public Class Parent { get; }
    public string Name { get; }
    public string FullName { get; }
    public ClassMemberType MemberType { get; }
    public SourcefilePosition SourceLocation { get; }
}

public abstract class AbstractClassMember : IClassMember
{
    private protected string _name;

    protected AbstractClassMember(SourcefilePosition sourceLocation, Class parent, string name, MemberModifier modifier)
    {
        Parent = parent;
        _name = name;
        Modifier = modifier;
        SourceLocation = sourceLocation;
    }


    public IEnumerable<IBytecode> Header => new IBytecode[]
    {
        IBytecode.Byte((byte)MemberType),
        IBytecode.UInt((uint)Modifier),
        IBytecode.String(Name)
    };

    public Class Parent { get; }

    public virtual string Name => _name;

    public virtual string FullName => Parent.FullName + '.' + Name;
    public MemberModifier Modifier { get; protected set; }
    public abstract ClassMemberType MemberType { get; }
    public abstract BytecodeElementType ElementType { get; }
    public SourcefilePosition SourceLocation { get; }

    public abstract Stack Evaluate(RuntimeBase vm, Stack stack);
}