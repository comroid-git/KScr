using System;
using System.Collections.Generic;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Store;
using String = KScr.Lib.Core.String;

namespace KScr.Lib.Model
{
    [Flags]
    public enum BytecodeType : uint
    {
        Terminator = 0xFFFF_FFFF,

        Declaration = 0x0000_0001,
        Assignment = 0x0000_0002,
        Expression = 0x0000_0004,
        Statement = 0x0000_0008,
        Operator = 0x0000_000F | Expression,
        Parentheses = 0x0000_0010 | Expression,

        DeclarationNumeric = 0x0000_0020 | Declaration,
        DeclarationString = 0x0000_0040 | Declaration,
        DeclarationByte = 0x0000_0080 | Declaration,
        DeclarationVariable = 0x0000_00F0 | Declaration,

        LiteralNumeric = 0x0000_0100 | Expression,
        LiteralString = 0x0000_0200 | Expression,
        LiteralTrue = 0x0000_0400 | Expression,
        LiteralFalse = 0x0000_0800 | Expression,
        ExpressionVariable = 0x0000_0F00 | Expression,

        OperatorPlus = 0x0000_1000 | Operator,
        OperatorMinus = 0x0000_2000 | Operator,
        OperatorMultiply = 0x0000_4000 | Operator,
        OperatorDivide = 0x0000_8000 | Operator,
        OperatorModulus = 0x0000_F000 | Operator,


        Throw = 0x4000_0000 | Statement,
        Return = 0x8000_0000 | Statement,
        Null = 0xF000_0000 | Expression
    }

    public class Bytecode
    {
        public IList<BytecodePacket> main = new List<BytecodePacket>();

        public static Bytecode operator +(Bytecode left, Bytecode right)
        {
            foreach (var packet in right.main)
                left.main.Add(packet);
            return left;
        }
    }

    public class BytecodePacket
    {
        public BytecodePacket(BytecodeType type = BytecodeType.Terminator, object arg = null)
        {
            Type = type;
            Arg = arg;
        }

        public BytecodeType Type { get; }
        public object? Arg { get; }
        public BytecodePacket? AltPacket { get; set; }
        public BytecodePacket? SubPacket { get; set; }
        public List<BytecodePacket> SubStack { get; } = new List<BytecodePacket>();

        public IObject? Evaluate(BytecodePacket? prev, IObject? prevResult, RuntimeBase vm)
        {
            IObject? result = null, altResult = AltPacket?.Evaluate(this, null, vm);

            if (Type == BytecodeType.Terminator)
                return prevResult;

            // null
            if (Type == BytecodeType.Null)
                return null;
            // syntax statements
            if (Type == BytecodeType.Return)
                return new ReturnValue(altResult);
            if (Type == BytecodeType.Throw)
                return new ThrownValue(altResult);

            // literals
            if (Type == BytecodeType.LiteralNumeric)
                return Numeric.Compile(vm, Arg as string);
            if (Type == BytecodeType.LiteralString)
                return String.Instance(vm, Arg as string);
            if (Type == BytecodeType.LiteralTrue)
                return Numeric.One;
            if (Type == BytecodeType.LiteralFalse)
                return Numeric.MinusOne;

            // numeric operators (need to be BEFORE symbols)
            if ((Type & BytecodeType.Operator) != 0 && prevResult is Numeric left && altResult is Numeric right)
            {
                if ((Type & BytecodeType.OperatorPlus) != BytecodeType.Operator)
                    return left.OpPlus(vm, right);
                if ((Type & BytecodeType.OperatorMinus) != BytecodeType.Operator)
                    return left.OpMinus(vm, right);
                if ((Type & BytecodeType.OperatorMultiply) != BytecodeType.Operator)
                    return left.OpMultiply(vm, right);
                if ((Type & BytecodeType.OperatorDivide) != BytecodeType.Operator)
                    return left.OpDivide(vm, right);
                if ((Type & BytecodeType.OperatorModulus) != BytecodeType.Operator)
                    return left.OpModulus(vm, right);
            }

            // parentheses
            if (Type == BytecodeType.Parentheses)
                return SubStack.Evaluate(vm);

            // symbols
            if ((Type & BytecodeType.Declaration) != 0)
            {
                var rev = new ObjectRef(TypeRef.VoidType, IObject.Null);
                vm[VariableContext.Local, (Arg as string)!] = rev;
                return rev.Value;
            }

            if ((Type & BytecodeType.Assignment) != 0 && ((prev?.Type & BytecodeType.Declaration) != 0 ||
                                                          (prev?.Type & BytecodeType.ExpressionVariable) != 0))
            {
                var value = SubPacket!.Evaluate(this, null, vm) ?? Numeric.Zero;
                if (value is ReturnValue rtn)
                    value = rtn.Value;
                var rev = vm[VariableContext.Local, (prev.Arg as string)!];
                if (rev == null)
                    throw new InternalException("Assignment failed: Local variable not found: " + prev.Arg);
                return rev.Value = value;
            }

            if ((Type & BytecodeType.ExpressionVariable) != 0)
                return vm[VariableContext.Local, (Arg as string)!]?.Value;

            return result;
        }

        public override string ToString()
        {
            return $"BytecodePacket<{Type}{(Arg != null ? "," + Arg : string.Empty)}>";
        }
    }

    public static class BytecodeExtensions
    {
        public static IObject? Evaluate(this IList<BytecodePacket> stack, RuntimeBase vm)
        {
            // todo: Sort arithmetic operators in stack before evaluation

            IObject? yield = null;
            var len = stack.Count;
            BytecodePacket? prev = null;

            for (var i = 0; i < len; i++)
            {
                BytecodePacket packet = stack[i];
                yield = packet.Evaluate(prev, yield, vm);

                if (yield is ReturnValue rtn)
                    return rtn.Value;
                if (yield is ThrownValue thr)
                    throw thr;

                prev = packet;
            }

            return yield;
        }
    }
}