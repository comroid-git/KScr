using System;
using System.Collections.Generic;
using System.Linq;
using KScr.Core.Bytecode;
using KScr.Core.Exception;
using KScr.Core.Model;
using KScr.Core.Std;
using static KScr.Core.Store.StackOutput;

// ReSharper disable ConditionIsAlwaysTrueOrFalse

namespace KScr.Core.Store;

public enum VariableContext : byte
{
    Local,
    This,
    Super,
    Property,
    Absolute
}

public struct CallLocation
{
    public CallLocation(SourcefilePosition srcPos)
    {
        SourceName = srcPos.SourcefilePath;
        SourceRow = srcPos.SourcefileLine;
        SourceColumn = srcPos.SourcefileCursor;
    }

    public string SourceName { get; set; }
    public int SourceRow { get; set; }
    public int SourceColumn { get; set; }
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
    Tri = 0b0000_0111,

    Alp = 0b0000_0001, // accumulate
    Bet = 0b0000_0010, // buffer
    Del = 0b0000_0100, // delta
    Eps = 0b0000_1000, // epsilon
    Tau = 0b0001_0000, // tau
    Phi = 0b0010_0000, // phi
    Omg = 0b0100_0000 // omega
}

public sealed class StackOutputMapping : Dictionary<StackOutput, StackOutput>
{
}

public sealed class Stack
{
    public const string Separator = ".";
    public static readonly List<StackTraceException> StackTrace = new();
    private readonly CtxBlob _blob = null!;
    private readonly List<string> _keys = new();
    internal readonly StackOutput _output;
    internal readonly Stack _parent = null!;

    private readonly IObjectRef?[] _refs = new IObjectRef[7];
    public readonly ObjectStoreKeyGenerator KeyGen;
    public State State;

    public Stack()
    {
        _output = StackOutput.Alp;
        KeyGen = CreateKeys;
    }

    private Stack(Stack parent, StackOutput output)
    {
        _output = output;
        _parent = parent;
        _blob = parent._blob;
        KeyGen = CreateKeys;
        this[Tri] = parent.This;
    }

    private Stack(Stack parent, CtxBlob blob)
    {
        _output = StackOutput.Alp;
        _parent = parent;
        _blob = blob;
        KeyGen = CreateKeys;
    }

    public IObjectRef This => _blob.This ?? _parent.This;
    public IClass Class => _blob.Class ?? _parent.Class;
    private string _local => _blob?.Local ?? _parent?._local ?? RuntimeBase.SystemSrcPos.SourcefilePath;
    public CallLocation CallLocation => _blob.CallLocation ?? _parent.CallLocation;
    public string PrefixLocal => _local + Separator;

