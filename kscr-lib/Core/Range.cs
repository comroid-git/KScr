using System;
using KScr.Lib.Bytecode;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Core
{
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

        public ObjectRef? Invoke(RuntimeBase vm, string member, ref ObjectRef? rev, params IObject?[] args)
        {
            switch (member)
            {
                case "toString":
                    return String.Instance(vm, ToString(0));
                case "equals":
                    if (args[0] is not Range other)
                        return vm.ConstantFalse;
                    return Start == other.Start && End == other.End ? vm.ConstantTrue : vm.ConstantFalse;
                case "getType":
                    return Type.SelfRef;
                case "iterator":
                    var iterator = new RangeIterator(vm, this);
                    return vm.PutObject(VariableContext.Local, iterator);
                case "start": // get first value
                    return start(vm);
                case "end": // get last value
                    return end(vm);
                case "test": // can accumulate?
                    if (args[0] is not Numeric i)
                        throw new ArgumentException("Invalid Argument; expected num");
                    return test(vm, i);
                case "accumulate": // accumulate
                    if (args[0] is not Numeric x)
                        throw new ArgumentException("Invalid Argument; expected num");
                    return accumulate(vm, x);
                case "decremental": // accumulate
                    return Decremental ? vm.ConstantTrue : vm.ConstantFalse;
            }

            throw new NotImplementedException();
        }

        public string GetKey()
        {
            return CreateKey(Start, End);
        }

        private static string CreateKey(int start, int end)
        {
            return $"static-range:{start}~{end}";
        }

        public ObjectRef start(RuntimeBase vm)
        {
            return Numeric.Constant(vm, Start);
        }

        public ObjectRef end(RuntimeBase vm)
        {
            return Numeric.Constant(vm, End);
        }

        public ObjectRef test(RuntimeBase vm, Numeric n)
        {
            return (Decremental ? n.IntValue > End : n.IntValue < End) ? vm.ConstantTrue : vm.ConstantFalse;
        }

        public ObjectRef accumulate(RuntimeBase vm, Numeric n)
        {
            return Decremental ? n.OpMinus(vm, Numeric.One) : n.OpPlus(vm, Numeric.One);
        }

        public static ObjectRef Instance(RuntimeBase vm, int start, int end)
        {
            return vm.ComputeObject(VariableContext.Absolute, CreateKey(start, end), () => new Range(vm, start, end));
        }

        private class RangeIterator : IObject
        {
            private readonly Range _range;
            private ObjectRef? _n;

            public RangeIterator(RuntimeBase vm, Range range)
            {
                _range = range;
                ObjectId = vm.NextObjId(ToString(0));
                Type = Class.IteratorType.CreateInstance(vm, Class.NumericIntType);
            }

            public long ObjectId { get; }
            public IClassInstance Type { get; }

            public string ToString(short variant)
            {
                return "range-iterator:" + _range;
            }

            public ObjectRef? Invoke(RuntimeBase vm, string member, ref ObjectRef? rev, params IObject?[] args)
            {
                switch (member)
                {
                    case "current":
                        return _n ?? vm.ConstantVoid;
                    case "next":
                        return _n = _n == null ? _range.start(vm) : _range.accumulate(vm, (_n.Value as Numeric)!);
                    case "hasNext":
                        return _n == null
                            ? vm.ConstantTrue
                            : _range.test(vm, (_range.accumulate(vm, (_n.Value as Numeric)!).Value as Numeric)!);
                    default:
                        throw new InvalidOperationException();
                }
            }

            public string GetKey()
            {
                return $"{_range.ToString(0)}-iterator#{ObjectId}";
            }
        }
    }
}