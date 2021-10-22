namespace KScr.Lib.Bytecode
{
    public sealed class Class : AbstractPackageMember
    {
        private Class()
        {
        }

        internal Class(Package parent, string name) : base(parent, name)
        {
        }
    }
}