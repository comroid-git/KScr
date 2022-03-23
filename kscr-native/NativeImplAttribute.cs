namespace KScr.Native;

public sealed class NativeImplAttribute : Attribute
{
    public string? Package;
    public string? ClassName;
    public string? MemberName;
}