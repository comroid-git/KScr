using System.Collections.Generic;
using System.IO;

namespace KScr.Lib.Bytecode
{
    public interface IBytecode
    {
        void Write(Stream stream);
        void Load(RuntimeBase vm, byte[] data, ref int index);
    }

    public abstract class AbstractBytecode : IBytecode
    {
        protected static readonly byte[] NewLineBytes = RuntimeBase.Encoding.GetBytes("\n");
        protected abstract IEnumerable<AbstractBytecode> BytecodeMembers { get; }
        public abstract void Write(Stream stream);

        public virtual void Load(RuntimeBase vm, byte[] data, ref int index)
        {
        }

        public virtual void Load(RuntimeBase vm, byte[] data)
        {
            for (var i = 0; i < data.Length; i++)
                Load(vm, data, ref i);
        }
    }
}