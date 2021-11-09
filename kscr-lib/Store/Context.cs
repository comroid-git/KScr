using KScr.Lib.Core;
using KScr.Lib.Model;

namespace KScr.Lib.Store
{
    public enum VariableContext : byte
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
        public ObjectRef? This { get; private set; }

        // put focus into static class
        public void Refocus(IClassRef into, string local /*todo implement memberref type*/)
        {
            _local = local;
            _this = into.TypeId.ToString();
            This = null;
        }

        // put focus into object instance
        public void Refocus(ObjectRef into, string local /*todo implement memberref type*/)
        {
            _local = local;
            _this = into.Value?.ObjectId.ToString() ?? long.MinValue.ToString();
            This = into;
        }
    }
}