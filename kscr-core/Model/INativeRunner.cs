using KScr.Core.Bytecode;
using KScr.Core.System;
using KScr.Core.Store;

namespace KScr.Core.Model;

public delegate IObjectRef? NativeImplMember(RuntimeBase vm, Stack stack, IObject target, params IObject[] args);

public interface INativeRunner
{
    Stack InvokeMember(RuntimeBase vm, Stack stack, IObject target, IClassMember member);
}

public abstract class NativeObj : IObject
{
    private readonly uint _internalId;

    public NativeObj(RuntimeBase vm)
    {
        _internalId = vm.NextObjId();
    }

    public long ObjectId => RuntimeBase.CombineHash(_internalId, Type.FullDetailedName);

    public virtual string ToString(short variant)
    {
        return $"{Type.Name}#{ObjectId:X16}";
    }

    public abstract IClassInstance Type { get; }

    public abstract string GetKey();

    public abstract Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args);
}