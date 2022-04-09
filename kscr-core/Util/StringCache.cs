using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using KScr.Core.Bytecode;

namespace KScr.Core.Util;

public sealed class StringCache
{
    public const string FileName = "strings.kbin";
    private readonly IList<string> _strings = new List<string>();
    private int _index = -1;

    private int this[string str]
    {
        get
        {
            if (_strings.IndexOf(str) is { } i && i != -1)
                return i;
            int id = ++_index;
            _strings.Add(str);
            if (_strings[id] != str)
                throw new System.Exception("invalid state");
            return id;
        }
    }

    private string? this[int id] => _strings[id];

    public void Push(Stream write, string str)
    {
        write.Write(BitConverter.GetBytes(this[str]));
    }

    public string Find(byte[] data, ref int index)
    {
        int key = BitConverter.ToInt32(data, index);
        index += 4;
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
        Stream stream = new FileStream(file.FullName, FileMode.Create);
        //stream = new GZipStream(stream, CompressionLevel.SmallestSize);
        
        stream.Write(BitConverter.GetBytes(_strings.Count));
        stream.Write(NewLineBytes);
        stream.Flush();
        
        foreach (var str in _strings)
        {
            var buf = RuntimeBase.Encoding.GetBytes(str);
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
            Stream stream = new FileStream(file.FullName, FileMode.Open);
            //stream = new GZipStream(stream, CompressionMode.Decompress);
            // ReSharper disable once NotAccessedVariable
            int i = 0, c;
            byte[] buf;

            i += stream.Read(buf = new byte[4]);
            c = BitConverter.ToInt32(buf);
            i += stream.Read(buf = new byte[NewLineBytes.Length]);
            while (c-- > 0)
            {
                i += stream.Read(buf = new byte[4]);
                int l = BitConverter.ToInt32(buf);
                i += stream.Read(buf = new byte[l]);
                string value = RuntimeBase.Encoding.GetString(buf);
                i += stream.Read(buf = new byte[NewLineBytes.Length]);

                _strings.Add(value);
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