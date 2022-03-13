using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using String = System.String;

namespace KScr.Lib.Store
{
    public enum VariableContext : byte
    {
        Local,
        This,
        Absolute
    }

    public struct CallLocation
    {
        public CallLocation(SourcefilePosition srcPos)
        {
            SourceName = srcPos.SourcefilePath;
            SourceLine = srcPos.SourcefileLine;
            SourceCursor = srcPos.SourcefileCursor;
        }

        public string SourceName { get; set; }
        public int SourceLine { get; set; }
        public int SourceCursor { get; set; }
    }

    public sealed class CtxBlob
    {
        protected internal List<string> _keys = new List<string>();
#pragma warning disable CS0628
        protected internal CtxBlob(CallLocation callLocation, string local)
        {
            CallLocation = callLocation;
            Local = local;
        }

        public CallLocation CallLocation { get; }
        public CtxBlob? Parent { get; protected internal set; }
        public string Local { get; protected internal set; }
        public ObjectRef? It { get; protected internal set; }
        public IClass? Class { get; protected internal set; }
        public bool IsSub { get; protected internal set; }
#pragma warning restore CS0628
    }

    public sealed class Stack
    {
        public const string Delimiter = ".";
        private readonly List<CtxBlob> _dequeue = new();
        private string _local => _dequeue.Count == 0 ? "org.comroid.kscr.core.Object.main()" : _dequeue[^1].Local;
        // todo fixme: these two need to go up until they find something
        public ObjectRef? This => _dequeue[^1].It ??  _dequeue[^2].It;
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
            if(varctx == VariableContext.Local && _dequeue.Count > 0 && !_dequeue[^1].IsSub && !_dequeue[^1]._keys.Contains(me))
                _dequeue[^1]._keys.Add(me); // cache in stack for cleanup
            CtxBlob? parent;
            var arr = new[] { me };
            if (_dequeue.Count > 0 && (parent = _dequeue[^1].Parent) != null)
                return arr.Append(varctx switch
                {
                    VariableContext.Local => parent.Local + Delimiter + name,
                    VariableContext.Absolute => name,
                    _ => throw new ArgumentOutOfRangeException(nameof(varctx), varctx, null)
                }).Distinct();
            return arr;
        }

        public void StepInside(RuntimeBase vm, SourcefilePosition srcPos, string sub, ref ObjectRef t, Func<ObjectRef,ObjectRef> exec)
        {
            _dequeue.Add(new CtxBlob(new CallLocation
            {
                SourceName = _local + ".." + sub,
                SourceLine = srcPos.SourcefileLine,
                SourceCursor = srcPos.SourcefileCursor
            }, _local)
            {
                Parent = _dequeue.Last(),
                IsSub = true,
                It = This
            });
            WrapExecution(vm, ref t, exec);
        }
        
        // put focus into static class
        public void StepInto(RuntimeBase vm, SourcefilePosition srcPos, IClassMember local, ref ObjectRef t, Func<ObjectRef,ObjectRef> exec)
        {
            IClass cls = local.Parent;
            string localStr = cls.FullName + '.' + local.Name + (local is IMethod mtd
                ? '(' + string.Join(", ", mtd.Parameters.Select(mp => $"{mp.Type.Name} {mp.Name}")) + ')'
                : string.Empty);
            _dequeue.Add(new CtxBlob(new CallLocation
            {
                SourceName = _local,
                SourceLine = srcPos.SourcefileLine,
                SourceCursor = srcPos.SourcefileCursor
            }, PrefixLocal + local)
            {
                Local = localStr,
                Class = cls,
                It = cls.SelfRef
            });
            WrapExecution(vm, ref t, exec);
        }

        // put focus into object instance
        public void StepInto(RuntimeBase vm, SourcefilePosition srcPos, ObjectRef? into, IClassMember local, ref ObjectRef t, Func<ObjectRef,ObjectRef> exec)
        {
            into ??= vm.ConstantVoid;
            var cls = local.Parent;
            string localStr = cls.FullName + '#' + (into.Value??IObject.Null).ObjectId.ToString("X") 
                              + '.' + local.Name + (local is IMethod mtd
                                  ? '(' + string.Join(", ", mtd.Parameters.Select(mp => $"{mp.Type.Name} {mp.Name}")) + ')'
                                  : string.Empty);
            _dequeue.Add(new CtxBlob(new CallLocation
            {
                SourceName = _local,
                SourceLine = srcPos.SourcefileLine,
                SourceCursor = srcPos.SourcefileCursor
            }, PrefixLocal + cls.Name)
            {
                Local = localStr,
                Class = into.Value!.Type,
                It = into
            });
            WrapExecution(vm, ref t, exec);
        }
        
        public readonly List<StackTraceException> StackTrace = new();

        private void WrapExecution(RuntimeBase vm, ref ObjectRef rev, Func<ObjectRef,ObjectRef> exec)
        {
            try
            {
                rev = exec(rev);
            }
            catch (StackTraceException ex)
            {
                var next = new StackTraceException(_dequeue[^1].CallLocation, _local, ex);
                StackTrace.Add(next);
#pragma warning disable CA2200
                // ReSharper disable once PossibleIntendedRethrow
                throw ex;
            }
#if !DEBUG
            catch (System.Exception ex)
            {
                //if (RuntimeBase.DebugMode)
                    // ReSharper disable once PossibleIntendedRethrow
                //    throw ex;
#pragma warning restore CA2200
                throw new StackTraceException(_dequeue[^1].CallLocation, _local, ex);
            }
#endif
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