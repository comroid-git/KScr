using System;
using System.IO;
using KScr.Lib.Bytecode;

namespace KScr.Lib.Model;

public struct SourcefilePosition : IBytecode
{
    public string SourcefilePath;
    public int SourcefileLine;
    public int SourcefileCursor;

    public void Write(Stream stream)
    {
        stream.Write(BitConverter.GetBytes(SourcefileLine));
        stream.Write(BitConverter.GetBytes(SourcefileCursor));
        /*var buf = RuntimeBase.Encoding.GetBytes(SourcefilePath);
            stream.Write(BitConverter.GetBytes(buf.Length));
            stream.Write(buf);*/
    }

    public void Load(RuntimeBase vm, byte[] data, ref int index)
    {
        SourcefileLine = BitConverter.ToInt32(data, index);
        index += 4;
        SourcefileCursor = BitConverter.ToInt32(data, index);
        index += 4;
        /*int len = BitConverter.ToInt32(data, index);
            index += 4;
            SourcefilePath = RuntimeBase.Encoding.GetString(data, index, len);*/
    }

    public static SourcefilePosition Read(RuntimeBase vm, byte[] data, ref int i)
    {
        var srcPos = new SourcefilePosition();
        srcPos.Load(vm, data, ref i);
        return srcPos;
    }
}