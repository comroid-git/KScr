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
        public string This;
        public ObjectRef? It;
    }

    public sealed class Stack
    {
        public const string Delimiter = ".";
        private readonly List<CtxBlob> _dequeue = new List<CtxBlob>();
        private string _local => _dequeue.Last().Local;
        private string _this => _dequeue.Last().This;
        public ObjectRef? This => _dequeue.Last().It;
        public string PrefixLocal => _local + Delimiter;
        public string PrefixThis => _this + Delimiter;

        // put focus into static class
        public void StepDown(IClassInstance into, object? local = null /*todo implement memberref type*/)
        {
            _dequeue.Add(new CtxBlob()
            {
                Local = local?.ToString() ?? "static"+into.FullName,
                This = into.FullName,
                It = null
            });
        }

        // put focus into object instance
        public void StepDown(ObjectRef into, object local /*todo implement memberref type*/)
        {
            var o = into.Value!;
            _dequeue.Add(new CtxBlob
            {
                Local = local.ToString()!, 
                This = o.Type.FullName + '#' + o.ObjectId,
                It = into
            });
        }

        public void StepUp() => _dequeue.RemoveAt(_dequeue.Count - 1);
    }
}