using KScr.Lib.Core;

namespace KScr.Lib.Store
{
    public enum VariableContext
    {
        Local,
        This,
        Absolute
    }

    public sealed class Context
    {
        public const string Delimiter = ".";
        private string _local;
        private string _this;

        public string PrefixLocal => _local + Delimiter;
        public string PrefixThis => _this + Delimiter;

        public void Refocus(ClassRef into, string local /*todo implement memberref type*/)
        {
            _local = local;
            _this = into.TypeId.ToString();
        }

        public void Refocus(IObject into, string local /*todo implement memberref type*/)
        {
            _local = local;
            _this = into.ObjectId.ToString();
        }
    }
}