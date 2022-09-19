using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Store;
using static KScr.Core.Store.StackOutput;
using Range = KScr.Core.Std.Range;
using String = KScr.Core.Std.String;

// ReSharper disable VariableHidesOuterVariable

namespace KScr.Core.Bytecode;

[Flags]
public enum ComponentMember : byte
{
    None = 0x0,
    SubStatement = 0x01,
    AltStatement = 0x02,
    SubComponent = 0x04,
    AltComponent = 0x08,
    PostComponent = 0x10,
    InnerCode = 0x20
}

public class Statement : IBytecode, IStatement<StatementComponent>
{
    public BytecodeType CodeType { get; set; } = BytecodeType.Undefined;
    public string? Arg { get; set; }
    public StatementComponent? CatchFinally { get; set; }
    public BytecodeElementType ElementType => BytecodeElementType.Statement;
    public StatementComponentType Type { get; set; }
    public IClassInstance TargetType { get; set; } = Class.VoidType.DefaultInstance;
    public List<StatementComponent> Main { get; } = new();

    public Stack Evaluate(RuntimeBase vm, Stack stack)
    {
        var caught = false;
        try
        {
            foreach (var component in Main)
            {
                switch (Type, CodeType)
                {
                    case (StatementComponentType.Code, BytecodeType.ParameterExpression):
                        stack[Default] = new ObjectRef(Class.VoidType.DefaultInstance, Main.Count);
                        for (var i = 0; i < Main.Count; i++)
                        {
                            Main[i].Evaluate(vm, stack.Output()).Copy(output: Bet);
                            stack[Default]![vm, stack, i] = stack.Bet?[vm, stack, 0] ?? IObject.Null;
                        }

                        break;
                    case (StatementComponentType.Code, BytecodeType.StmtTry):
                        (string name, IObject value)[]? resources = null;
                        var comp = Main[0];
                        try
                        {
                            if (comp.ByteArg == 1) // try-with-resources
                                resources = comp.SubStatement!.Main.Select(x => (x.Args[1],
                                    x.SubComponent!.Evaluate(vm, stack.Output()).Copy()![vm, stack, 0])).ToArray();

                            var resourcesF = resources;
                            var compF = comp;
                            stack.StepInside(vm, comp.SourcefilePosition, "try", stack =>
                            {
                                foreach (var resource in resourcesF ?? Array.Empty<(string, IObject)>())
                                    vm.PutLocal(stack, resource.name, resource.value);
                                compF.InnerCode!.Evaluate(vm, stack);
                            });
                        }
                        catch (StackTraceException trace)
                        {
                            var innerCause = trace.InnerCause.Obj;
                            foreach (var catchBlock in CatchFinally!.SubStatement!.Main
                                         .Where(x => x.Args.Select(t => vm.FindType(t))
                                             .Any(x => x!.CanHold(innerCause.Type)))
                                         .Select(x => (x.Arg, x.InnerCode)))
                                stack.StepInside(vm, CatchFinally.SourcefilePosition, "catch", stack =>
                                {
                                    vm.PutLocal(stack, catchBlock.Arg, innerCause);
                                    catchBlock.InnerCode!.Evaluate(vm, stack);
                                });
                            caught = true;
                        }
                        finally
                        {
                            try
                            {
                                if (resources != null)
                                    foreach (var resource in resources.Select(x => x.value))
                                        Class.CloseableType.DeclaredMembers["close"].Invoke(vm, stack, resource);
                                CatchFinally!.AltComponent!.InnerCode!.Evaluate(vm, stack);
                                caught = true;
                            }
                            catch (StackTraceException fatal)
                            {
                                throw new FatalException("Inner exception during finalization", fatal);
                            }
                        }

                        break;
                    default:
                        component.Evaluate(vm, stack);
                        break;
                }

                if (stack.State != State.Normal)
                    break;
            }
        }
        catch (InternalException codeEx)
        {
            stack[Tau] = new ObjectRef(Class.ThrowableType.DefaultInstance, codeEx.Obj);
            CatchFinally?.Evaluate(vm, stack);
            caught = true;
            throw codeEx;
        }
        catch (StackTraceException codeEx)
        {
            stack[Tau] = new ObjectRef(Class.ThrowableType.DefaultInstance, codeEx.InnerCause.Obj);
            CatchFinally?.Evaluate(vm, stack);
            caught = true;
            throw codeEx;
        }
        finally
        {
            if (!caught)
                CatchFinally?.AltComponent?.InnerCode!.Evaluate(vm, stack);
        }

        return stack;
    }

