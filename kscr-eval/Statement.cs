using System;
using System.Collections.Generic;
using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;
using String = KScr.Lib.Core.String;

namespace KScr.Eval
{
    public class Statement : IStatement<StatementComponent>
    {
        public StatementComponentType Type { get; internal set; }
        public TypeRef TargetType { get; internal set; } = TypeRef.VoidType;
        public List<StatementComponent> Main { get; } = new List<StatementComponent>();
        
        public ObjectRef? Evaluate(RuntimeBase vm, IEvaluable? prev, ObjectRef? _)
        {
            ObjectRef? rev = null;
            foreach (var component in Main)
            {
                switch (component.Type)
                {
                    default:
                        rev = component.Evaluate(vm, prev, rev);
                        break;
                }
                prev = component;
            }
            return rev;
        }
    }
    public class StatementComponent : IStatementComponent
    {
        public Statement Statement { get; internal set; } = null!;
        public StatementComponentType Type { get; internal set; }
        public VariableContext VariableContext { get; internal set; }
        public string Arg { get; internal set; } = string.Empty;
        public BytecodeType CodeType { get; internal set; } = BytecodeType.Terminator;
        public StatementComponent? SubComponent { get; internal set; }
        
        public ObjectRef? Evaluate(RuntimeBase vm, IEvaluable? prev, ObjectRef? prevRef)
        {
            switch (Type)
            {
                case StatementComponentType.Expression:
                    // constant expressions; literals
                    switch (CodeType)
                    {
                        case BytecodeType.LiteralNumeric:
                            return Numeric.Compile(vm, Arg);
                        case BytecodeType.LiteralString:
                            return String.Instance(vm, Arg);
                        case BytecodeType.LiteralTrue:
                            return vm.ConstantTrue;
                        case BytecodeType.LiteralFalse:
                            return vm.ConstantFalse;
                        case BytecodeType.Null:
                            return vm.ConstantVoid;
                    }

                    throw new InternalException("Invalid Expression Subtype: " + CodeType);
                case StatementComponentType.Declaration:
                    // variable declaration
                    return vm[VariableContext, Arg] = new ObjectRef(Statement.TargetType);
                case StatementComponentType.Pipe:
                    throw new NotImplementedException();
                case StatementComponentType.Code:
                    // subtypes
                    switch (CodeType)
                    {
                        case BytecodeType.Assignment:
                            // assignment
                            if (prevRef == null)
                                throw new InternalException("Invalid assignment; no ObjectRef found");
                            if (SubComponent == null || (SubComponent.Type & StatementComponentType.Expression) == 0)
                                throw new InternalException("Invalid assignment; no Expression found");
                            prevRef.Value = SubComponent.Evaluate(vm, this, null)?.Value;
                            return prevRef;
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
                            return vm[VariableContext, Arg];
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
            throw new NotImplementedException();
        }
    }
}