using System.Collections.Generic;
using System.IO;
using KScr.Core.Store;
using KScr.Core.Util;

namespace KScr.Core.Bytecode
{
    public interface IBytecode
    {
        void Write(StringCache strings, Stream stream);
        void Load(RuntimeBase vm, StringCache strings, byte[] data, ref int index);
    }

    public abstract class AbstractBytecode : IBytecode
    {
        protected abstract IEnumerable<AbstractBytecode> BytecodeMembers { get; }
        public abstract void Write(StringCache strings, Stream stream);

        public virtual void Load(RuntimeBase vm, StringCache strings, byte[] data, ref int index)
        {
        }

        public virtual void Load(RuntimeBase vm, StringCache strings, byte[] data)
        {
            for (var i = 0; i < data.Length; i++)
                Load(vm, strings, data, ref i);
        }
    }
}