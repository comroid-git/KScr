using System.Collections.Generic;
using System.IO;
using KScr.Lib.Exception;
using KScr.Lib.Model;
using KScr.Lib.Store;

namespace KScr.Lib.Bytecode
{
    public class MethodParameterComponent : StatementComponent
    {
        public readonly List<StatementComponent> Expressions = new List<StatementComponent>();

        public override void Write(Stream stream)
        {
            // todo
        }

        public override void Load(RuntimeBase vm, byte[] data, ref int index)
        {
            // todo
        }

        public override State Evaluate(RuntimeBase vm, IEvaluable? prev, ref ObjectRef? rev)
        {
            if (Expressions.Count != rev!.Length)
                throw new InternalException("Invalid method parameter expression count; expected " + rev.Length);

            for (var i = 0; i < Expressions.Count; i++)
            {
                ObjectRef val = new ObjectRef(Class.VoidType);
                Expressions[i].Evaluate(vm, null, ref val!);
                rev[i] = val.Value;
            }
            
            return State.Normal;
        }
    }
}