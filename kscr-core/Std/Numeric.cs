using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Store;

namespace KScr.Core.Std;

public enum NumericMode
{
    Int,
    IntN,
    Float,
    Double
}

public class Numeric : NativeObj
{
    public static readonly Regex NumberRegex = new(@"([\d]+(i|l|f|d|s|b)?)([.,]([\d]+)(f|d))?");
    public static readonly Numeric Zero = new(0)
    {
        Mutable = false,
        Bytes = BitConverter.GetBytes((byte)0),
        Mode = NumericMode.Int
    };
    public static readonly Numeric One = new(1)
    {
        Mutable = false,
        Bytes = BitConverter.GetBytes((byte)1),
        Mode = NumericMode.Int
    };

    protected virtual bool _constant { get; }

    private Numeric(byte value) : base((uint)(value + 1))
    {
    }

    internal Numeric(RuntimeBase vm, bool constant = false) : base(vm.NextObjId())
    {
        _constant = constant;
    }

    [Obsolete]
    public bool Mutable => !_constant;

    public NumericMode Mode { get; protected set; } = NumericMode.Int;
    public byte[] Bytes { get; protected set; } = BitConverter.GetBytes((short)0);
    public IntN IntNValue => GetAs<IntN>();
    public float FloatValue => GetAs<float>();
    public double DoubleValue => GetAs<double>();
    public string StringValue => GetAs<string>();
    public bool Primitive => true;
    public bool ImplicitlyFalse => FloatValue <= 0;

    public override IClassInstance Type => Class._NumericType(Mode);

    public override string ToString(short variant)
    {
        return variant switch
        {
            0 => StringValue,
            -1 => Type.FullName,
            _ => $"{StringValue}{char.ToLower(Mode.ToString()[0])}"
        };
    }

