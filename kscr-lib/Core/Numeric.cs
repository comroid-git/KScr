﻿using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Core
{
    public enum NumericMode
    {
        Byte,
        Short,
        Int,
        Long,
        Float,
        Double
    }

    public sealed class Numeric : IObject
    {
        public static readonly Regex NumberRegex = new(@"([\d]+)(i|l|f|d|b)?(\.([\d]+)(f|d)?)?");

        public static readonly Numeric Zero = new(0)
        {
            Mutable = false,
            Bytes = BitConverter.GetBytes((byte)0),
            Mode = NumericMode.Byte
        };

        public static readonly Numeric One = new(1)
        {
            Mutable = false,
            Bytes = BitConverter.GetBytes((byte)1),
            Mode = NumericMode.Byte
        };

        private static readonly ConcurrentDictionary<decimal, Numeric> Cache = new();

        private readonly bool _constant;

        private readonly uint _objId;
        private bool _mutable = true;

        private Numeric(uint objId)
        {
            _objId = objId;
        }

        private Numeric(RuntimeBase vm, bool constant = false)
        {
            _constant = constant;
            _objId = vm.NextObjId();
        }

        public bool Mutable
        {
            get => !_constant && _mutable;
            private set => _mutable = value;
        }

        public NumericMode Mode { get; private set; } = NumericMode.Short;
        public byte[] Bytes { get; private set; } = BitConverter.GetBytes((short)0);
        public byte ByteValue => GetAs<byte>();
        public short ShortValue => GetAs<short>();
        public int IntValue => GetAs<int>();
        public long LongValue => GetAs<long>();
        public float FloatValue => GetAs<float>();
        public double DoubleValue => GetAs<double>();
        public string StringValue => GetAs<string>();

        public long ObjectId => RuntimeBase.CombineHash(_objId, CreateKey(StringValue));
        public bool Primitive => true;
        public IClassInstance Type => Class._NumericType(Mode);
        public bool ImplicitlyFalse => FloatValue <= 0;

        public string ToString(short variant)
        {
            return variant switch
            {
                0 => StringValue,
                -1 => Type.FullName,
                _ => $"{StringValue}{char.ToLower(Mode.ToString()[0])}"
            };
        }

        public ObjectRef Invoke(RuntimeBase vm, string member, params IObject?[] args)
        {
            switch (member)
            {
                case "toString":
                    return String.Instance(vm, StringValue);
                case "equals":
                    if (args[0] is not Numeric other)
                        return vm.ConstantFalse;
                    if (Mode is NumericMode.Float or NumericMode.Double && args.Length == 2 &&
                        args[1] is not Numeric delta)
                        throw new InternalException("Invalid second argument; expected: num delta");
                    else delta = (Constant(vm, 0.001).Value as Numeric)!;
                    return Mode switch
                    {
                        NumericMode.Byte => ByteValue == other.ByteValue,
                        NumericMode.Short => ShortValue == other.ShortValue,
                        NumericMode.Int => IntValue == other.IntValue,
                        NumericMode.Long => LongValue == other.LongValue,
                        NumericMode.Float => Math.Abs(FloatValue - other.FloatValue) < delta.FloatValue,
                        NumericMode.Double => Math.Abs(DoubleValue - other.DoubleValue) < delta.DoubleValue,
                        _ => throw new ArgumentOutOfRangeException()
                    } ? vm.ConstantTrue : vm.ConstantFalse;
                default:
                    throw new NotImplementedException();
            }
        }

        public static ObjectRef Constant(RuntimeBase vm, byte value)
        {
            return vm.ComputeObject(VariableContext.Absolute, CreateKey(value), () =>
            {
                var num = new Numeric(vm, true);
                num.SetAs(value);
                num.Mutable = false;
                return num;
            });
        }

        public static ObjectRef Constant(RuntimeBase vm, short value)
        {
            return vm.ComputeObject(VariableContext.Absolute, CreateKey(value), () =>
            {
                var num = new Numeric(vm, true);
                num.SetAs(value);
                num.Mutable = false;
                return num;
            });
        }

        public static ObjectRef Constant(RuntimeBase vm, int value)
        {
            return vm.ComputeObject(VariableContext.Absolute, CreateKey(value), () =>
            {
                var num = new Numeric(vm, true);
                num.SetAs(value);
                num.Mutable = false;
                return num;
            });
        }

        public static ObjectRef Constant(RuntimeBase vm, long value)
        {
            return vm.ComputeObject(VariableContext.Absolute, CreateKey(value), () =>
            {
                var num = new Numeric(vm, true);
                num.SetAs(value);
                num.Mutable = false;
                return num;
            });
        }

        public static ObjectRef Constant(RuntimeBase vm, float value)
        {
            return vm.ComputeObject(VariableContext.Absolute, CreateKey(value), () =>
            {
                var num = new Numeric(vm, true);
                num.SetAs(value);
                num.Mutable = false;
                return num;
            });
        }

        public static ObjectRef Constant(RuntimeBase vm, double value)
        {
            return vm.ComputeObject(VariableContext.Absolute, CreateKey(value), () =>
            {
                var num = new Numeric(vm, true);
                num.SetAs(value);
                num.Mutable = false;
                return num;
            });
        }

        private void SetAs<T>(T value)
        {
            if (!Mutable && !_constant)
                throw new InternalException("Numeric is immutable");

            var type = typeof(T);

            if (type == typeof(byte))
            {
                Bytes = BitConverter.GetBytes((byte)(object)value);
                Mode = NumericMode.Byte;
            }
            else if (type == typeof(short))
            {
                Bytes = BitConverter.GetBytes((short)(object)value);
                Mode = NumericMode.Short;
            }
            else if (type == typeof(int))
            {
                Bytes = BitConverter.GetBytes((int)(object)value);
                Mode = NumericMode.Int;
            }
            else if (type == typeof(long))
            {
                Bytes = BitConverter.GetBytes((long)(object)value);
                Mode = NumericMode.Long;
            }
            else if (type == typeof(float))
            {
                Bytes = BitConverter.GetBytes((float)(object)value);
                Mode = NumericMode.Float;
            }
            else if (type == typeof(double))
            {
                Bytes = BitConverter.GetBytes((double)(object)value);
                Mode = NumericMode.Double;
            }
            else
            {
                throw new InternalException("Invalid target Type: " + type);
            }
        }

        private T GetAs<T>()
        {
            var type = typeof(T);

            if (type == typeof(byte))
            {
                if (Mode == NumericMode.Byte)
                    return (T)(object)Bytes[0];
                switch (Mode)
                {
                    case NumericMode.Short:
                        return (T)(object)(byte)ShortValue;
                    case NumericMode.Int:
                        return (T)(object)(byte)IntValue;
                    case NumericMode.Long:
                        return (T)(object)(byte)LongValue;
                    case NumericMode.Float:
                        return (T)(object)(byte)FloatValue;
                    case NumericMode.Double:
                        return (T)(object)(byte)DoubleValue;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
                }
            }

            if (type == typeof(short))
            {
                if (Mode == NumericMode.Short)
                    return (T)(object)BitConverter.ToInt16(Bytes);
                switch (Mode)
                {
                    case NumericMode.Byte:
                        return (T)(object)(short)ByteValue;
                    case NumericMode.Int:
                        return (T)(object)(short)IntValue;
                    case NumericMode.Long:
                        return (T)(object)(short)LongValue;
                    case NumericMode.Float:
                        return (T)(object)(short)FloatValue;
                    case NumericMode.Double:
                        return (T)(object)(short)DoubleValue;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
                }
            }

            if (type == typeof(int))
            {
                if (Mode == NumericMode.Int)
                    return (T)(object)BitConverter.ToInt32(Bytes);
                switch (Mode)
                {
                    case NumericMode.Byte:
                        return (T)(object)(int)ByteValue;
                    case NumericMode.Short:
                        return (T)(object)(int)ShortValue;
                    case NumericMode.Long:
                        return (T)(object)(int)LongValue;
                    case NumericMode.Float:
                        return (T)(object)(int)FloatValue;
                    case NumericMode.Double:
                        return (T)(object)(int)DoubleValue;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
                }
            }

            if (type == typeof(long))
            {
                if (Mode == NumericMode.Long)
                    return (T)(object)BitConverter.ToInt64(Bytes);
                switch (Mode)
                {
                    case NumericMode.Byte:
                        return (T)(object)(long)ByteValue;
                    case NumericMode.Short:
                        return (T)(object)(long)ShortValue;
                    case NumericMode.Int:
                        return (T)(object)(long)IntValue;
                    case NumericMode.Float:
                        return (T)(object)(long)FloatValue;
                    case NumericMode.Double:
                        return (T)(object)(long)DoubleValue;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
                }
            }

            if (type == typeof(float))
            {
                if (Mode == NumericMode.Float)
                    return (T)(object)BitConverter.ToSingle(Bytes);
                switch (Mode)
                {
                    case NumericMode.Byte:
                        return (T)(object)(float)ByteValue;
                    case NumericMode.Short:
                        return (T)(object)(float)ShortValue;
                    case NumericMode.Int:
                        return (T)(object)(float)IntValue;
                    case NumericMode.Long:
                        return (T)(object)(float)LongValue;
                    case NumericMode.Double:
                        return (T)(object)(float)DoubleValue;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
                }
            }

            if (type == typeof(double))
            {
                if (Mode == NumericMode.Double)
                    return (T)(object)BitConverter.ToDouble(Bytes);
                switch (Mode)
                {
                    case NumericMode.Byte:
                        return (T)(object)(double)ByteValue;
                    case NumericMode.Short:
                        return (T)(object)(double)ShortValue;
                    case NumericMode.Int:
                        return (T)(object)(double)IntValue;
                    case NumericMode.Long:
                        return (T)(object)(double)LongValue;
                    case NumericMode.Float:
                        return (T)(object)(double)FloatValue;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
                }
            }

            if (type == typeof(string))
                switch (Mode)
                {
                    case NumericMode.Byte:
                        return (T)(object)ByteValue.ToString();
                    case NumericMode.Short:
                        return (T)(object)ShortValue.ToString();
                    case NumericMode.Int:
                        return (T)(object)IntValue.ToString();
                    case NumericMode.Long:
                        return (T)(object)LongValue.ToString();
                    case NumericMode.Float:
                        return (T)(object)FloatValue.ToString();
                    case NumericMode.Double:
                        return (T)(object)DoubleValue.ToString();
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
                }

            throw new InternalException("Invalid target Type: " + type);
        }

        public static ObjectRef Compile(RuntimeBase vm, string str)
        {
            var result = NumberRegex.Match(str);

            if (!result.Success)
                throw new InternalException("Invalid numeric literal: " + str);
            var groups = result.Groups;
            string? type = groups[5].Value;
            if (type == string.Empty)
                type = groups[2].Value;

            if (type == "b")
                return Constant(vm, byte.Parse(str.Substring(0, str.Length - 1)));
            if (type == "i")
                return Constant(vm, int.Parse(str.Substring(0, str.Length - 1)));
            if (type == "l")
                return Constant(vm, long.Parse(str.Substring(0, str.Length - 1)));
            if (type == "f")
                return Constant(vm, float.Parse(str.Substring(0, str.Length - 1)));
            if (type == "d")
                return Constant(vm, double.Parse(str.Substring(0, str.Length - 1)));
            if (type != string.Empty)
                throw new InternalException("Invalid target Type: " + type);
            if (groups[3].Length > groups[4].Length)
                return Constant(vm, float.Parse(str));
            try
            {
                return Constant(vm, int.Parse(str));
            }
            catch (OverflowException ignored)
            {
                return Constant(vm, long.Parse(str));
            }
        }

        public ObjectRef Operator(RuntimeBase vm, Operator op, Numeric? right = null) => op switch
        {
            Model.Operator.IncrementRead => OpIR(vm),
            Model.Operator.ReadIncrement => OpRI(vm),
            Model.Operator.DecrementRead => OpDR(vm),
            Model.Operator.ReadDecrement => OpRD(vm),
            Model.Operator.ArithmeticNot => OpNegate(vm),
            Model.Operator.Plus => OpPlus(vm, right!),
            Model.Operator.Minus => OpMinus(vm, right!),
            Model.Operator.Multiply => OpMultiply(vm, right!),
            Model.Operator.Divide => OpDivide(vm, right!),
            Model.Operator.Modulus => OpModulus(vm, right!),
            Model.Operator.Circumflex => OpCircumflex(vm, right!),
            Model.Operator.Greater => OpGt(vm, right!),
            Model.Operator.GreaterEq => OpGtEq(vm, right!),
            Model.Operator.Lesser => OpLs(vm, right!),
            Model.Operator.LesserEq => OpLsEq(vm, right!),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };

        public ObjectRef OpIR(RuntimeBase vm) => OpPlus(vm, One);

        public ObjectRef OpRI(RuntimeBase vm)
        {
            var rev = OpMultiply(vm, One);
            SetAs((OpPlus(vm, One).Value as Numeric)!.DoubleValue);
            return rev;
        }

        public ObjectRef OpDR(RuntimeBase vm) => OpMinus(vm, One);

        public ObjectRef OpRD(RuntimeBase vm)
        {
            var rev = OpMultiply(vm, One);
            SetAs((OpMinus(vm, One).Value as Numeric)!.DoubleValue);
            return rev;
        }
        public ObjectRef OpGt(RuntimeBase vm, Numeric right)=> Mode switch
        {
            NumericMode.Byte => ByteValue > right.ByteValue,
            NumericMode.Short => ShortValue > right.ShortValue,
            NumericMode.Int => IntValue > right.IntValue,
            NumericMode.Long => LongValue > right.LongValue,
            NumericMode.Float => FloatValue > right.FloatValue,
            NumericMode.Double => DoubleValue > right.DoubleValue,
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        } ? vm.ConstantTrue : vm.ConstantFalse;

        public ObjectRef OpGtEq(RuntimeBase vm, Numeric right)=> Mode switch
        {
            NumericMode.Byte => ByteValue >= right.ByteValue,
            NumericMode.Short => ShortValue >= right.ShortValue,
            NumericMode.Int => IntValue >= right.IntValue,
            NumericMode.Long => LongValue >= right.LongValue,
            NumericMode.Float => FloatValue >= right.FloatValue,
            NumericMode.Double => DoubleValue >= right.DoubleValue,
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        } ? vm.ConstantTrue : vm.ConstantFalse;

        public ObjectRef OpLs(RuntimeBase vm, Numeric right)=> Mode switch
        {
            NumericMode.Byte => ByteValue < right.ByteValue,
            NumericMode.Short => ShortValue < right.ShortValue,
            NumericMode.Int => IntValue < right.IntValue,
            NumericMode.Long => LongValue < right.LongValue,
            NumericMode.Float => FloatValue < right.FloatValue,
            NumericMode.Double => DoubleValue < right.DoubleValue,
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        } ? vm.ConstantTrue : vm.ConstantFalse;

        public ObjectRef OpLsEq(RuntimeBase vm, Numeric right)=> Mode switch
        {
            NumericMode.Byte => ByteValue <= right.ByteValue,
            NumericMode.Short => ShortValue <= right.ShortValue,
            NumericMode.Int => IntValue <= right.IntValue,
            NumericMode.Long => LongValue <= right.LongValue,
            NumericMode.Float => FloatValue <= right.FloatValue,
            NumericMode.Double => DoubleValue <= right.DoubleValue,
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        } ? vm.ConstantTrue : vm.ConstantFalse;

        public ObjectRef OpNegate(RuntimeBase vm) => Mode switch
        {
            NumericMode.Byte => Constant(vm, -ByteValue),
            NumericMode.Short => Constant(vm, -ShortValue),
            NumericMode.Int => Constant(vm, -IntValue),
            NumericMode.Long => Constant(vm, -LongValue),
            NumericMode.Float => Constant(vm, -FloatValue),
            NumericMode.Double => Constant(vm, -DoubleValue),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        public ObjectRef OpPlus(RuntimeBase vm, Numeric right) => Mode switch
            {
                NumericMode.Byte => Constant(vm, ByteValue + right.ByteValue),
                NumericMode.Short => Constant(vm, ShortValue + right.ShortValue),
                NumericMode.Int => Constant(vm, IntValue + right.IntValue),
                NumericMode.Long => Constant(vm, LongValue + right.LongValue),
                NumericMode.Float => Constant(vm, FloatValue + right.FloatValue),
                NumericMode.Double => Constant(vm, DoubleValue + right.DoubleValue),
                _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
            };

        public ObjectRef OpMinus(RuntimeBase vm, Numeric right) => Mode switch
            {
                NumericMode.Byte => Constant(vm, ByteValue - right.ByteValue),
                NumericMode.Short => Constant(vm, ShortValue - right.ShortValue),
                NumericMode.Int => Constant(vm, IntValue - right.IntValue),
                NumericMode.Long => Constant(vm, LongValue - right.LongValue),
                NumericMode.Float => Constant(vm, FloatValue - right.FloatValue),
                NumericMode.Double => Constant(vm, DoubleValue - right.DoubleValue),
                _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
            };

        public ObjectRef OpMultiply(RuntimeBase vm, Numeric right) => Mode switch
            {
                NumericMode.Byte => Constant(vm, ByteValue * right.ByteValue),
                NumericMode.Short => Constant(vm, ShortValue * right.ShortValue),
                NumericMode.Int => Constant(vm, IntValue * right.IntValue),
                NumericMode.Long => Constant(vm, LongValue * right.LongValue),
                NumericMode.Float => Constant(vm, FloatValue * right.FloatValue),
                NumericMode.Double => Constant(vm, DoubleValue * right.DoubleValue),
                _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
            };

        public ObjectRef OpDivide(RuntimeBase vm, Numeric right) => Mode switch
            {
                NumericMode.Byte => Constant(vm, ByteValue / right.ByteValue),
                NumericMode.Short => Constant(vm, ShortValue / right.ShortValue),
                NumericMode.Int => Constant(vm, IntValue / right.IntValue),
                NumericMode.Long => Constant(vm, LongValue / right.LongValue),
                NumericMode.Float => Constant(vm, FloatValue / right.FloatValue),
                NumericMode.Double => Constant(vm, DoubleValue / right.DoubleValue),
                _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
            };

        public ObjectRef OpModulus(RuntimeBase vm, Numeric right) => Mode switch
            {
                NumericMode.Byte => Constant(vm, ByteValue % right.ByteValue),
                NumericMode.Short => Constant(vm, ShortValue % right.ShortValue),
                NumericMode.Int => Constant(vm, IntValue % right.IntValue),
                NumericMode.Long => Constant(vm, LongValue % right.LongValue),
                NumericMode.Float => Constant(vm, FloatValue % right.FloatValue),
                NumericMode.Double => Constant(vm, DoubleValue % right.DoubleValue),
                _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
            };

        public ObjectRef OpCircumflex(RuntimeBase vm, Numeric right)
        {
            ObjectRef rev = OpMultiply(vm, One);
            for (int n = right.IntValue; n > 0; n--)
                rev = (rev.Value as Numeric)!.OpMultiply(vm, this);
            return rev;
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public static string CreateKey(string num)
        {
            return "static-num:" + num;
        }

        public static string CreateKey(long num)
        {
            return CreateKey(num.ToString());
        }

        public static string CreateKey(float num)
        {
            return CreateKey(num.ToString(CultureInfo.InvariantCulture));
        }

        public static string CreateKey(double num)
        {
            return CreateKey(num.ToString(CultureInfo.InvariantCulture));
        }
    }
}