    public IObjectRef? this[StackOutput adr]
    {
        get => (adr == Default ? _output : adr) switch
        {
            Default => throw new System.Exception("Invalid State"),
            None => This,
            StackOutput.Alp => _refs[0] ?? _parent?[StackOutput.Alp],
            StackOutput.Bet => _refs[1] ?? _parent?[StackOutput.Bet],
            StackOutput.Del => _refs[2] ?? _parent?[StackOutput.Del],
            StackOutput.Eps => _refs[3],
            StackOutput.Tau => _refs[4],
            StackOutput.Phi => _refs[5],
            StackOutput.Omg => _refs[6],
            _ => throw new ArgumentOutOfRangeException(nameof(adr), adr, "Single argument required for getter")
        };
        set
        {
            var x = adr == Default ? _output : adr;
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

    private bool ContainsKey(string key)
    {
        return _keys.Contains(key) || (_parent?.ContainsKey(key) ?? false);
    }

    private IEnumerable<string> CreateKeys(VariableContext varctx, string name)
    {
        var me = varctx switch
        {
            VariableContext.Local => PrefixLocal + name,
            VariableContext.Absolute or VariableContext.Property => name,
            _ => throw new ArgumentOutOfRangeException(nameof(varctx), varctx, null)
        };
        if (varctx == VariableContext.Local && !ContainsKey(me))
            _keys.Add(me); // cache in stack for cleanup
        CtxBlob? parent;
        string[] arr = { me };
        if ((parent = _blob?.Parent) != null)
            return arr.Append(varctx switch
            {
                VariableContext.Local => parent.Local + Separator + name,
                VariableContext.Absolute or VariableContext.Property => name,
                _ => throw new ArgumentOutOfRangeException(nameof(varctx), varctx, null)
            }).Distinct();
        return arr;
    }

    public Stack Output(StackOutput outputMode = StackOutput.Alp)
    {
        return new Stack(this, outputMode);
    }

    public Stack Channel(StackOutput channel, StackOutput outputMode = StackOutput.Alp)
    {
        var stack = new Stack(this, outputMode);
        stack[outputMode | channel] = this[channel];
        return stack;
    }

    public void StepInside(RuntimeBase vm, SourcefilePosition srcPos, string sub, Action<Stack> exec,
        StackOutput maintain = None)
    {
        new Stack(this, new CtxBlob(new CallLocation
        {
            SourceName = _local + ".." + sub,
            SourceRow = srcPos.SourcefileLine,
            SourceColumn = srcPos.SourcefileCursor
        }, _local)
        {
            Parent = _blob,
            IsSub = true,
            This = This
        }).WrapExecution(vm, exec, maintain);
    }

    // put focus into object instance

    public void StepInto(RuntimeBase vm, SourcefilePosition srcPos, IObject? into, IClassMember local,
        Action<Stack> exec, StackOutput maintain = None)
    {
        into ??= local.Parent.DefaultInstance;
        var cls = into as IClassInstance ?? into.Type;
        var localStr =
            (local.IsStatic()
                ? $"{cls.FullName}"
                : $"{cls.FullName}#{into.ObjectId:X16}")
            + $".{local.Name}{(local is IMethod mtd ? '(' + string.Join(", ", mtd.Parameters.Select(mp => $"{mp.Type.Name} {mp.Name}")) + ')' : string.Empty)}";
        new Stack(this, new CtxBlob(new CallLocation
        {
            SourceName = _local,
            SourceRow = srcPos.SourcefileLine,
            SourceColumn = srcPos.SourcefileCursor
        }, PrefixLocal + cls.Name)
        {
            Local = localStr,
            Class = into!.Type,
            This = new ObjectRef(into.Type, into)
        }).WrapExecution(vm, exec, maintain);
    }

    public IObjectRef? StepIntoLambda(RuntimeBase vm, Stack stack, StatementComponent lambda, params IObject[] args)
    {
        var srcPos = lambda.SourcefilePosition;
        var lambdaContext = stack;
        while (!lambdaContext._local.StartsWith(lambda.Args[0]))
            lambdaContext = lambdaContext._parent;
        var _stack = new Stack(this, new CtxBlob(new CallLocation
        {
            SourceName = _local,
            SourceRow = srcPos.SourcefileLine,
            SourceColumn = srcPos.SourcefileCursor
        }, lambdaContext.PrefixLocal)
        {
            Class = lambdaContext.Class,
            This = lambdaContext.This
        });
        for (var i = 0; i < lambda.SubStatement!.Main.Count; i++)
        {
            var param = lambda.SubStatement!.Main[i];
            vm.PutLocal(_stack, param.Args[1], args[i]);
        }

        _stack.WrapExecution(vm,
            stack =>
            {
                lambda.InnerCode!.Evaluate(vm, stack).Copy(StackOutput.Alp, StackOutput.Bet);
            }, StackOutput.Bet);
        return _stack[StackOutput.Bet];
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
        catch (RuntimeException ex)
        {
            //if (RuntimeBase.DebugMode)
            // ReSharper disable once PossibleIntendedRethrow
            //    throw ex;
            var stackTraceException = new StackTraceException(CallLocation, _local, ex);
            StackTrace.Add(stackTraceException);
            throw stackTraceException;
        }
#if !DEBUG
            catch (System.Exception ex)
            {
                //if (RuntimeBase.DebugMode)
                    // ReSharper disable once PossibleIntendedRethrow
                //    throw ex;
#pragma warning restore CA2200
                throw new StackTraceException(CallLocation, _local, new RuntimeException(null, ex), $"Fatal internal {ex.GetType().Name}: {ex.Message}");
            }
#endif
        finally
        {
            if (State == State.Return)
            {
                this[Default] = Omg ?? vm.ConstantVoid;
            }
            else if (State == State.Throw)
            {
                if (Omg == null || /* null check */ Omg.Value.ObjectId == 0)
                {
                    RuntimeBase.ExitCode = -1;
                    throw new RuntimeException("No Message Provided", IObject.Null);
                }

                if (!Std.Class.ThrowableType.CanHold(Omg.Value.Type)
                    || Omg.Value is not { } throwable)
                    throw new FatalException(
                        "Value is not instanceof Throwable: " + Omg.Value.ToString(0));
                RuntimeBase.ExitCode =
                    (throwable.InvokeNative(vm, Output(), "ExitCode").Copy(output: StackOutput.Alp)![vm, this, 0] as
                        Numeric)!
                    .IntValue;
                RuntimeBase.ExitMessage =
                    throwable.InvokeNative(vm, Output(), "Message").Copy(output: StackOutput.Bet)![vm, this, 0]
                        .ToString(0);
                throw new RuntimeException(
                    $"{throwable.Type.Name}: {RuntimeBase.ExitMessage} ({RuntimeBase.ExitCode})", Omg.Value);
            }

            maintain = (maintain == Default ? _output : maintain) | StackOutput.Omg;
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

            CopyState();
            StepUp(vm);
        }
    }

    private void StepUp(RuntimeBase vm)
    {
        if (!_blob.IsSub)
            vm.ObjectStore.ClearLocals(this);
    }

    public IObjectRef? Copy(StackOutput passthrough)
    {
        return Copy(passthrough, passthrough);
    }

    public IObjectRef? Copy(StackOutput channel = StackOutput.Alp, StackOutput output = Default)
    {
        return Copy(_parent, channel, output);
    }

    public IObjectRef? Copy(Stack target, StackOutput channel = StackOutput.Alp, StackOutput output = Default)
    {
        CopyState();
        return target[output] = this[channel];
    }

    public IObjectRef? CopyState(Stack target = null!)
    {
        if (((target ??= _parent).State = State) is State.Return or State.Throw)
            target[StackOutput.Omg] = this[StackOutput.Omg];
        return this[Default];
    }
}