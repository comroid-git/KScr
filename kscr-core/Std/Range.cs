using System;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Std;

public sealed class Range : IObject
{
    private Range(RuntimeBase vm, int start, int end)
    {
        Start = start;
        End = end;
        ObjectId = vm.NextObjId(CreateKey(start, end));
    }

    public int Start { get; }
    public int End { get; }
    public bool Decremental => End < Start;

    public bool Primitive => true;
    public long ObjectId { get; }
    public IClassInstance Type => Class.RangeType.DefaultInstance;

    public string ToString(short variant)
    {
        return variant switch
        {
            1 => Start.ToString(),
            2 => End.ToString(),
            _ => $"{Start}~{End}"
        };
    }

    public Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
    {
        switch (member)
        {
            case "toString":
                stack[StackOutput.Default] = String.Instance(vm, ToString(0));
                break;
            case "equals":
                stack[StackOutput.Default] = args[0] is not Range other ? vm.ConstantFalse :
                    Start == other.Start && End == other.End ? vm.ConstantTrue : vm.ConstantFalse;
                break;
            case "getType":
                stack[StackOutput.Default] = Type.SelfRef;
                break;
            case "iterator":
                var iterator = new RangeIterator(vm, this);
                stack[StackOutput.Default] = vm.PutObject(stack, VariableContext.Local, iterator);
                break;
            case "start": // get first value
                stack[StackOutput.Default] = start(vm);
                break;
            case "end": // get last value
                stack[StackOutput.Default] = end(vm);
                break;
            case "test": // can accumulate?
                if (args[0] is not Numeric i)
                    throw new ArgumentException("Invalid Argument; expected num");
                stack[StackOutput.Default] = test(vm, i);
                break;
            case "accumulate": // accumulate
                if (args[0] is not Numeric x)
                    throw new ArgumentException("Invalid Argument; expected num");
                stack[StackOutput.Default] = accumulate(vm, x);
                break;
            case "decremental": // accumulate
                stack[StackOutput.Default] = Decremental ? vm.ConstantTrue : vm.ConstantFalse;
                break;
            default: throw new NotImplementedException();
        }

        return stack;
    }

    public string GetKey()
    {
        return CreateKey(Start, End);
    }

    private static string CreateKey(int start, int end)
    {
        return $"range:{start}~{end}";
    }

    public IObjectRef start(RuntimeBase vm)
    {
        return Numeric.Constant(vm, Start);
    }

    public IObjectRef end(RuntimeBase vm)
    {
        return Numeric.Constant(vm, End);
    }

    public IObjectRef test(RuntimeBase vm, Numeric n)
    {
        return (Decremental ? n.IntValue > End : n.IntValue < End) ? vm.ConstantTrue : vm.ConstantFalse;
    }

    public IObjectRef accumulate(RuntimeBase vm, Numeric n)
    {
        return Decremental ? n.OpMinus(vm, Numeric.One) : n.OpPlus(vm, Numeric.One);
    }

    public static IObjectRef Instance(RuntimeBase vm, int start, int end)
    {
        return vm.ComputeObject(RuntimeBase.MainStack, VariableContext.Absolute, CreateKey(start, end),
            () => new Range(vm, start, end));
    }

    private class RangeIterator : IObject
    {
        private readonly Range _range;
        private IObjectRef? _n;

        public RangeIterator(RuntimeBase vm, Range range)
        {
            _range = range;
            ObjectId = vm.NextObjId(ToString(0));
            Type = Class.IteratorType.GetInstance(vm, Class.NumericIntType);
        }

        public long ObjectId { get; }
        public IClassInstance Type { get; }

        public string ToString(short variant)
        {
            return "range-iterator:" + _range;
        }

        public Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
        {
            switch (member)
            {
                case "current":
                    stack[StackOutput.Default] = _n ?? vm.ConstantVoid;
                    break;
                case "next":
                    stack[StackOutput.Default] =
                        _n = _n == null ? _range.start(vm) : _range.accumulate(vm, (_n.Value as Numeric)!);
                    break;
                case "hasNext":
                    stack[StackOutput.Default] = _n == null
                        ? vm.ConstantTrue
                        : _range.test(vm, (_range.accumulate(vm, (_n.Value as Numeric)!).Value as Numeric)!);
                    break;
                default: throw new InvalidOperationException();
            }

            return stack;
        }

        public string GetKey()
        {
            return $"{_range.ToString(0)}-iterator#{ObjectId:X}";
        }

        public override string ToString()
        {
            return ToString(0);
        }
    }
}