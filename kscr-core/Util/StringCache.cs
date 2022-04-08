using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using KScr.Core.Bytecode;

namespace KScr.Core.Util;

public sealed class StringCache
{
    public const string FileName = "strings.kbin";
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
                //throw new InvalidOperationException($"Duplicate String hash of strings {str} and {buf}");
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

    public string Find(byte[] data, ref int index)
    {
        long key = BitConverter.ToInt64(data, index);
        index += 8;
        return this[key] ?? throw new InvalidOperationException($"String with ID {key} could not be found");
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

    public void Write(DirectoryInfo dir) => Write(MakeFile(dir));

    public void Write(FileInfo file)
    {
        var stream = new FileStream(file.FullName, FileMode.Create);
        
        stream.Write(BitConverter.GetBytes(_dict.Count));
        stream.Flush();
        
        foreach (var pair in _dict)
        {
            stream.Write(BitConverter.GetBytes(pair.Key));
            var buf = RuntimeBase.Encoding.GetBytes(pair.Value);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);
            stream.Write(NewLineBytes);
            stream.Flush();
        }
        
        stream.Close();
    }

    public void Load(DirectoryInfo dir) => Load(MakeFile(dir));

    public void Load(FileInfo file)
    {
        if (!file.Exists)
            Debug.WriteLine("[StringCache] Warning: Empty StringCache loaded");
        else
        {
            var stream = new FileStream(file.FullName, FileMode.Open);
            // ReSharper disable once NotAccessedVariable
            int i = 0, c;
            byte[] buf;

            i += stream.Read(buf = new byte[4]);
            c = BitConverter.ToInt32(buf);
            while (c-- > 0)
            {
                i += stream.Read(buf = new byte[8]);
                long key = BitConverter.ToInt64(buf);
                i += stream.Read(buf = new byte[4]);
                int l = BitConverter.ToInt32(buf);
                i += stream.Read(buf = new byte[l]);
                string value = RuntimeBase.Encoding.GetString(buf);
                i += stream.Read(buf = new byte[NewLineBytes.Length]);

                _dict[key] = value;
            }

            stream.Close();
        }
    }

    public static StringCache Read(DirectoryInfo dir) => Read(MakeFile(dir));

    public static StringCache Read(FileInfo file)
    {
        var strings = new StringCache();
        strings.Load(file);
        return strings;
    }

    private static FileInfo MakeFile(DirectoryInfo dir)
    {
        return new FileInfo(Path.Combine(dir.FullName, FileName));
    }

    public static readonly byte[] NewLineBytes = RuntimeBase.Encoding.GetBytes("\n");
}