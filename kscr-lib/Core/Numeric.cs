﻿using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using KScr.Lib.VM;

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
        public static readonly Regex NumberRegex = new Regex(@"([\d]+)(i|l|f|d|b)?(\.([\d]+)(f|d)?)?");
        public static readonly Numeric Zero = new Numeric(0)
        {
            Mutable = false,
            Bytes = BitConverter.GetBytes((short)0),
            Mode = NumericMode.Short
        };
        public static readonly Numeric One = new Numeric(1)
        {
            Mutable = false,
            Bytes = BitConverter.GetBytes((short)1),
            Mode = NumericMode.Short
        };
        public static readonly Numeric MinusOne = new Numeric(2)
        {
            Mutable = false,
            Bytes = BitConverter.GetBytes((short)-1),
            Mode = NumericMode.Short
        };

        private static readonly ConcurrentDictionary<decimal, Numeric> Cache =
            new ConcurrentDictionary<decimal, Numeric>();

        private readonly uint _objId;

        private Numeric(uint objId) => _objId = objId;

        private Numeric(VirtualMachine vm) => _objId = vm.NextObjId();

        public bool Mutable { get; private set; } = true;

        public NumericMode Mode { get; private set; } = NumericMode.Short;
        public byte[] Bytes { get; private set; } = BitConverter.GetBytes((short)0);
        public byte ByteValue => GetAs<byte>();
        public short ShortValue => GetAs<short>();
        public int IntValue => GetAs<int>();
        public long LongValue => GetAs<long>();
        public float FloatValue => GetAs<float>();
        public double DoubleValue => GetAs<double>();
        public string StringValue => GetAs<string>();

        public long ObjectId => VirtualMachine.CombineHash(_objId, "num:" + StringValue);
        public bool Primitive => true;
        public TypeRef Type => TypeRef.NumericType(Mode);

        public string ToString(short variant)
        {
            return variant switch
            {
                0 => StringValue,
                -1 => Type.FullName,
                _ => $"{StringValue}{char.ToLower(Mode.ToString()[0])}"
            };
        }

        public static Numeric Constant(VirtualMachine vm, byte value)
        {
            var num = new Numeric(vm);
            num.SetAs(value);
            num.Mutable = false;
            return num;
        }

        public static Numeric Constant(VirtualMachine vm, short value)
        {
            var num = new Numeric(vm);
            num.SetAs(value);
            num.Mutable = false;
            return num;
        }

        public static Numeric Constant(VirtualMachine vm, int value)
        {
            var num = new Numeric(vm);
            num.SetAs(value);
            num.Mutable = false;
            return num;
        }

        public static Numeric Constant(VirtualMachine vm, long value)
        {
            var num = new Numeric(vm);
            num.SetAs(value);
            num.Mutable = false;
            return num;
        }

        public static Numeric Constant(VirtualMachine vm, float value)
        {
            var num = new Numeric(vm);
            num.SetAs(value);
            num.Mutable = false;
            return num;
        }

        public static Numeric Constant(VirtualMachine vm, double value)
        {
            var num = new Numeric(vm);
            num.SetAs(value);
            num.Mutable = false;
            return num;
        }

        private void SetAs<T>(T value)
        {
            if (!Mutable)
                throw new Exception("Numeric is immutable");

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
                throw new Exception("Invalid target Type: " + type);
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

            throw new Exception("Invalid target Type: " + type);
        }

        public static Numeric Compile(VirtualMachine vm, string str)
        {
            var result = NumberRegex.Match(str);

            if (!result.Success)
                throw new Exception("Invalid numeric literal: " + str);
            var groups = result.Groups;
            var type = groups[5].Value;
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
                throw new Exception("Invalid target Type: " + type);
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

        public Numeric OpPlus(VirtualMachine vm, Numeric right)
        {
            switch (Mode)
            {
                case NumericMode.Byte:
                    return Constant(vm, ByteValue + right.ByteValue);
                case NumericMode.Short:
                    return Constant(vm, ShortValue + right.ShortValue);
                case NumericMode.Int:
                    return Constant(vm, IntValue + right.IntValue);
                case NumericMode.Long:
                    return Constant(vm, LongValue + right.LongValue);
                case NumericMode.Float:
                    return Constant(vm, FloatValue + right.FloatValue);
                case NumericMode.Double:
                    return Constant(vm, DoubleValue + right.DoubleValue);
                default:
                    throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
            }
        }

        public Numeric OpMinus(VirtualMachine vm, Numeric right)
        {
            switch (Mode)
            {
                case NumericMode.Byte:
                    return Constant(vm, ByteValue - right.ByteValue);
                case NumericMode.Short:
                    return Constant(vm, ShortValue - right.ShortValue);
                case NumericMode.Int:
                    return Constant(vm, IntValue - right.IntValue);
                case NumericMode.Long:
                    return Constant(vm, LongValue - right.LongValue);
                case NumericMode.Float:
                    return Constant(vm, FloatValue - right.FloatValue);
                case NumericMode.Double:
                    return Constant(vm, DoubleValue - right.DoubleValue);
                default:
                    throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
            }
        }

        public Numeric OpMultiply(VirtualMachine vm, Numeric right)
        {
            switch (Mode)
            {
                case NumericMode.Byte:
                    return Constant(vm, ByteValue * right.ByteValue);
                case NumericMode.Short:
                    return Constant(vm, ShortValue * right.ShortValue);
                case NumericMode.Int:
                    return Constant(vm, IntValue * right.IntValue);
                case NumericMode.Long:
                    return Constant(vm, LongValue * right.LongValue);
                case NumericMode.Float:
                    return Constant(vm, FloatValue * right.FloatValue);
                case NumericMode.Double:
                    return Constant(vm, DoubleValue * right.DoubleValue);
                default:
                    throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
            }
        }

        public Numeric OpDivide(VirtualMachine vm, Numeric right)
        {
            switch (Mode)
            {
                case NumericMode.Byte:
                    return Constant(vm, ByteValue / right.ByteValue);
                case NumericMode.Short:
                    return Constant(vm, ShortValue / right.ShortValue);
                case NumericMode.Int:
                    return Constant(vm, IntValue / right.IntValue);
                case NumericMode.Long:
                    return Constant(vm, LongValue / right.LongValue);
                case NumericMode.Float:
                    return Constant(vm, FloatValue / right.FloatValue);
                case NumericMode.Double:
                    return Constant(vm, DoubleValue / right.DoubleValue);
                default:
                    throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
            }
        }

        public Numeric OpModulus(VirtualMachine vm, Numeric right)
        {
            switch (Mode)
            {
                case NumericMode.Byte:
                    return Constant(vm, ByteValue % right.ByteValue);
                case NumericMode.Short:
                    return Constant(vm, ShortValue % right.ShortValue);
                case NumericMode.Int:
                    return Constant(vm, IntValue % right.IntValue);
                case NumericMode.Long:
                    return Constant(vm, LongValue % right.LongValue);
                case NumericMode.Float:
                    return Constant(vm, FloatValue % right.FloatValue);
                case NumericMode.Double:
                    return Constant(vm, DoubleValue % right.DoubleValue);
                default:
                    throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
            }
        }

        public override string ToString()
        {
            return ToString(0);
        }
    }
}