    public override Stack InvokeNative(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
    {
        switch (member)
        {
            case "toString":
            case "Message":
                stack[StackOutput.Default] = String.Instance(vm, StringValue);
                break;
            case "ExitCode":
                stack[StackOutput.Default] = Constant(vm, GetAs<int>());
                break;
            case "equals":
                if (args[0] is not Numeric other)
                {
                    stack[StackOutput.Default] = vm.ConstantFalse;
                    break;
                }

                if (Mode is NumericMode.Float or NumericMode.Double && args.Length == 2 &&
                    args[1] is not Numeric delta)
                    throw new FatalException("Invalid second argument; expected: num delta");
                delta = (Constant(vm, 0.001).Value as Numeric)!;
                stack[StackOutput.Default] = Mode switch
                {
                    NumericMode.Int => IntValue == other.IntValue,
                    NumericMode.Float => Math.Abs(FloatValue - other.FloatValue) < delta.FloatValue,
                    NumericMode.Double => Math.Abs(DoubleValue - other.DoubleValue) < delta.DoubleValue,
                    _ => throw new ArgumentOutOfRangeException()
                }
                    ? vm.ConstantTrue
                    : vm.ConstantFalse;
                break;
            case "getType":
                stack[StackOutput.Default] = Type.SelfRef;
                break;
            default: throw new NotImplementedException(member);
        }

        return stack;
    }

    public override string GetKey()
    {
        return Mode switch
        {
            NumericMode.Int => CreateKey(LongValue),
            NumericMode.Float => CreateKey(FloatValue),
            NumericMode.Double => CreateKey(DoubleValue),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static IObjectRef Constant(RuntimeBase vm, byte value)
    {
        return vm.ComputeObject(RuntimeBase.MainStack, VariableContext.Absolute, CreateKey(value), () =>
        {
            var num = new Numeric(vm, true)
            {
                Bytes = BitConverter.GetBytes(value),
                Mode = NumericMode.Byte
            };
            return num;
        });
    }

    public static IObjectRef Constant(RuntimeBase vm, short value)
    {
        return vm.ComputeObject(RuntimeBase.MainStack, VariableContext.Absolute, CreateKey(value), () =>
        {
            var num = new Numeric(vm, true)
            {
                Bytes = BitConverter.GetBytes(value),
                Mode = NumericMode.Short
            };
            return num;
        });
    }

    public static IObjectRef Constant(RuntimeBase vm, int value)
    {
        return vm.ComputeObject(RuntimeBase.MainStack, VariableContext.Absolute, CreateKey(value), () =>
        {
            var num = new Numeric(vm, true)
            {
                Bytes = BitConverter.GetBytes(value),
                Mode = NumericMode.Int
            };
            return num;
        });
    }

    public static IObjectRef Constant(RuntimeBase vm, long value)
    {
        return vm.ComputeObject(RuntimeBase.MainStack, VariableContext.Absolute, CreateKey(value), () =>
        {
            var num = new Numeric(vm, true)
            {
                Bytes = BitConverter.GetBytes(value),
                Mode = NumericMode.Long
            };
            return num;
        });
    }

    public static IObjectRef Constant(RuntimeBase vm, float value)
    {
        return vm.ComputeObject(RuntimeBase.MainStack, VariableContext.Absolute, CreateKey(value), () =>
        {
            var num = new Numeric(vm, true)
            {
                Bytes = BitConverter.GetBytes(value),
                Mode = NumericMode.Float
            };
            return num;
        });
    }

    public static IObjectRef Constant(RuntimeBase vm, double value)
    {
        return vm.ComputeObject(RuntimeBase.MainStack, VariableContext.Absolute, CreateKey(value), () =>
        {
            var num = new Numeric(vm, true)
            {
                Bytes = BitConverter.GetBytes(value),
                Mode = NumericMode.Double
            };
            return num;
        });
    }

    [Obsolete]
    private void SetAs<T>(T value)
    {
        if (_constant)
            throw new FatalException("Numeric is constant");

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
            throw new FatalException("Invalid target Type: " + type);
        }
    }

    private T GetAs<T>()
    {
        switch (Mode, typeof(T).Name)
        {
            case (NumericMode.Int, "byte"):
            case (NumericMode.IntN, "byte"):
            case (NumericMode.Float, "byte"):
            case (NumericMode.Double, "byte"):
                break;
        }

        if (type == typeof(byte))
        {
            switch (Mode)
            {
                case NumericMode.Int:
                    return (T)(object)(byte)IntNValue.IntValue;
                case NumericMode.IntN:
                    return (T)(object)
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
                    return (T)(object)FloatValue.ToString(CultureInfo.InvariantCulture);
                case NumericMode.Double:
                    return (T)(object)DoubleValue.ToString(CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode);
            }

        throw new FatalException("Invalid target Type: " + type);
    }

    public static IObjectRef Compile(RuntimeBase vm, string str)
    {
        var result = NumberRegex.Match(str);

        if (!result.Success)
            throw new FatalException("Invalid numeric literal: " + str);
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
        if (type == "d")
            return Constant(vm, double.Parse(str.Substring(0, str.Length - 1), CultureInfo.InvariantCulture));
        if (type == "f")
            return Constant(vm, float.Parse(str.Substring(0, str.Length - 1), CultureInfo.InvariantCulture));
        if (type != string.Empty)
            throw new FatalException("Invalid target Type: " + type);
        if (groups[3].Length > groups[4].Length)
            return Constant(vm, float.Parse(str, CultureInfo.InvariantCulture));
        try
        {
            return Constant(vm, int.Parse(str));
        }
        catch (OverflowException ignored)
        {
            return Constant(vm, long.Parse(str));
        }
    }

    public IObjectRef Operator(RuntimeBase vm, Operator op, Numeric? right = null)
    {
        return ((op & Model.Operator.Compound) != 0 ? op ^ Model.Operator.Compound : op) switch
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
            Model.Operator.Pow => OpCircumflex(vm, right!),
            Model.Operator.Greater => OpGt(vm, right!),
            Model.Operator.GreaterEq => OpGtEq(vm, right!),
            Model.Operator.Lesser => OpLs(vm, right!),
            Model.Operator.LesserEq => OpLsEq(vm, right!),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }

    public IObjectRef OpIR(RuntimeBase vm)
    {
        return OpPlus(vm, One);
    }

    public IObjectRef OpRI(RuntimeBase vm)
    {
        //var rev = OpMultiply(vm, One);
        //SetAs((OpPlus(vm, One).Value as Numeric)!.DoubleValue);
        return OpPlus(vm, One);
    }

    public IObjectRef OpDR(RuntimeBase vm)
    {
        return OpMinus(vm, One);
    }

    public IObjectRef OpRD(RuntimeBase vm)
    {
        //var rev = OpMultiply(vm, One);
        //SetAs((OpMinus(vm, One).Value as Numeric)!.DoubleValue);
        return OpMinus(vm, One);
    }

    public IObjectRef OpGt(RuntimeBase vm, Numeric right)
    {
        return Mode switch
        {
            NumericMode.Byte => ByteValue > right.ByteValue,
            NumericMode.Short => ShortValue > right.ShortValue,
            NumericMode.Int => IntValue > right.IntValue,
            NumericMode.Long => LongValue > right.LongValue,
            NumericMode.Float => FloatValue > right.FloatValue,
            NumericMode.Double => DoubleValue > right.DoubleValue,
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        }
            ? vm.ConstantTrue
            : vm.ConstantFalse;
    }

    public IObjectRef OpGtEq(RuntimeBase vm, Numeric right)
    {
        return Mode switch
        {
            NumericMode.Byte => ByteValue >= right.ByteValue,
            NumericMode.Short => ShortValue >= right.ShortValue,
            NumericMode.Int => IntValue >= right.IntValue,
            NumericMode.Long => LongValue >= right.LongValue,
            NumericMode.Float => FloatValue >= right.FloatValue,
            NumericMode.Double => DoubleValue >= right.DoubleValue,
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        }
            ? vm.ConstantTrue
            : vm.ConstantFalse;
    }

    public IObjectRef OpLs(RuntimeBase vm, Numeric right)
    {
        return Mode switch
        {
            NumericMode.Byte => ByteValue < right.ByteValue,
            NumericMode.Short => ShortValue < right.ShortValue,
            NumericMode.Int => IntValue < right.IntValue,
            NumericMode.Long => LongValue < right.LongValue,
            NumericMode.Float => FloatValue < right.FloatValue,
            NumericMode.Double => DoubleValue < right.DoubleValue,
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        }
            ? vm.ConstantTrue
            : vm.ConstantFalse;
    }

    public IObjectRef OpLsEq(RuntimeBase vm, Numeric right)
    {
        return Mode switch
        {
            NumericMode.Byte => ByteValue <= right.ByteValue,
            NumericMode.Short => ShortValue <= right.ShortValue,
            NumericMode.Int => IntValue <= right.IntValue,
            NumericMode.Long => LongValue <= right.LongValue,
            NumericMode.Float => FloatValue <= right.FloatValue,
            NumericMode.Double => DoubleValue <= right.DoubleValue,
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        }
            ? vm.ConstantTrue
            : vm.ConstantFalse;
    }

    public IObjectRef OpNegate(RuntimeBase vm)
    {
        return Mode switch
        {
            NumericMode.Byte => Constant(vm, -ByteValue),
            NumericMode.Short => Constant(vm, -ShortValue),
            NumericMode.Int => Constant(vm, -IntValue),
            NumericMode.Long => Constant(vm, -LongValue),
            NumericMode.Float => Constant(vm, -FloatValue),
            NumericMode.Double => Constant(vm, -DoubleValue),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public IObjectRef OpPlus(RuntimeBase vm, Numeric right)
    {
        return Mode switch
        {
            NumericMode.Byte => Constant(vm, ByteValue + right.ByteValue),
            NumericMode.Short => Constant(vm, ShortValue + right.ShortValue),
            NumericMode.Int => Constant(vm, IntValue + right.IntValue),
            NumericMode.Long => Constant(vm, LongValue + right.LongValue),
            NumericMode.Float => Constant(vm, FloatValue + right.FloatValue),
            NumericMode.Double => Constant(vm, DoubleValue + right.DoubleValue),
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        };
    }

    public IObjectRef OpMinus(RuntimeBase vm, Numeric right)
    {
        return Mode switch
        {
            NumericMode.Byte => Constant(vm, ByteValue - right.ByteValue),
            NumericMode.Short => Constant(vm, ShortValue - right.ShortValue),
            NumericMode.Int => Constant(vm, IntValue - right.IntValue),
            NumericMode.Long => Constant(vm, LongValue - right.LongValue),
            NumericMode.Float => Constant(vm, FloatValue - right.FloatValue),
            NumericMode.Double => Constant(vm, DoubleValue - right.DoubleValue),
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        };
    }

    public IObjectRef OpMultiply(RuntimeBase vm, Numeric right)
    {
        return Mode switch
        {
            NumericMode.Byte => Constant(vm, ByteValue * right.ByteValue),
            NumericMode.Short => Constant(vm, ShortValue * right.ShortValue),
            NumericMode.Int => Constant(vm, IntValue * right.IntValue),
            NumericMode.Long => Constant(vm, LongValue * right.LongValue),
            NumericMode.Float => Constant(vm, FloatValue * right.FloatValue),
            NumericMode.Double => Constant(vm, DoubleValue * right.DoubleValue),
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        };
    }

    public IObjectRef OpDivide(RuntimeBase vm, Numeric right)
    {
        return Mode switch
        {
            NumericMode.Byte => Constant(vm, ByteValue / right.ByteValue),
            NumericMode.Short => Constant(vm, ShortValue / right.ShortValue),
            NumericMode.Int => Constant(vm, IntValue / right.IntValue),
            NumericMode.Long => Constant(vm, LongValue / right.LongValue),
            NumericMode.Float => Constant(vm, FloatValue / right.FloatValue),
            NumericMode.Double => Constant(vm, DoubleValue / right.DoubleValue),
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        };
    }

    public IObjectRef OpModulus(RuntimeBase vm, Numeric right)
    {
        return Mode switch
        {
            NumericMode.Byte => Constant(vm, ByteValue % right.ByteValue),
            NumericMode.Short => Constant(vm, ShortValue % right.ShortValue),
            NumericMode.Int => Constant(vm, IntValue % right.IntValue),
            NumericMode.Long => Constant(vm, LongValue % right.LongValue),
            NumericMode.Float => Constant(vm, FloatValue % right.FloatValue),
            NumericMode.Double => Constant(vm, DoubleValue % right.DoubleValue),
            _ => throw new ArgumentOutOfRangeException(nameof(Mode), "Unexpected NumericMode: " + Mode)
        };
    }

    public IObjectRef OpCircumflex(RuntimeBase vm, Numeric right)
    {
        var rev = OpMultiply(vm, One);
        for (var n = right.IntValue; n > 0; n--)
            rev = (rev.Value as Numeric)!.OpMultiply(vm, this);
        return rev;
    }

    public IObjectRef Sqrt(RuntimeBase vm)
    {
        switch (Mode)
        {
            case NumericMode.Byte:
            case NumericMode.Short:
            case NumericMode.Int:
            case NumericMode.Long:
                return Constant(vm, Math.Sqrt(LongValue));
            case NumericMode.Float:
                return Constant(vm, MathF.Sqrt(FloatValue));
            case NumericMode.Double:
                return Constant(vm, Math.Sqrt(DoubleValue));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public IObjectRef Sin(RuntimeBase vm)
    {
        switch (Mode)
        {
            case NumericMode.Byte:
            case NumericMode.Short:
            case NumericMode.Int:
            case NumericMode.Long:
                return Constant(vm, Math.Sin(LongValue));
            case NumericMode.Float:
                return Constant(vm, MathF.Sin(FloatValue));
            case NumericMode.Double:
                return Constant(vm, Math.Sin(DoubleValue));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public IObjectRef Cos(RuntimeBase vm)
    {
        switch (Mode)
        {
            case NumericMode.Byte:
            case NumericMode.Short:
            case NumericMode.Int:
            case NumericMode.Long:
                return Constant(vm, Math.Cos(LongValue));
            case NumericMode.Float:
                return Constant(vm, MathF.Cos(FloatValue));
            case NumericMode.Double:
                return Constant(vm, Math.Cos(DoubleValue));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public IObjectRef Tan(RuntimeBase vm)
    {
        switch (Mode)
        {
            case NumericMode.Byte:
            case NumericMode.Short:
            case NumericMode.Int:
            case NumericMode.Long:
                return Constant(vm, Math.Tan(LongValue));
            case NumericMode.Float:
                return Constant(vm, MathF.Tan(FloatValue));
            case NumericMode.Double:
                return Constant(vm, Math.Tan(DoubleValue));
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override string ToString()
    {
        return ToString(0);
    }

    public static string CreateKey(string num)
    {
        return "num:" + num;
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

public sealed class IntN : Numeric
{
    public const int NSteps = 8;
    //private static readonly ConcurrentDictionary<decimal, IntN> Cache = new();
    private static readonly Dictionary<int, IClassInstance> _clsInsts = new();
    private readonly IClassInstance _clsInst;

    public int N
    {
        get => Bytes.Length * NSteps;
        set
        {
            var arr = new byte[value / NSteps];
            Bytes.CopyTo(arr, 0);
            Bytes = arr;
        }
    }

    public IntN(RuntimeBase vm, int n = 32) : base(vm)
    {
        if (n / 8 > chars.Length)
            throw new FatalException($"n too large: {n} ({n / 8})");
        N = n;
        Bytes = new byte[(int)Math.Ceiling((double)n / 8)];
        _clsInst = GetClsInst(vm, n);
    }

    private byte[] Bits
    {
        get
        {
            byte[] bits = new byte[N];
            for (int i = 0; i < N; i++)
                bits[i] = (byte)((Bytes[i / 8] & (1 << i % 8 - 1)) != 0 ? 1 : 0);
            bits = bits.Reverse().ToArray();
            return bits;
        }
    }

    public byte ByteValue
    {
        get => Bytes[0];
        set => Set(new[] { value });
    }

    public short ShortValue
    {
        get => BitConverter.ToInt16(Bytes);
        set => Set(BitConverter.GetBytes(value));
    }

    public int IntValue
    {
        get => BitConverter.ToInt32(Bytes);
        set => Set(BitConverter.GetBytes(value));
    }

    public long LongValue
    {
        get => BitConverter.ToInt64(Bytes);
        set => Set(BitConverter.GetBytes(value));
    }

    public BigInteger BigIntValue
    {
        get => new(Bytes);
        set => Set(value.ToByteArray());
    }

    public new string StringValue(byte radix = 10)
    {
        var str = Convert.ToString(LongValue, radix);
        return radix switch
        {
            2 => "0b" + str,
            8 => "0o" + str,
            10 => str,
            16 => "0x" + str,
            _ => "0_" + str
        };
    }

    private void Set(byte[] bytes)
    {
        if (bytes.Length > N)
            throw new ArgumentOutOfRangeException(nameof(bytes.Length), bytes.Length, "Set value too large");
        Array.Fill(Bytes, (byte)0);
        Array.Copy(bytes, 0, Bytes, N - bytes.Length, bytes.Length);
    }

    public override IClassInstance Type => _clsInst;

    public override string ToString(short variant) => StringValue((byte)(variant == 0 ? 10 : variant));

    public Stack Invoke(RuntimeBase vm, Stack stack, string member, params IObject?[] args)
    {
        throw new NotImplementedException();
    }

    public override string GetKey() => $"int<{N}>: {StringValue(16)}";

    private IClassInstance GetClsInst(RuntimeBase vm, int n)
    {
        if (_clsInsts.ContainsKey(n))
            return _clsInsts[n];
        return _clsInsts[n] = Class.IntType.CreateInstance(vm, Class.IntType,
            new TypeParameter(N.ToString(), TypeParameterSpecializationType.N));
    }
}