using System;

namespace KScr.Core;

public static class RuntimeVersion
{
    public static Version Current => V_0_3_0;
    public static readonly Version V_0_1_0 = new("0.1.0");
    public static readonly Version V_0_1_1 = new("0.1.1");
    public static readonly Version V_0_2_0 = new("0.2.0");
    public static readonly Version V_0_2_1 = new("0.2.1");
    public static readonly Version V_0_2_2 = new("0.2.2");
    public static readonly Version V_0_3_0 = new("0.3.0");
}

public static class BytecodeVersion
{
    public static Version Current => V_0_10;
    public static readonly Version V_0_10 = new("0.10");
}
