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
    private MemberModifier _modifier;

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

    public Class Parent { get; set; }
    public string Name { get; set; }
    public string FullName => Parent.FullName + '.' + Name + ": " + ReturnType.FullName;

    public MemberModifier Modifier
    {
        get => _modifier | MemberModifier.Native;
        set => _modifier = value;
    }

    public List<MethodParameter> Parameters { get; set; }
    public ITypeInfo ReturnType { get; set; }
    public ClassMemberType MemberType => ClassMemberType.Method;
    public StatementComponent CatchFinally { get; set; }
    public SourcefilePosition SourceLocation => RuntimeBase.SystemSrcPos;

    public BytecodeElementType ElementType => BytecodeElementType.Method;

    public Stack Invoke(RuntimeBase vm, Stack stack, IObject? target = null, StackOutput maintain = Omg,
        params IObject?[] args)
    {
        stack.StepInto(vm, SourceLocation, target, this, stack =>
        {
            for (var i = 0; i < Math.Min(Parameters.Count, args.Length); i++)
                vm.PutLocal(stack, Parameters[i].Name, args[i]);
            stack[Omg] = target!.InvokeNative(vm, stack, Name, args).Copy();
        }, maintain);
        return stack;
    }

    public Stack Evaluate(RuntimeBase vm, Stack stack)
    {
        throw new InvalidOperationException("Cannot evaluate a dummy method. This is an invalid state.");
    }
}

public sealed class Method : AbstractClassMember, IMethod
{
    public const string ConstructorName = ".ctor";
    public const string StaticInitializerName = ".cctor";
    public const string IndexerName = ".idx";
    public ExecutableCode Body = null!;

    public Method(SourcefilePosition sourceLocation, Class parent, string name, ITypeInfo returnType,
        MemberModifier modifier) : base(sourceLocation, parent, name, modifier)
    {
        ReturnType = returnType;
    }

    public List<MethodParameter> Parameters { get; } = new();
    public List<StatementComponent> SuperCalls { get; } = new();
    public ITypeInfo ReturnType { get; }

    public override string FullName =>
        base.FullName + '(' + string.Join(", ", Parameters.Select(it => it.Type)) + ')';

    public override ClassMemberType MemberType => ClassMemberType.Method;
    public override BytecodeElementType ElementType => BytecodeElementType.Method;

    public override Stack Invoke(RuntimeBase vm, Stack stack, IObject? target = null,
        StackOutput maintain = Omg, params IObject?[] args)
    {
        if (target == null && !this.IsStatic())
            throw new FatalException("Missing invocation target for non-static method " + Name);
        stack.StepInto(vm, SourceLocation, target, this, stack =>
        {
            if (Name == ConstructorName)
            {
                // pre-handle super calls
                foreach (var superCall in SuperCalls)
                {
                    var superType = vm.FindType(superCall.Arg!)!;
                    var superCtor = superType.DeclaredMembers[ConstructorName];
                    if ((superCall.SubStatement!.CodeType & BytecodeType.ParameterExpression) == 0)
                        throw new FatalException("Invalid supercall: Missing parameter expression");
                    var args = superCall.SubStatement.Evaluate(vm, stack.Output(Del))[Del]!;
                    superCtor.Invoke(vm, stack, target, args: args.AsArray(vm, stack));
                }
            }
            for (var i = 0; i < Math.Min(Parameters.Count, args.Length); i++)
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