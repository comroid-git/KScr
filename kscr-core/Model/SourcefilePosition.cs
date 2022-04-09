using System;
using System.IO;
using KScr.Core.Util;

namespace KScr.Core.Model;

public struct SourcefilePosition : IBytecode
{
    public string SourcefilePath;
    public int SourcefileLine;
    public int SourcefileCursor;

    public void Write(StringCache strings, Stream stream)
    {
        stream.Write(BitConverter.GetBytes(SourcefileLine));
        stream.Write(BitConverter.GetBytes(SourcefileCursor));
        /*var buf = RuntimeBase.Encoding.GetBytes(SourcefilePath);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);*/
    }

    public void Load(RuntimeBase vm, StringCache strings, byte[] data, ref int index)
    {
        SourcefileLine = BitConverter.ToInt32(data, index);
        index += 4;
        SourcefileCursor = BitConverter.ToInt32(data, index);
        index += 4;
        /*int len = BitConverter.ToInt32(data, index);
            index += 4;
            SourcefilePath = RuntimeBase.Encoding.GetString(data, index, len);*/
    }

    public static SourcefilePosition Read(RuntimeBase vm, StringCache strings, byte[] data, ref int i)
    {
        var srcPos = new SourcefilePosition();
        srcPos.Load(vm, strings, data, ref i);
        return srcPos;
    }

    public BytecodeElementType ElementType => BytecodeElementType.SourcePosition;
}