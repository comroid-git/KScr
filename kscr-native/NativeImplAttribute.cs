using KScr.Lib;
using KScr.Lib.Core;
using KScr.Lib.Store;

namespace KScr.Native;

public delegate IObjectRef? NativeImplMember(RuntimeBase vm, Stack stack, params IObject[] args);

public sealed class NativeImplAttribute : Attribute
{
    public string? Package;
    public string? ClassName;
    public string? MemberName;
}