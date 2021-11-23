using System.Collections.Generic;
using System.Linq;
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

    public struct CtxBlob
    {
        public string Local;
        public long This;
        public ObjectRef? It;
    }

    public sealed class Context
    {
        public const string Delimiter = ".";
        private readonly List<CtxBlob> _blobs = new List<CtxBlob>();
        private string _local => _blobs.Last().Local;
        private long _this => _blobs.Last().This;
        public ObjectRef? This => _blobs.Last().It;
        public string PrefixLocal => _local + Delimiter;
        public string PrefixThis => _this + Delimiter;

        // put focus into static class
        public void Refocus(IClassRef into, object? local = null /*todo implement memberref type*/)
        {
            _blobs.Add(new CtxBlob()
            {
                Local = local?.ToString() ?? "static"+into.TypeId,
                This = into.TypeId,
                It = null
            });
        }

        // put focus into object instance
        public void Refocus(ObjectRef into, object local /*todo implement memberref type*/)
        {
            _blobs.Add(new CtxBlob()
            {
                Local = local.ToString()!, 
                This = into.Value?.ObjectId ?? long.MinValue,
                It = into
            });
        }

        public void RevertFocus() => _blobs.RemoveAt(_blobs.Count - 1);
    }
}