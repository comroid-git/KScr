using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Lib.Model;

namespace KScr.Lib.Store
{
    public enum VariableContext : byte
    {
        Local,
        This,
        Absolute
    }

    public sealed class CtxBlob
    {
        private string? _this;
#pragma warning disable CS0628
        protected internal CtxBlob(string local)
        {
            Local = local;
        }
        public CtxBlob? Parent { get; protected internal set; }
        public string Local { get; protected internal set; }

        public string This
        {
            get => _this ?? Parent!.This;
            protected internal set => _this = value;
        }

        public ObjectRef? It { get; protected internal set; }
#pragma warning restore CS0628
    }

    public sealed class Stack
    {
        public const string Delimiter = ".";
        private readonly List<CtxBlob> _dequeue = new();
        private string _local => _dequeue.Last().Local;
        private string _this => _dequeue.Last().This;
        public ObjectRef? This => _dequeue.Last().It;
        public string PrefixLocal => _local + Delimiter;
        public string PrefixThis => _this + Delimiter;

        public IEnumerable<string> CreateKeys(VariableContext varctx, string name)
        {
            var me = varctx switch
            {
                VariableContext.Local => PrefixLocal + name,
                VariableContext.This => PrefixThis + name,
                VariableContext.Absolute => name,
                _ => throw new ArgumentOutOfRangeException(nameof(varctx), varctx, null)
            };
            CtxBlob? parent;
            var arr = new[] { me };
            if (_dequeue.Count > 0 && (parent = _dequeue.Last().Parent) != null)
                return arr.Append(varctx switch
                {
                    VariableContext.Local => parent.Local + Delimiter + name,
                    VariableContext.This => parent.Local + Delimiter + name,
                    VariableContext.Absolute => name,
                    _ => throw new ArgumentOutOfRangeException(nameof(varctx), varctx, null)
                });
            return arr;
        }

        public void StepInside(string sub)
        {
            _dequeue.Add(new CtxBlob(sub)
            {
                Local = sub,
                Parent = _dequeue.Last()
            });
        }
        
        // put focus into static class
        public void StepDown(IClassInfo into, object? local = null /*todo implement memberref type*/)
        {
            _dequeue.Add(new CtxBlob(local?.ToString() ?? "static" + into.FullName)
            {
                This = into.FullName
            });
        }

        // put focus into object instance
        public void StepDown(ObjectRef into, object local /*todo implement memberref type*/)
        {
            var o = into.Value!;
            _dequeue.Add(new CtxBlob(local.ToString()!)
            {
                This = o.Type.FullName + '#' + o.ObjectId,
                It = into
            });
        }

        public void StepUp()
        {
            _dequeue.RemoveAt(_dequeue.Count - 1);
        }
    }
}