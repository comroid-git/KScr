using System;
using KScr.Lib.Bytecode;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Core
{
    public sealed class Range : IObject
    {
        public long ObjectId { get; }
        public int Start { get; }
        public int End { get; }
        public bool Decremental => End < Start;

        private Range(RuntimeBase vm, int start, int end)
        {
            Start = start;
            End = end;
            ObjectId = vm.NextObjId($"range:{CreateKey(start,end)}");
        }

        public bool Primitive => true;
        public IClassInstance Type => Class.RangeType.DefaultInstance;

        public string ToString(short variant) => variant switch
        {
            1 => Start.ToString(),
            2 => End.ToString(),
            _ => CreateKey(Start, End)
        };

        private static string CreateKey(int start, int end) => $"{start}~{end}";

        public ObjectRef? Invoke(RuntimeBase vm, string member, params IObject?[] args)
        {
            switch (member)
            {
                case "toString":
                    return String.Instance(vm, ToString(0));
                case "equals":
                    if (args[0] is not Range other)
                        return vm.ConstantFalse;
                    return Start == other.Start && End == other.End ? vm.ConstantTrue : vm.ConstantFalse;
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
            }

            throw new NotImplementedException();
        }
        
        public ObjectRef start(RuntimeBase vm) => Numeric.Constant(vm, Start);
        public ObjectRef end(RuntimeBase vm) => Numeric.Constant(vm, End);
        public ObjectRef test(RuntimeBase vm, Numeric n) => (Decremental ? n.IntValue > End : n.IntValue < End) ? vm.ConstantTrue : vm.ConstantFalse;
        public ObjectRef accumulate(RuntimeBase vm, Numeric n) => Decremental ? n.OpMinus(vm, Numeric.One) : n.OpPlus(vm, Numeric.One);

        public static ObjectRef Instance(RuntimeBase vm, int start, int end)
        {
            return vm.ComputeObject(VariableContext.Absolute, CreateKey(start, end), () => new Range(vm, start, end));
        }
    }
}