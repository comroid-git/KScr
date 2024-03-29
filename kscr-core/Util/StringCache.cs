﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using comroid.common;
using KScr.Core.Bytecode;
using KScr.Core.System;

namespace KScr.Core.Util;

public sealed class StringCache
{
    public const string FileName = "strings" + RuntimeBase.BinaryFileExt;

    public static readonly byte[] NewLineBytes = RuntimeBase.Encoding.GetBytes("\n");

    private readonly IList<string> _strings = new List<string>();
    private int _index = -1;

    public static IEnumerable<string> CommonStrings => new[] { Method.ConstructorName, Method.StaticInitializerName }
        .Concat(GetAllClasses(Class.LibRootPackage)
            .SelectMany(cls => new[] { cls.CanonicalName, cls.FullDetailedName }));

    public int this[string str]
    {
        get
        {
            if (str == null)
                throw new NullReferenceException();
            if (CommonStrings.Contains(str))
                ; // todo Do not include common strings in every string cache
            if (_strings.IndexOf(str) is var i && i != -1)
                return i;
            var id = ++_index;
            _strings.Add(str);
            if (_strings[id] != str)
                throw new global::System.Exception("invalid state");
            return id;
        }
    }

    public string? this[int id] => _strings[id];

    private static IEnumerable<Class> GetAllClasses(IPackageMember mem)
    {
        if (mem is Package pkg)
            return pkg.PackageMembers.Values.SelectMany(GetAllClasses);
        if (mem is Class cls)
            return new[] { cls };
        throw new global::System.Exception("invalid state");
    }

    public void Write(DirectoryInfo dir)
    {
        Write(MakeFile(dir).OpenWrite());
    }

    public void Write(Stream stream)
    {
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

    public void Load(Stream stream)
    {
        // ReSharper disable once NotAccessedVariable
        int i = 0, c;
        byte[] buf;

        i += stream.Read(buf = new byte[4]);
        c = BitConverter.ToInt32(buf);
        i += stream.Read(buf = new byte[NewLineBytes.Length]);
        while (c-- > 0)
        {
            i += stream.Read(buf = new byte[4]);
            var l = BitConverter.ToInt32(buf);
            i += stream.Read(buf = new byte[l]);
            var value = RuntimeBase.Encoding.GetString(buf);
            i += stream.Read(buf = new byte[NewLineBytes.Length]);

            _strings.Add(value);
        }

        stream.Close();
    }

    public static StringCache Read(DirectoryInfo dir)
    {
        return Read(MakeFile(dir).OpenRead());
    }

    public static StringCache Read(Stream stream)
    {
        var strings = new StringCache();
        strings.Load(stream);
        return strings;
    }

    private static FileInfo MakeFile(DirectoryInfo dir)
    {
        return new FileInfo(Path.Combine(dir.FullName, FileName));
    }
}