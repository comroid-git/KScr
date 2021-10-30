﻿using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;
using String = KScr.Lib.Core.String;

namespace KScr.Lib.Bytecode
{
    public class Statement : AbstractBytecode, IStatement<IStatementComponent>
    {
        public StatementComponentType Type { get; internal set; }
        public IClassRef TargetType { get; internal set; } = ClassRef.VoidType;
        public List<IStatementComponent> Main { get; } = new List<IStatementComponent>();

        public State Evaluate(RuntimeBase vm, IEvaluable? prev, ref ObjectRef? rev)
        {
            var state = State.Normal;

            foreach (var component in Main)
            {
                switch (component.Type)
                {
                    default:
                        state = component.Evaluate(vm, prev, ref rev);
                        break;
                }

                if (state != State.Normal)
                    break;
                prev = component;
            }

            return state;
        }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => Main.Cast<AbstractBytecode>();
    }

    public class StatementComponent : AbstractBytecode, IStatementComponent
    {
        public Statement Statement { get; internal set; } = null!;
        public VariableContext VariableContext { get; internal set; }
        public string Arg { get; internal set; } = string.Empty;
        public StatementComponent? SubComponent { get; internal set; }
        public StatementComponentType Type { get; internal set; }
        public BytecodeType CodeType { get; internal set; } = BytecodeType.Terminator;

        public State Evaluate(RuntimeBase vm, IEvaluable? prev, ref ObjectRef? rev)
        {
            ObjectRef? output;
            State state;
            switch (Type)
            {
                case StatementComponentType.Expression:
                    // constant expressions; literals
                    switch (CodeType)
                    {
                        case BytecodeType.LiteralNumeric:
                            rev = Numeric.Compile(vm, Arg);
                            return State.Normal;
                        case BytecodeType.LiteralString:
                            rev = String.Instance(vm, Arg);
                            return State.Normal;
                        case BytecodeType.LiteralTrue:
                            rev = vm.ConstantTrue;
                            return State.Normal;
                        case BytecodeType.LiteralFalse:
                            rev = vm.ConstantFalse;
                            return State.Normal;
                        case BytecodeType.Null:
                            rev = vm.ConstantVoid;
                            return State.Normal;
                    }

                    return State.Normal;
                case StatementComponentType.Declaration:
                    // variable declaration
                    rev = vm[VariableContext, Arg] = new ObjectRef(Statement.TargetType);
                    break;
                case StatementComponentType.Pipe:
                    throw new NotImplementedException();
                case StatementComponentType.Code:
                    // subtypes
                    switch (CodeType)
                    {
                        case BytecodeType.Assignment:
                            // assignment
                            if (rev == null)
                                throw new InternalException("Invalid assignment; missing variable name");
                            if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException("Invalid assignment; no Expression found");
                            output = null;
                            var state1 = SubComponent.Evaluate(vm, this, ref output);
                            rev.Value = output?.Value;
                            return state1;
                        case BytecodeType.Return:
                            // return
                            if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException("Invalid return statement; no Expression found");
                            state = SubComponent.Evaluate(vm, this, ref rev);
                            return state == State.Normal ? State.Return : state;
                    }

                    throw new NotImplementedException();
                case StatementComponentType.Operator:
                    throw new NotImplementedException();
                case StatementComponentType.Provider:
                    // non-constant expressions
                    switch (CodeType)
                    {
                        case BytecodeType.ExpressionVariable:
                            // read variable
                            rev = vm[VariableContext, Arg];
                            break;
                        case BytecodeType.Call:
                            // invoke method
                            rev = rev?.Value?.Invoke(vm, Arg); // todo: allow parameters
                            break;
                    }

                    break;
                case StatementComponentType.Consumer:
                    throw new NotImplementedException();
                case StatementComponentType.Emitter:
                    throw new NotImplementedException();
                case StatementComponentType.Lambda:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return State.Normal;
        }

        protected override IEnumerable<AbstractBytecode> BytecodeMembers => SubComponent != null ? new []{ SubComponent } : Array.Empty<AbstractBytecode>();
    }
}