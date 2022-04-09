using System;
using System.IO;
using System.Linq;
using System.Text;

namespace KScr.Core.Util;

public class PeekStream : Stream
{
    private readonly Stream _stream;
    private readonly TextWriter _writer;

    public PeekStream(TextWriter writer, Stream stream)
    {
        _stream = stream;
        _writer = writer;
    }

    public override void Flush()
    {
        _stream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var x = _stream.Read(buffer, offset, count);
        _writer.WriteLine(MakeText(buffer, offset, count, "Read()"));
        return x;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _stream.Write(buffer, offset, count);
        _writer.WriteLine(MakeText(buffer, offset, count, "Write()"));
    }

    public override bool CanRead => _stream.CanRead;
    public override bool CanSeek => _stream.CanSeek;
    public override bool CanWrite => _stream.CanWrite;
    public override long Length => _stream.Length;
    public override long Position { get => _stream.Position; set => _stream.Position = value; }

    private ReadOnlySpan<char> MakeText(byte[] buffer, int offset, int len, string msg)
    {
        byte[] txt = new byte[len];
        Array.Copy(buffer, offset, txt, 0, len);
        msg += ": 0x" + string.Join(" 0x", txt.Select(x => x.ToString("X")));
        return msg;
    }
}