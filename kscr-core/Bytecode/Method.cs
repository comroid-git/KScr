using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Store;
using static KScr.Core.Store.StackOutput;

namespace KScr.Core.Bytecode;

public interface IMethod : IClassMember
{
    List<MethodParameter> Parameters { get; }
    ITypeInfo ReturnType { get; }
}

public sealed class DummyMethod : IMethod
{
    public DummyMethod(Class parent, string name, MemberModifier modifier, ITypeInfo returnType)
        : this(parent, name, modifier, returnType, new List<MethodParameter>())
    {
    }

    public DummyMethod(Class parent, string name, MemberModifier modifier, ITypeInfo returnType,
        List<MethodParameter> parameters)
    {
        Parent = parent;
        Name = name;
        Modifier = modifier | MemberModifier.Native;
        Parameters = parameters;
        ReturnType = returnType;
    }

    public Stack Evaluate(RuntimeBase vm, Stack stack)
    {
        throw new InvalidOperationException("Cannot evaluate a dummy method. This is an invalid state.");
    }

    public Class Parent { get; set; }
    public string Name { get; set; }
    public string FullName => Parent.FullName + '.' + Name + ": " + ReturnType.FullName;

    public MemberModifier Modifier { get; set; }
    public List<MethodParameter> Parameters { get; set; }
    public ITypeInfo ReturnType { get; set; }
    public ClassMemberType MemberType => ClassMemberType.Method;
    public SourcefilePosition SourceLocation => RuntimeBase.SystemSrcPos;

    public BytecodeElementType ElementType => BytecodeElementType.Method;
    public Stack Invoke(RuntimeBase vm, Stack stack, IObject? target = null, StackOutput maintain = StackOutput.Omg,
        params IObject?[] args)
    {
        throw new NotImplementedException();
    }
}

public sealed class Method : AbstractClassMember, IMethod
{
    public const string ConstructorName = "ctor";
    public const string StaticInitializerName = "cctor";
    public ExecutableCode Body = null!;

    public Method(SourcefilePosition sourceLocation, Class parent, string name, ITypeInfo returnType,
        MemberModifier modifier) : base(sourceLocation, parent, name, modifier)
    {
        ReturnType = returnType;
    }

    public List<MethodParameter> Parameters { get; } = new();
    public ITypeInfo ReturnType { get; }

    public override string FullName =>
        base.FullName + '(' + string.Join(", ", Parameters.Select(it => it.Type)) + ')';

    public override ClassMemberType MemberType => ClassMemberType.Method;
    public override BytecodeElementType ElementType => BytecodeElementType.Method;
    public override Stack Invoke(RuntimeBase vm, Stack stack, IObject? target = null,
        StackOutput maintain = StackOutput.Omg, params IObject?[] args)
    {
        if (target != null && !this.IsStatic())
            throw new FatalException("Missing invocation target for non-static method " + this.Name);
        stack.StepInto(vm, SourceLocation, target, this, stack =>
        {
            for (int i = 0; i < args.Length; i++)
                vm.PutLocal(stack, Parameters[i].Name, args[i]);
            Body.Evaluate(vm, stack);
            if (stack.State != State.Return && Name != ConstructorName && ReturnType.Name != "void")
                throw new FatalException("Invalid state after method: " + stack.State);
        }, maintain);
        return stack;
    }
}

public class MethodParameter : IBytecode
{
    public ITypeInfo Type { get; set; }
    public string Name { get; set; }

    public BytecodeElementType ElementType => BytecodeElementType.MethodParameter;
}