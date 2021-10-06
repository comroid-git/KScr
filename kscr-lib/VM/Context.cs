namespace KScr.Lib.VM
{
    public enum VariableContext
    {
        Local,
        This,
        Relative,
        Absolute
    }
    
    public sealed class Context
    {
        public const string Delimiter = ".";
        public string Local { get; internal set; } = "";
        public string This { get; internal set; } = "this";
        public string Relative { get; internal set; } = "§";
    }
}