    public ITypeInfo OutputType(RuntimeBase vm, ISymbolValidator symbols)
    {
        return Main.LastOrDefault()?.OutputType(vm, symbols)!;
    }

    public void Clear()
    {
        Main.Clear();
    }
}

public class StatementComponent : IBytecode, IStatementComponent
{
    public Statement Statement { get; set; } = null!;
    public VariableContext VariableContext { get; set; }
    public string Arg { get; set; } = string.Empty;
    public List<string> Args { get; set; } = new();
    public ulong ByteArg { get; set; }
    public SourcefilePosition SourcefilePosition { get; set; }
    public Statement? SubStatement { get; set; }
    public Statement? AltStatement { get; set; }
    public StatementComponent? SubComponent { get; set; }
    public StatementComponent? AltComponent { get; set; }
    public StatementComponent? PostComponent { get; set; }
    public ExecutableCode? InnerCode { get; set; }

    public BytecodeElementType ElementType => BytecodeElementType.Component;

    public StatementComponentType Type { get; set; }
    public BytecodeType CodeType { get; set; } = BytecodeType.Undefined;

    public virtual Stack Evaluate(RuntimeBase vm, Stack stack)
    {
        IObjectRef? bak;
        int len;
        switch (Type, CodeType)
        {
            case (StatementComponentType.Expression, BytecodeType.LiteralNumeric):
                stack[Default] = Numeric.Compile(vm, Arg);
                break;
            case (StatementComponentType.Expression, BytecodeType.LiteralString):
                stack[Default] = String.Instance(vm, Arg);
                break;
            case (StatementComponentType.Expression, BytecodeType.LiteralTrue):
                stack[Default] = vm.ConstantTrue;
                break;
            case (StatementComponentType.Expression, BytecodeType.LiteralFalse):
                stack[Default] = vm.ConstantFalse;
                break;
            case (StatementComponentType.Expression, BytecodeType.Null):
                stack[Default] = vm.ConstantVoid;
                break;
            case (StatementComponentType.Code, BytecodeType.Parentheses):
                SubStatement!.Evaluate(vm, stack.Output()).Copy();
                break;
            case (StatementComponentType.Expression, BytecodeType.Parentheses):
                SubComponent!.Evaluate(vm, stack.Output()).Copy();
                break;
            case (StatementComponentType.Expression, BytecodeType.TypeExpression):
                stack[Default] = vm.FindType(Arg).SelfRef;
                break;
            case (StatementComponentType.Expression, BytecodeType.ConstructorCall):
                if (SubStatement?.CodeType != BytecodeType.ParameterExpression)
                    throw new FatalException(
                        "Invalid constructor call; missing parameter expression");
                var type = vm.FindType(Arg)!;
                var ctor = (type.ClassMembers.First(x => x.Name == Method.ConstructorName) as IMethod)!;
                var obj = new CodeObject(vm, type);
                bak = stack[Default] = vm.PutObject(stack, VariableContext.Absolute, obj);
                SubStatement.Evaluate(vm, stack.Output()).Copy(output: Bet);
                stack[Default] = ctor.Invoke(vm, stack, obj, args: stack.Bet!.AsArray(vm, stack)).Copy(output: Omg);
                break;
            case (StatementComponentType.Expression, BytecodeType.Call):
                // invoke member
                if (((stack[Default]!.IsPipe && stack[Default]!.Type is IClass cls)
                     || (cls = (stack[Default]![vm, stack, 0] as IClass)!) != null
                     || (cls = stack[Default]![vm, stack, 0].Type) != null)
                    && cls.ClassMembers.FirstOrDefault(x => x.MemberType == ClassMemberType.Method && x.Name == Arg) is
                        IMethod mtd)
                {
                    var output = stack.Output(Del);
                    output[Del] = new ObjectRef(Class.VoidType.DefaultInstance, mtd.Parameters.Count);
                    SubStatement!.Evaluate(vm, output).Copy(Del);
                    if (mtd.IsNative() && !mtd.Parent.IsNative())
                        if (vm.NativeRunner == null)
                            throw new FatalException("Cannot invoke native method; NativeRunner not loaded");
                        else
                            vm.NativeRunner.InvokeMember(vm, stack, stack[Default][vm, stack, 0], mtd)
                                .Copy(Omg, Default);
                    else mtd.Invoke(vm, stack, stack[Default][vm, stack, 0], args: stack.Del!.AsArray(vm, stack));
                }
                else if (cls.ClassMembers.FirstOrDefault(x => x.MemberType == ClassMemberType.Property && x.Name == Arg)
                         is Property prop)
                {
                    if (prop.IsNative())
                        if (vm.NativeRunner == null)
                            throw new FatalException("Cannot invoke native method; NativeRunner not loaded");
                        else
                            vm.NativeRunner.InvokeMember(vm, stack, stack[Default][vm, stack, 0], prop)
                                .Copy(Omg, Default);
                    else
                        //stack[Default] = prop;
                        prop.ReadValue(vm, stack.Output(),
                            stack[Default]?.Value ?? cls as IClassInstance ?? cls.DefaultInstance).Copy();
                }
                else
                {
                    throw new System.Exception("Invalid state; not a method or property");
                }

                break;
            case (StatementComponentType.Expression, BytecodeType.StdioExpression):
                stack[Default] = vm.StdioRef;
                break;
            case (StatementComponentType.Expression, BytecodeType.EndlExpression):
                stack[Default] = String.Instance(vm, Environment.NewLine);
                break;
            case (StatementComponentType.Expression, BytecodeType.Undefined):
                stack[Default] = stack.This;
                break;
            case (StatementComponentType.Expression, BytecodeType.Cast):
                // casting is implicitly evaluated by design
                break;
            case (StatementComponentType.Declaration, _):
                // variable declaration
                stack[Default] = vm[stack, VariableContext, Args[1]] = new ObjectRef(vm.FindType(Args[0])!);
                if (CodeType == BytecodeType.Assignment)
                {
                    SubComponent!.Evaluate(vm, stack.Output()).Copy(Alp, Bet);
                    stack[Default]![vm, stack, 0] = stack.Bet![vm, stack, 0];
                }

                break;
            case (StatementComponentType.Code, BytecodeType.Assignment):
                // assignment
                if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                    throw new FatalException("Invalid assignment; no target found");
                if (AltComponent == null || (AltComponent.Type & StatementComponentType.Expression) == 0)
                    throw new FatalException("Invalid assignment; no expression found");
                SubComponent.Evaluate(vm, stack.Output()).Copy();
                AltComponent.Evaluate(vm, stack.Output()).Copy(output: Bet);
                stack[Default]![vm, stack, 0] = stack.Bet![vm, stack, 0];
                break;
            case (StatementComponentType.Code, BytecodeType.Return):
                // return
                if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                    throw new FatalException("Invalid return statement; no Expression found");
                SubComponent.Evaluate(vm, stack.Output()).Copy(output: Alp | Omg);
                stack.State = State.Return;
                break;
            case (StatementComponentType.Code, BytecodeType.Throw):
                if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                    throw new FatalException(
                        "Invalid throw statement; no Exception found");
                SubComponent.Evaluate(vm, stack.Output()).Copy(output: Alp | Omg);
                //Console.WriteLine(stack[Alp]);
                stack.State = State.Throw;
                Stack.StackTrace.Clear();
                break;
            case (StatementComponentType.Code, BytecodeType.StmtIf):
                stack.StepInside(vm, SourcefilePosition, "if", stack =>
                {
                    if (SubComponent!.Evaluate(vm, stack.Output()).Copy(output: Phi)!.ToBool())
                        InnerCode!.Evaluate(vm, stack.Output()).CopyState();
                    else if (AltComponent?.CodeType == BytecodeType.StmtElse)
                        AltComponent!.InnerCode!.Evaluate(vm, stack.Output()).CopyState();
                });
                break;
            case (StatementComponentType.Code, BytecodeType.StmtFor):
                stack.StepInside(vm, SourcefilePosition, "for", stack =>
                {
                    for (SubStatement!.Evaluate(vm, stack.Output()).Copy(output: Del);
                         stack.State == State.Normal
                         && SubComponent!.Evaluate(vm, stack.Channel(Del, Phi)).Copy(Phi)!.ToBool()
                         && stack.State == State.Normal;
                         AltComponent!.Evaluate(vm, stack.Output()).Copy(output: Del))
                    {
                        InnerCode!.Evaluate(vm, stack.Output()).CopyState();
                        if (stack.State != State.Normal)
                            break;
                    }
                });
                break;
            case (StatementComponentType.Code, BytecodeType.StmtForEach):
                stack.StepInside(vm, SourcefilePosition, "foreach", stack =>
                {
                    SubComponent!.Evaluate(vm, stack.Output()).Copy(Alp);
                    var iterable = stack[Alp]![vm, stack, 0];
                    iterable.InvokeNative(vm, stack.Output(Eps), "sequence").Copy(Eps);
                    var iterator = stack[Eps]![vm, stack, 0];
                    vm[stack, VariableContext.Local, Arg] = stack[Del]
                        = new ObjectRef(iterator.Type.TypeParameterInstances[0].ResolveType(vm, iterator.Type));
                    while (iterator.InvokeNative(vm, stack.Channel(Eps, Phi), "hasNext").Copy(Phi)!.ToBool())
                    {
                        iterator.InvokeNative(vm, stack.Channel(Eps, Bet), "next").Copy(Bet);
                        var val = stack[Del]![vm, stack, 0] = stack[Bet]![vm, stack, 0];
                        if (val == null || val.ObjectId == 0)
                            throw new NullReferenceException();
                        InnerCode!.Evaluate(vm, stack.Output()).CopyState();
                    }
                });
                break;
            case (StatementComponentType.Code, BytecodeType.StmtWhile):
                stack.StepInside(vm, SourcefilePosition, "while", stack =>
                {
                    while (SubComponent.Evaluate(vm, stack.Output(Phi)).Copy(Phi).ToBool())
                        InnerCode.Evaluate(vm, stack.Output()).CopyState();
                });
                break;
            case (StatementComponentType.Code, BytecodeType.StmtDo):
                stack.StepInside(vm, SourcefilePosition, "do-while", stack =>
                {
                    do
                    {
                        InnerCode.Evaluate(vm, stack.Output()).CopyState();
                    } while (SubComponent.Evaluate(vm, stack.Output(Phi)).Copy(Phi).ToBool());
                });
                break;
            case (StatementComponentType.Code, BytecodeType.StmtCatch):
                var ex = stack[Tau]![vm, stack, 0];
                IClassInfo exType = ex.Type;
                try
                {
                    SubStatement!.Main
                        .FirstOrDefault(comp => comp.Args.Any(exClsName => exClsName == exType.CanonicalName))
                        ?.InnerCode!.Evaluate(vm, stack);
                }
                finally
                {
                    AltComponent?.InnerCode!.Evaluate(vm, stack);
                }

                break;
            case (StatementComponentType.Operator, _):
                var op = (Operator)ByteArg;
                var compound = (op & Operator.Compound) == Operator.Compound;
                var unaryPrefix = (op & Operator.UnaryPrefix) == Operator.UnaryPrefix;
                var unaryPostfix = (op & Operator.UnaryPostfix) == Operator.UnaryPostfix;
                var binary = (op & Operator.Binary) == Operator.Binary;
                if (compound) op ^= Operator.Compound;
                if (unaryPrefix) op ^= Operator.UnaryPrefix;
                if (unaryPostfix) op ^= Operator.UnaryPostfix;
                if (binary) op ^= Operator.Binary;
                if (op is Operator.IncrementRead or Operator.DecrementRead) compound = true;

                var left = SubComponent!.Evaluate(vm, stack.Output()).Copy(output: Alp);

                if (op == Operator.NullFallback) // implement null fallback
                {
                    if (left![vm, stack, 0].IsNull())
                        stack[Default] = AltComponent?.Evaluate(vm, stack.Output()).Copy(output: Bet);
                    else stack[Default] = left;
                }
                else
                {
                    if (unaryPrefix || unaryPostfix)
                    {
                        if (left![vm, stack, 0] is Numeric numA)
                            if (op is Operator.ReadIncrement or Operator.ReadDecrement)
                            {
                                stack[Default] = new ObjectRef(numA.Type, numA);
                                left![vm, stack, 0] = numA.Operator(vm, op)[vm, stack, 0];
                            }
                            else
                            {
                                stack[Default] = numA.Operator(vm, op);
                            }
                        else stack[Default] = left![vm, stack, 0].InvokeNative(vm, stack.Output(), "op" + op).Copy();
                    }
                    else if (binary)
                    {
                        var right = AltComponent?.Evaluate(vm, stack.Output()).Copy(output: Bet);

                        if (left![vm, stack, 0] is Numeric numA && right![vm, stack, 0] is Numeric numB)
                            stack[Default] = numA.Operator(vm, op, numB);
                        else
                            stack[Default] = left![vm, stack, 0]
                                .InvokeNative(vm, stack.Output(), "op" + op, right![vm, stack, 0]).Copy();
                    }

                    if (compound)
                        left![vm, stack, 0] = stack[Default]![vm, stack, 0];
                }

                break;
            case (StatementComponentType.Provider, BytecodeType.LiteralRange):
                SubComponent!.Evaluate(vm, stack.Output()).Copy(Alp, Bet);
                AltComponent!.Evaluate(vm, stack.Output()).Copy(Alp, Del);
                stack[Default] = Range.Instance(vm, (stack.Bet.Value as Numeric)!.IntValue,
                    (stack.Del.Value as Numeric)!.IntValue);
                break;
            case (StatementComponentType.Provider, BytecodeType.ExpressionVariable):
                if (stack[Default]?.Value is { } obj1
                    && obj1.Type.ClassMembers.FirstOrDefault(x => x.Name == Arg) is { } icm1)
                    // call member
                    stack[Default] = icm1.Invoke(vm, stack, stack[Default][vm, stack, 0]).Copy();
                else if (stack.This.Type.ClassMembers.FirstOrDefault(x => x.Name == Arg) is { } icm2)
                    // call to 'this'
                    stack[Default] = icm2.Invoke(vm, stack, stack.This[vm, stack, 0]).Copy();
                else
                    // read variable
                    stack[Default] = vm[stack, VariableContext.Local, Arg]
                                     ?? throw new FatalException("Undefined variable: " + Arg);

                break;
            case (StatementComponentType.Provider, BytecodeType.ArrayConstructor):
                var arrType = vm.FindType(Arg);
                var listed = ByteArg == 1;
                len = listed
                    ? SubStatement!.Main.Count
                    : (SubStatement!.Main[0].Evaluate(vm, stack.Output())[Alp]!.Value as Numeric).IntValue;
                var arrObj = new ArrayObj(vm, arrType ?? Class.VoidType.DefaultInstance, len);
                if (listed)
                    for (var i = 0; i < len; i++)
                    {
                        var idxRes = SubStatement!.Main[i].Evaluate(vm, stack.Output()).Copy(output: Bet)!.Value;
                        arrObj.Arr[i] = new ObjectRef(idxRes);
                    }

                stack[Default] = new ObjectRef(arrObj);
                break;
            case (StatementComponentType.Provider, BytecodeType.Indexer):
                var arr = SubComponent!.Evaluate(vm, stack.Output()).Copy()!.Value as ArrayObj;
                if (arr == null)
                    throw new FatalException("Target Array could not be found");
                var idx = (SubStatement!.Evaluate(vm, stack.Output()).Copy(output: Bet)!.Value as Numeric).IntValue;
                stack[Default] = arr.Arr[idx];
                break;
            case (StatementComponentType.Provider, BytecodeType.Undefined):
                stack[Default] = stack.This!;
                break;
            case (StatementComponentType.Setter, _):
                // assignment
                if (stack[Default] == null)
                    throw new FatalException("Invalid assignment; missing target");
                if (SubStatement == null || (SubStatement.Type & StatementComponentType.Expression) == 0)
                    throw new FatalException("Invalid assignment; no Expression found");
                SubStatement!.Evaluate(vm, stack.Output()).Copy(Alp, Bet);
                stack[Default].Value = stack.Bet.Value;
                break;
            case (StatementComponentType.Emitter, _):
                if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                    throw new FatalException("Invalid emitter; no Expression found");
                if (!stack[Default].IsPipe)
                    throw new FatalException("Cannot emit value into non-pipe accessor");
                SubComponent.Evaluate(vm, stack.Output()).Copy(Alp, Bet);
                stack[Default].WriteValue(vm, stack.Channel(Bet), stack.Bet?.Value ?? IObject.Null);
                break;
            case (StatementComponentType.Consumer, _):
                if (SubComponent == null || (SubComponent.Type & StatementComponentType.Declaration) == 0)
                    throw new FatalException("Invalid consumer; no declaration found");
                if (!stack[Default].IsPipe)
                    throw new FatalException("Cannot consume value from non-pipe accessor");
                SubComponent.Evaluate(vm, stack.Output()).Copy(Alp, Bet);
                stack[Default].ReadValue(vm, stack.Channel(Bet), stack.Bet?.Value ?? IObject.Null);
                break;
            case (StatementComponentType.Pipe, BytecodeType.Call):
                if (SubComponent == null || (SubComponent.Type & StatementComponentType.Lambda) == 0)
                    throw new FatalException("Invalid pipe listener; no lambda found");
                if (!Class.Sequence.CanHold(stack[Default]!.Value.Type) &&
                    !Class.Sequencable.CanHold(stack[Default]!.Value.Type))
                    throw new FatalException(
                        $"Invalid type for pipe listener {stack[Default]!.Value.Type}; requires {Class.Sequencable}");
                if (!Class.Sequence.CanHold(stack[Default]!.Value.Type))
                    Class.Sequencable.DeclaredMembers["sequence"].Invoke(vm, stack.Output(), stack[Default]!.Value)
                        .Copy(Omg, Alp);

                if (Class.Sequence.DeclaredMembers["finite"].Invoke(vm, stack.Output(), stack[Default]!.Value)[Omg]
                    .ToBool())
                {
                    // evaluate finite sequence
                    len = (Class.Sequence.DeclaredMembers["length"]
                        .Invoke(vm, stack.Output(), stack[Default]!.Value)[Omg].Value as Numeric).IntValue;
                    var next = new ObjectRef(Class.VoidType.DefaultInstance, len);

                    for (var i = 0; i < len; i++)
                    {
                        if (!Class.Sequence.DeclaredMembers["hasNext"]
                                .Invoke(vm, stack.Output(), stack[Default]!.Value)[Omg].ToBool())
                            throw new FatalException("Unexpected end of sequence");
                        var it = Class.Sequence.DeclaredMembers["next"]
                            .Invoke(vm, stack.Output(), stack[Default]!.Value)[Omg];
                        var res = stack.StepIntoLambda(vm, stack.Output(), SubComponent, it.Value);
                        next[vm, stack, i] = res!.Value;
                    }

                    stack[Default] = new ObjectRef(Class.Sequence.DefaultInstance,
                        new DummySequence_Finite(vm, next.Type, next.Refs));
                    break;
                } // evaluate infinite sequence

                throw new NotImplementedException("Listening to infinite sequences not implemented");
            default:
                throw new NotImplementedException($"Not Implemented: {Type} {CodeType}");
        }

        if (PostComponent != null)
            PostComponent.Evaluate(vm, stack);

        return stack;
    }

