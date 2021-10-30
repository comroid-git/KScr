using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KScr.Lib.Bytecode
{
    public abstract class AbstractBytecode
    {
        protected static readonly byte[] NewLineBytes = RuntimeBase.Encoding.GetBytes("\n");
        protected abstract IEnumerable<AbstractBytecode> BytecodeMembers { get; }
        public abstract void Write(Stream stream);

        public virtual void Load(RuntimeBase vm, byte[] data)
        {
            for (var i = 0; i < data.Length; i++) 
                Load(vm, data, ref i);
        }

        public virtual void Load(RuntimeBase vm, byte[] data, ref int index) {}
    }
}