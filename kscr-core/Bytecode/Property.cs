using System;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Store;

namespace KScr.Core.Bytecode;

public sealed class Property : AbstractClassMember, IObjectRef
{
    public bool Gettable;
    public ExecutableCode? Getter;
    public bool Inittable;
    public ExecutableCode? Initter;
    public bool Settable;
    public ExecutableCode? Setter;

    public Property(SourcefilePosition sourceLocation, Class parent, string name, ITypeInfo returnType,
        MemberModifier modifier) : base(sourceLocation, parent, name, modifier)
    {
        ReturnType = returnType;
    }

    public override string FullName => Parent.FullName + '.' + Name + ": " + ReturnType.FullName;

    public override ClassMemberType MemberType => ClassMemberType.Property;
    public override BytecodeElementType ElementType => BytecodeElementType.Property;
    public ITypeInfo ReturnType { get; }

    public int Length => 1;
    public bool IsPipe => false; // todo

    public Stack ReadValue(RuntimeBase vm, Stack stack, IObject from)
    {
        // evaluate property with object
        if (Gettable && (ReadAccessor is not ExecutableCode ra || ra.Main.Count == 0))
        {
            // is auto-property
            if (vm[stack, VariableContext.Property, CreateKey(from)] == null)
                stack[StackOutput.Alp | StackOutput.Omg] = vm[stack, VariableContext.Property, CreateKey(from)]
                    = new ObjectRef(ReturnType.ResolveType(stack[StackOutput.Default]!.Value.Type));
            else stack[StackOutput.Alp | StackOutput.Omg] = vm[stack, VariableContext.Property, CreateKey(from)];
            return stack;
        }

        if (ReadAccessor != null)
        {
            ReadAccessor.Evaluate(vm, stack.Output()).Copy(StackOutput.Alp, StackOutput.Alp | StackOutput.Omg);
            return stack;
        }

        throw new FatalException("Property " + FullName + " is not gettable"); // invalid state?
    }

    public Stack WriteValue(RuntimeBase vm, Stack stack, IObject to)
    {
        // evaluate property with object
        if (Settable && (WriteAccessor is not ExecutableCode wa || wa.Main.Count == 0))
        {
            // is auto-property
            vm[stack, VariableContext.Absolute, CreateKey(to)] = stack[StackOutput.Default];
            return stack;
        }

        if (WriteAccessor != null)
            return WriteAccessor.Evaluate(vm, stack);
        throw new InternalException("Property " + FullName + " is not settable");
    }

    public IEvaluable? ReadAccessor
    {
        get => Getter;
        set => Getter = (value as ExecutableCode)!;
    }

    public IEvaluable? WriteAccessor
    {
        get => Setter;
        set => Setter = (value as ExecutableCode)!;
    }

    public IObject Value
    {
        get
        {
            Console.Error.WriteLine("Error: Property needs evaluation");
            return IObject.Null;
        }
        set => Console.Error.WriteLine("Error: Property needs evaluation");
    }

    public IClassInstance Type => ReturnType.ResolveType(Parent.DefaultInstance);

    public IObject this[RuntimeBase vm, Stack stack, int i]
    {
        get => Value;
        set => Value = value;
    }

    public override Stack Invoke(RuntimeBase vm, Stack stack, IObject? target = null, StackOutput maintain = StackOutput.Omg,
        params IObject?[] args) =>
        ReadValue(vm, stack, target ?? Parent.SelfRef[vm, stack, 0]);

    private string CreateSubKey(string ownerKey)
    {
        return $"property-{ownerKey}.{Name}";
    }

    private string CreateKey(IObject parent)
    {
        return $"property-{parent.GetKey()}.{Name}";
    }
}