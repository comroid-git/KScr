using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using KScr.Lib.Bytecode;
using KScr.Lib.Core;
using KScr.Lib.Exception;
using KScr.Lib.Model;

namespace KScr.Lib.Store
{
    public sealed class ObjectStore
    {
        private readonly ConcurrentDictionary<string, ObjectRef?>
            cache = new ConcurrentDictionary<string, ObjectRef?>();

        public ObjectRef? this[Context ctx, VariableContext varctx, string name]
        {
            get
            {
                string? key = CreateKey(ctx, varctx, name);
                if (cache.ContainsKey(key))
                    return cache[key];
                return null;
            }
            set
            {
                string? key = CreateKey(ctx, varctx, name);
                cache[key] = value;
            }
        }

        private static string CreateKey(Context ctx, VariableContext varctx, string name)
        {
            return varctx switch
            {
                VariableContext.Local => ctx.PrefixLocal + name,
                VariableContext.This => ctx.PrefixThis + name,
                VariableContext.Absolute => name,
                _ => throw new ArgumentOutOfRangeException(nameof(varctx), varctx, null)
            };
        }

        public void ClearLocals(Context ctx)
        {
            foreach (var localKey in cache.Keys.Where(it => it.StartsWith(ctx.PrefixLocal)))
                if (!cache.TryRemove(localKey, out _))
                    throw new InternalException("Unable to remove local variable " + localKey);
        }

        public void Clear()
        {
            cache.Clear();
        }
    }

    public class ObjectRef
    {
        public ObjectRef(IClassRef type, IObject? value) : this(type)
        {
            Value = value;
        }

        public ObjectRef(IClassRef type, [Range(1, int.MaxValue)] int len = 1)
        {
            if (len < 1)
                throw new ArgumentOutOfRangeException(nameof(len), len, "Invalid ObjectRef size");
            
            Type = type;
            Stack = new IObject?[len];
        }

        public readonly IClassRef Type;
        public readonly IObject?[] Stack;
        
        public int Length => Stack.Length;
        public bool IsPipe => ReadAccessor != null || WriteAccessor != null;
        public virtual IEvaluable? ReadAccessor { get; set; }
        public virtual IEvaluable? WriteAccessor { get; set; }

        public IObject? this[RuntimeBase vm, int i]
        {
            get
            {
                if (ReadAccessor != null)
                {
                    ObjectRef output = Numeric.Constant(vm, i);
                    ReadAccessor!.Evaluate(vm, null, ref output!);
                    return output.Value;
                }
                else return Stack[i];
            }
            set
            {
                CheckTypeCompat(value!.Type);
                if (WriteAccessor != null)
                {
                    ObjectRef output = new ObjectRef(Class.VoidType) { Value = value };
                    WriteAccessor!.Evaluate(vm, null, ref output!);
                } 
                else InsertToStack(i, value);
            }
        }

        public IObject? Value
        {
            get => Stack[0];
            set
            {
                CheckTypeCompat(value!.Type);
                InsertToStack(0, value);
            }
        }

        public override string ToString() => Length > 1
            ? Type + "" + string.Join(",", Stack.Select(it => it?.ToString()))
            : Type + ": " + Value;

        private void CheckTypeCompat(IClassRef other)
        {
            if (!Type.CanHold(other))
                throw new InternalException("Invalid Type (" + other + ") assigned to reference of type " + Type);
        }

        private void InsertToStack(int i, IObject? value)
        {
            if (IsPipe)
                throw new InvalidOperationException("Cannot insert value inte pipe");
            Stack[i] = value;
        }
    }
}