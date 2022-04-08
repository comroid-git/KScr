using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace KScr.Core.Util;

public sealed class StringCache
{
    private readonly IDictionary<long, string> _dict = new ConcurrentDictionary<long, string>();

    public long this[string str]
    {
        get
        {
            var id = GetHashCode64(str);
            string? buf;
            // create key offset if it already exists
            if (_dict.ContainsKey(id) && _dict.TryGetValue(id, out buf) && buf != str)
            {
                Debug.WriteLine($"[StringCache] Needs to offset id {id} for string {str}");
                do id += 1;
                while (_dict.ContainsKey(id) && _dict.TryGetValue(id, out buf) && buf != str);
            }
            // store and return id
            _dict[id] = str;
            return id;
        }
    }

    public string? this[long id]
    {
        get
        {
            _dict.TryGetValue(id, out string? str);
            return str;
        }
    }

    public static long GetHashCode64(string input)
    {
        // inspired by https://stackoverflow.com/questions/8820399/c-sharp-4-0-how-to-get-64-bit-hash-code-of-given-string
        return CombineHash((uint)input.Substring(0, input.Length / 2).GetHashCode(),
            input.Substring(input.Length / 2));
    }

    public static long CombineHash(uint objId, string name)
    {
        return CombineHash(objId, name.GetHashCode());
    }

    public static long CombineHash(uint objId, int hash)
    {
        return ((long)hash << 0x20) | objId;
    }
}