    public ComponentMember GetComponentMember()
    {
        var memberState = ComponentMember.None;
        if (SubStatement != null)
            memberState |= ComponentMember.SubStatement;
        if (AltStatement != null)
            memberState |= ComponentMember.AltStatement;
        if (SubComponent != null)
            memberState |= ComponentMember.SubComponent;
        if (AltComponent != null)
            memberState |= ComponentMember.AltComponent;
        if (PostComponent != null)
            memberState |= ComponentMember.PostComponent;
        if (InnerCode != null)
            memberState |= ComponentMember.InnerCode;
        return memberState;
    }

    public ITypeInfo OutputType(RuntimeBase vm, ISymbolValidator symbols, StatementComponent? prev = null,
        bool ignorePostComp = false)
    {
        if (prev == this)
            throw new FatalException("invalid state");
        ITypeInfo rtrn;
        switch (Type, CodeType)
        {
            case (StatementComponentType.Expression, BytecodeType.LiteralNumeric):
                rtrn = Numeric.Compile(vm, Arg).Type;
                break;
            case (StatementComponentType.Expression, BytecodeType.LiteralString):
            case (StatementComponentType.Expression, BytecodeType.EndlExpression):
                rtrn = Class.StringType;
                break;
            case (StatementComponentType.Expression, BytecodeType.LiteralTrue):
                rtrn = vm.ConstantTrue.Type;
                break;
            case (StatementComponentType.Expression, BytecodeType.LiteralFalse):
                rtrn = vm.ConstantFalse.Type;
                break;
            case (StatementComponentType.Expression, BytecodeType.Null):
                rtrn = Class.VoidType;
                break;
            case (StatementComponentType.Code, BytecodeType.Parentheses):
            case (StatementComponentType.Setter, _):
                rtrn = SubStatement!.OutputType(vm, symbols);
                break;
            case (StatementComponentType.Expression, BytecodeType.TypeExpression):
            case (StatementComponentType.Expression, BytecodeType.ConstructorCall):
                rtrn = vm.FindType(Arg)!;
                break;
            case (StatementComponentType.Expression, BytecodeType.Call):
                // invoke member
                if (symbols.CurrentContext(vm) is { } cls1
                    && cls1.ClassMembers.FirstOrDefault(x => x.MemberType == ClassMemberType.Method && x.Name == Arg) is
                        IMethod mtd)
                    rtrn = mtd.ReturnType;
                else if (symbols.CurrentContext(vm) is { } cls2
                         && cls2.ClassMembers.FirstOrDefault(x =>
                             x.MemberType == ClassMemberType.Property && x.Name == Arg) is Property prop)
                    rtrn = prop.ReturnType;
                else
                    throw new CompilerException(SourcefilePosition, CompilerErrorMessage.SymbolNotFound, Arg,
                        symbols.CurrentContext(vm));
                break;
            case (StatementComponentType.Expression, BytecodeType.StdioExpression):
                rtrn = Class.PipeType.GetInstance(vm, Class.StringType);
                break;
            case (StatementComponentType.Expression, BytecodeType.Undefined):
                rtrn = symbols.CurrentContext(vm);
                break;
            case (StatementComponentType.Declaration, _):
                rtrn = vm.FindType(Args[0])!;
                break;
            case (StatementComponentType.Code, BytecodeType.Assignment):
                rtrn = AltComponent!.OutputType(vm, symbols, this);
                break;
            case (StatementComponentType.Expression, BytecodeType.Cast):
                rtrn = vm.FindTypeInfo(Arg, symbols.CurrentContext(vm).AsClass(vm),
                    symbols.CurrentContext(vm).Package!);
                break;
            case (StatementComponentType.Code, BytecodeType.Throw):
                rtrn = Class.ThrowableType; // todo Specify this (was in next case block)
                break;
            case (StatementComponentType.Expression, BytecodeType.Parentheses):
            case (StatementComponentType.Code, BytecodeType.Return):
            case (StatementComponentType.Operator, _):
                rtrn = SubComponent!.OutputType(vm, symbols, this);
                break;
            case (StatementComponentType.Provider, BytecodeType.LiteralRange):
                rtrn = Class.RangeType;
                break;
            case (StatementComponentType.Provider, BytecodeType.ExpressionVariable):
                if ((prev?.OutputType(vm, symbols, this).AsClass(vm) as IClass)?.ClassMembers.FirstOrDefault(x =>
                        x.Name == Arg) is { } icm1)
                {
                    // call member
                    if (icm1 is Method mtd1)
                        rtrn = mtd1.ReturnType;
                    else if (icm1 is Property prop1)
                        rtrn = prop1.ReturnType;
                    else throw new FatalException("Invalid call to member");
                }
                else if (symbols.CurrentContext(vm).ClassMembers.FirstOrDefault(x => x.Name == Arg) is { } icm2)
                    // call to 'this'
                {
                    if (icm2 is Method mtd1)
                        rtrn = mtd1.ReturnType;
                    else if (icm2 is Property prop1)
                        rtrn = prop1.ReturnType;
                    else throw new FatalException("Invalid call to this");
                }
                else
                    // read variable
                {
                    rtrn = symbols.FindSymbol(Arg)!.Type;
                }

                break;
            case (StatementComponentType.Provider, BytecodeType.ArrayConstructor):
                var type = vm.FindType(Arg) ?? Class.VoidType.DefaultInstance;
                rtrn = Class.ArrayType.CreateInstance(vm, Class.ArrayType, type);
                break;
            case (StatementComponentType.Provider, BytecodeType.Undefined):
                rtrn = symbols.CurrentContext(vm);
                break;
            case (StatementComponentType.Pipe, _):
            case (StatementComponentType.Emitter, _):
            case (StatementComponentType.Consumer, _):
                rtrn = Class.PipeType;
                break;
            default:
                throw new NotImplementedException($"Unable to obtain OutputType of: {Type} {CodeType}");
        }

        if (!ignorePostComp && PostComponent != null)
            rtrn = PostComponent.OutputType(vm, symbols, this);
        return rtrn;
    }
}