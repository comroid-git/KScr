using System;
using KScr.Lib.Core;

namespace KScr.Lib.VM
{
    public abstract class VirtualMachine
    {
        public abstract ObjectStore ObjectStore { get; }
        public abstract TypeStore TypeStore { get; }
        public Context Context { get; internal set; } = new Context();

        public ObjectRef? this[VariableContext varctx, string name]
        {
            get => ObjectStore[Context, varctx, name];
            set => ObjectStore[Context, varctx, name] = value;
        }

        public static long GetHashCode64(string input) =>
            // inspired by https://stackoverflow.com/questions/8820399/c-sharp-4-0-how-to-get-64-bit-hash-code-of-given-string
            CombineHash((uint)input.Substring(0,input.Length / 2).GetHashCode(), input.Substring(input.Length / 2));

        public static long CombineHash(uint objId, string name) => CombineHash(objId, name.GetHashCode());

        public static long CombineHash(uint objId, int hash) => (long)hash << 0x20 | objId; 

        private uint _lastObjId = 0xF;
        public uint NextObjId() => ++_lastObjId;
        public long NextObjId(string name) => CombineHash(NextObjId(), name);

        public void Clear()
        {
            ObjectStore.Clear();
            TypeStore.Clear();
        }
    }
}