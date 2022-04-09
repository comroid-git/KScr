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
    public const string FileName = "strings" + RuntimeBase.BinaryFileType;
    private readonly IList<string> _strings = new List<string>();
    private int _index = -1;

    public int this[string str]
    {
        get
        {
            if (str == null)
                throw new NullReferenceException();
            if (_strings.IndexOf(str) is var i && i != -1)
                return i;
            int id = ++_index;
            _strings.Add(str);
            if (_strings[id] != str)
                throw new System.Exception("invalid state");
            return id;
        }
    }

    public string? this[int id] => _strings[id];

    public void Write(DirectoryInfo dir) => Write(MakeFile(dir));

    public void Write(FileInfo file)
    {
        Stream stream = new FileStream(file.FullName, FileMode.Create);

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