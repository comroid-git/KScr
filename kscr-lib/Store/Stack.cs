using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Lib.Bytecode;
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
#pragma warning disable CS0628
        protected internal CtxBlob(string local)
        {
            Local = local;
        }
        public CtxBlob? Parent { get; protected internal set; }
        public string Local { get; protected internal set; }

        public ObjectRef? It { get; protected internal set; }
        public IClass? Class { get; protected internal set; }
#pragma warning restore CS0628
    }

    public sealed class Stack
    {
        public const string Delimiter = ".";
        private readonly List<CtxBlob> _dequeue = new();
        private string _local => _dequeue.Count == 0 ? string.Empty : _dequeue[^1].Local;
        public ObjectRef? This => _dequeue[^1].It;
        public IClass? Class => _dequeue[^1].Class ?? _dequeue[^2].Class;
        public string PrefixLocal => _local + Delimiter;
        public List<MethodParameter>? MethodParams { get; set; }

        public IEnumerable<string> CreateKeys(VariableContext varctx, string name)
        {
            var me = varctx switch
            {
                VariableContext.Local => PrefixLocal + name,
                VariableContext.Absolute => name,
                _ => throw new ArgumentOutOfRangeException(nameof(varctx), varctx, null)
            };
            CtxBlob? parent;
            var arr = new[] { me };
            if (_dequeue.Count > 0 && (parent = _dequeue[^1].Parent) != null)
                return arr.Append(varctx switch
                {
                    VariableContext.Local => parent.Local + Delimiter + name,
                    VariableContext.Absolute => name,
                    _ => throw new ArgumentOutOfRangeException(nameof(varctx), varctx, null)
                });
            return arr;
        }

        public void StepInside(string sub)
        {
            _dequeue.Add(new CtxBlob(PrefixLocal + sub)
            {
                Local = sub,
                Parent = _dequeue.Last()
            });
        }
        
        // put focus into static class
        public void StepDown(IClass into, object? local = null /*todo implement memberref type*/)
        {
            _dequeue.Add(new CtxBlob(PrefixLocal + local)
            {
                Local = local?.ToString() ?? string.Empty,
                Class = into
            });
        }

        // put focus into object instance
        public void StepDown(ObjectRef into, object local /*todo implement memberref type*/)
        {
            _dequeue.Add(new CtxBlob(PrefixLocal + local)
            {
                Local = local.ToString() ?? string.Empty,
                Class = into.Value!.Type,
                It = into,
            });
        }

        public void StepUp()
        {
            _dequeue.RemoveAt(_dequeue.Count - 1);
        }
    }
}