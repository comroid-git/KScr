using System.Collections.Generic;
using KScr.Core.Model;
using KScr.Core.Std;
using KScr.Core.Store;

namespace KScr.Core.Bytecode;

public class ExecutableCode : IBytecode, IStatement<Statement>, IEvaluable
{
    public IEnumerable<IBytecode> Components => Main;
    public BytecodeElementType ElementType => BytecodeElementType.CodeBlock;

    public Stack Evaluate(RuntimeBase vm, Stack stack)
    {
        foreach (var statement in Main)
        {
            statement.Evaluate(vm, stack);
            if (stack.State != State.Normal)
                break;
        }

        return stack;
    }

    public StatementComponentType Type => StatementComponentType.Code;
    public IClassInstance TargetType { get; protected set; } = Class.VoidType.DefaultInstance;

    public List<Statement> Main { get; } = new();

    public void Append(IEvaluable? here)
    {
        if (here is ExecutableCode bc)
            Main.AddRange(bc.Main);
    }

    public void Clear()
    {
        Main.ForEach(st => st.Clear());
        Main.Clear();
    }
}