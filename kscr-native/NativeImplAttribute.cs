namespace KScr.Native;

public sealed class NativeImplAttribute : Attribute
{
    public string? ClassName;
    public string? MemberName;
    public string? Package;
}