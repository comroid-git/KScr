using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Exception;
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
        protected internal List<string> _keys = new List<string>();
#pragma warning disable CS0628
        protected internal CtxBlob(SourcefilePosition callLocation, string local)
        {
            CallLocation = callLocation;
            Local = local;
        }

        public SourcefilePosition CallLocation { get; }
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
            if(varctx == VariableContext.Local && _dequeue.Count > 0 && !_dequeue[^1]._keys.Contains(me))
                _dequeue[^1]._keys.Add(me);
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

        public void StepInside<T>(RuntimeBase vm, SourcefilePosition callLocation, string sub, ref T t, Func<T,T> exec)
        {
            _dequeue.Add(new CtxBlob(callLocation, PrefixLocal + sub)
            {
                Local = sub,
                Parent = _dequeue.Last()
            });
            WrapExecution(vm, ref t, exec);
        }
        
        // put focus into static class
        public void StepInto<T>(RuntimeBase vm, SourcefilePosition callLocation, IClass into, object local, ref T t, Func<T,T> exec)
        {
            _dequeue.Add(new CtxBlob(callLocation, PrefixLocal + local)
            {
                Local = local.ToString() ?? string.Empty,
                Class = into,
                It = into.SelfRef
            });
            WrapExecution(vm, ref t, exec);
        }

        // put focus into object instance
        public void StepInto<T>(RuntimeBase vm, SourcefilePosition callLocation, ObjectRef into, object local, ref T t, Func<T,T> exec)
        {
            _dequeue.Add(new CtxBlob(callLocation, PrefixLocal + local)
            {
                Local = local.ToString() ?? string.Empty,
                Class = into.Value!.Type,
                It = into,
            });
            WrapExecution(vm, ref t, exec);
        }
        
        public readonly List<StackTraceException> StackTrace = new();

        private void WrapExecution<T>(RuntimeBase vm, ref T t, Func<T,T> exec)
        {
            try
            {
                t = exec(t);
            }
            catch (StackTraceException ex)
            {
                var next = new StackTraceException(_dequeue[^1].CallLocation, _local, ex);
                StackTrace.Add(next);
#pragma warning disable CA2200
                // ReSharper disable once PossibleIntendedRethrow
                throw ex;
#pragma warning restore CA2200
            }
            catch (System.Exception ex)
            {
                throw new StackTraceException(_dequeue[^1].CallLocation, _local, ex);
            }
            finally
            {
                StepUp(vm);
            }
        }

        private void StepUp(RuntimeBase vm)
        {
            var it = _dequeue[^1];
            foreach (string old in it._keys)
                vm.ObjectStore.Remove(old);
            _dequeue.RemoveAt(_dequeue.Count - 1);
                
        }
    }
}