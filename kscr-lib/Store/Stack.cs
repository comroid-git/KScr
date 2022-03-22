using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;
// ReSharper disable ConditionIsAlwaysTrueOrFalse

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
#pragma warning disable CS0628
        protected internal CtxBlob(CallLocation callLocation, string local)
        {
            CallLocation = callLocation;
            Local = local;
        }

        public CallLocation? CallLocation { get; }
        public CtxBlob? Parent { get; protected internal set; }
        public string Local { get; protected internal set; }
        public IObjectRef? This { get; protected internal set; }
        public IClass? Class { get; protected internal set; }
        public bool IsSub { get; protected internal set; }
#pragma warning restore CS0628
    }

    [Flags]
    public enum StackOutput : byte
    {
        Default = 0,
        None = 0b1000_0000,
        All = 0b0111_1111,
        Threadsafe = 0b0010_1111,

        Alp = 0b0000_0001, // accumulate
        Bet = 0b0000_0010, // buffer
        Del = 0b0000_0100,  // delta
        Eps = 0b0000_1000,  // epsilon
        Tau = 0b0001_0000,  // tau
        Phi = 0b0010_0000,  // phi
        Omg = 0b0100_0000   // omega
    }

    public sealed class StackOutputMapping : Dictionary<StackOutput, StackOutput>
    {
    }

    public sealed class Stack
    {
        public const string Separator = ".";
        internal readonly StackOutput _output;
        internal readonly Stack _parent = null!;
        private readonly List<string> _keys = new();
        private readonly CtxBlob _blob = null!;
        public readonly List<StackTraceException> StackTrace = new();
        public readonly ObjectStoreKeyGenerator KeyGen; 

        public Stack()
        {
            _output = StackOutput.Alp;
            KeyGen = CreateKeys;
        }
        
        private Stack(Stack parent, StackOutput output, bool copyRefs)
        {
            _output = output;
            _parent = parent;
            if (copyRefs)
                _refs = _parent._refs;
            _blob = parent._blob;
            KeyGen = CreateKeys;
        }

        private Stack(Stack parent, CtxBlob blob)
        {
            _output = StackOutput.Alp;
            _parent = parent;
            _blob = blob;
            KeyGen = CreateKeys;
        }

        private string _local => _blob?.Local ?? RuntimeBase.MainInvocPos.SourcefilePath;
        public IObjectRef This => _blob.This ?? _parent.This;
        public IClass Class => _blob.Class ?? _parent.Class;
        public CallLocation CallLocation => _blob.CallLocation ?? _parent.CallLocation;
        public string PrefixLocal => _local + Separator;

        private readonly IObjectRef?[] _refs = new IObjectRef[7];
        public IObjectRef? this[StackOutput adr]
        {
            get => (adr == StackOutput.Default ? _output : adr) switch {
                StackOutput.Default => throw new System.Exception("Invalid State"),
                StackOutput.None => This,
                StackOutput.Alp => _refs[0] ?? _parent?[StackOutput.Alp],
                StackOutput.Bet => _refs[1] ?? _parent?[StackOutput.Bet],
                StackOutput.Del => _refs[2] ?? _parent?[StackOutput.Del],
                StackOutput.Eps => _refs[3] ?? _parent?[StackOutput.Eps],
                StackOutput.Tau => _refs[4] ?? _parent?[StackOutput.Tau],
                StackOutput.Phi => _refs[5] ?? _parent?[StackOutput.Phi],
                StackOutput.Omg => _refs[6] ?? _parent?[StackOutput.Omg],
                _ => throw new ArgumentOutOfRangeException(nameof(adr), adr, "Single argument required for getter")
            };
            set
            {
                var x = adr == StackOutput.Default ? _output : adr;
                if ((x & StackOutput.Alp) == StackOutput.Alp)
                    _refs[0] = value;
                if ((x & StackOutput.Bet) == StackOutput.Bet)
                    _refs[1] = value;
                if ((x & StackOutput.Del) == StackOutput.Del)
                    _refs[2] = value;
                if ((x & StackOutput.Eps) == StackOutput.Eps)
                    _refs[3] = value;
                if ((x & StackOutput.Tau) == StackOutput.Tau)
                    _refs[4] = value;
                if ((x & StackOutput.Phi) == StackOutput.Phi)
                    _refs[5] = value;
                if ((x & StackOutput.Omg) == StackOutput.Omg)
                    _refs[6] = value;
            }
        }

        public IObjectRef? Alp => this[StackOutput.Alp];
        public IObjectRef? Bet => this[StackOutput.Bet];
        public IObjectRef? Del => this[StackOutput.Del];
        public IObjectRef? Eps => this[StackOutput.Eps];
        public IObjectRef? Tau => this[StackOutput.Tau];
        public IObjectRef? Phi => this[StackOutput.Phi];
        public IObjectRef? Omg => this[StackOutput.Omg];
        public State State;

        private bool ContainsKey(string key) => _keys.Contains(key) || (_parent?.ContainsKey(key) ?? false);

        private IEnumerable<string> CreateKeys(VariableContext varctx, string name)
        {
            string me = varctx switch
            {
                VariableContext.Local => PrefixLocal + name,
                VariableContext.Absolute => name,
                _ => throw new ArgumentOutOfRangeException(nameof(varctx), varctx, null)
            };
            if (varctx == VariableContext.Local && !ContainsKey(me))
                _keys.Add(me); // cache in stack for cleanup
            CtxBlob? parent;
            string[] arr = new[] { me };
            if ((parent = _blob?.Parent) != null)
                return arr.Append(varctx switch
                {
                    VariableContext.Local => parent.Local + Separator + name,
                    VariableContext.Absolute => name,
                    _ => throw new ArgumentOutOfRangeException(nameof(varctx), varctx, null)
                }).Distinct();
            return arr;
        }

        public Stack Output(StackOutput outputMode = StackOutput.Alp, bool copyRefs = false) 
            => new(this, outputMode, copyRefs);

        public Stack Channel(StackOutput channel, StackOutput outputMode = StackOutput.Alp, bool copyRefs = false)
        {
            var stack = new Stack(this, outputMode, false);
            stack[outputMode] = this[channel];
            return stack;
        }

        public void StepInside(RuntimeBase vm, SourcefilePosition srcPos, string sub, Action<Stack> exec, StackOutput maintain = StackOutput.None)
        {
            new Stack(this, new CtxBlob(new CallLocation
            {
                SourceName = _local + ".." + sub,
                SourceLine = srcPos.SourcefileLine,
                SourceCursor = srcPos.SourcefileCursor
            }, _local)
            {
                Parent = _blob,
                IsSub = true,
                This = This
            }).WrapExecution(vm, exec, maintain);
        }

        // put focus into static class
        public void StepInto(RuntimeBase vm, SourcefilePosition srcPos, IClassMember local, Action<Stack> exec, StackOutput maintain = StackOutput.None)
        {
            IClass cls = local.Parent;
            string localStr = cls.FullName + '.' + local.Name + (local is IMethod mtd
                ? '(' + string.Join(", ", mtd.Parameters.Select(mp => $"{mp.Type.Name} {mp.Name}")) + ')'
                : string.Empty);
            new Stack(this, new CtxBlob(new CallLocation
            {
                SourceName = _local,
                SourceLine = srcPos.SourcefileLine,
                SourceCursor = srcPos.SourcefileCursor
            }, PrefixLocal + local)
            {
                Local = localStr,
                Class = cls,
                This = cls.SelfRef
            }).WrapExecution(vm, exec, maintain);
        }

        // put focus into object instance
        public void StepInto(RuntimeBase vm, SourcefilePosition srcPos, IObjectRef? into, IClassMember local, Action<Stack> exec, StackOutput maintain = StackOutput.None)
        {
            into ??= vm.ConstantVoid;
            var cls = local.Parent;
            string localStr = cls.FullName + '#' + (into.Value ?? IObject.Null).ObjectId.ToString("X")
                              + '.' + local.Name + (local is IMethod mtd
                                  ? '(' + string.Join(", ", mtd.Parameters.Select(mp => $"{mp.Type.Name} {mp.Name}")) +
                                    ')'
                                  : string.Empty);
            new Stack(this, new CtxBlob(new CallLocation
            {
                SourceName = _local,
                SourceLine = srcPos.SourcefileLine,
                SourceCursor = srcPos.SourcefileCursor
            }, PrefixLocal + cls.Name)
            {
                Local = localStr,
                Class = into.Value!.Type,
                This = into
            }).WrapExecution(vm, exec, maintain);
        }

        private void WrapExecution(RuntimeBase vm, Action<Stack> exec, StackOutput maintain)
        {
            try
            {
                exec(this);
            }
            catch (StackTraceException ex)
            {
                var next = new StackTraceException(CallLocation, _local, ex);
                StackTrace.Add(next);
#pragma warning disable CA2200
                // ReSharper disable once PossibleIntendedRethrow
                throw ex;
            }
            catch (InternalException ex)
            {
                //if (RuntimeBase.DebugMode)
                    // ReSharper disable once PossibleIntendedRethrow
                //    throw ex;
                throw new StackTraceException(CallLocation, _local, ex);
            }
#if !DEBUG
            catch (FatalException ex)
            {
                //if (RuntimeBase.DebugMode)
                    // ReSharper disable once PossibleIntendedRethrow
                //    throw ex;
#pragma warning restore CA2200
                throw new StackTraceException(CallLocation, _local, ex, $"Fatal internal {ex.GetType().Name}: {ex.Message}");
            }
#endif
            finally
            {
                StepUp(vm);

                if (State == State.Return)
                {
                    this[StackOutput.Default] = Omg ?? vm.ConstantVoid;
                }
                else if (State == State.Throw)
                {
                    if (Omg == null || /* null check */ Omg.Value.ObjectId == 0)
                    {
                        RuntimeBase.ExitCode = -1;
                        throw new InternalException("No Message Provided");
                    }
                    else
                    {
                        if (!Bytecode.Class.ThrowableType.CanHold(Omg.Value.Type)
                            || Omg.Value is not { } throwable)
                            throw new FatalException(
                                "Value is not instanceof Throwable: " + Omg.Value.ToString(0));
                        RuntimeBase.ExitCode = (throwable.Invoke(vm, Output(StackOutput.Alp), "ExitCode")!.Value as Numeric)!.IntValue;
                        var msg = throwable.Invoke(vm, Output(StackOutput.Bet), "Message")!.Value.ToString(0);
                        throw new InternalException(throwable.Type.Name + ": " + msg);
                    }
                }
                
                maintain = (maintain == StackOutput.Default ? _output : maintain) | StackOutput.Omg;
                if ((maintain & StackOutput.Alp) == StackOutput.Alp)
                    _parent[StackOutput.Alp] = Alp;
                if ((maintain & StackOutput.Bet) == StackOutput.Bet)
                    _parent[StackOutput.Bet] = Bet;
                if ((maintain & StackOutput.Del) == StackOutput.Del)
                    _parent[StackOutput.Del] = Del;
                if ((maintain & StackOutput.Eps) == StackOutput.Eps)
                    _parent[StackOutput.Eps] = Eps;
                if ((maintain & StackOutput.Tau) == StackOutput.Tau)
                    _parent[StackOutput.Tau] = Tau;
                if ((maintain & StackOutput.Phi) == StackOutput.Phi)
                    _parent[StackOutput.Phi] = Phi;
                if ((maintain & StackOutput.Omg) == StackOutput.Omg)
                    _parent[StackOutput.Omg] = Omg;
            }
        }

        private void StepUp(RuntimeBase vm)
        {
            foreach (string old in _keys)
                vm.ObjectStore.Remove(old);
        }

        public void Copy(StackOutput passthrough) => Copy(passthrough, passthrough);

        public void Copy(StackOutput channel = StackOutput.Alp, StackOutput output = StackOutput.Default)
            => Copy(_parent, channel, output);

        public void Copy(Stack target, StackOutput channel = StackOutput.Alp, StackOutput output = StackOutput.Default) 
            => target[output == StackOutput.Default ? _output : output] = this[channel];
    }
}