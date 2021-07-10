using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.IO.Compression;
using System.Runtime.InteropServices;

public class Reader : IDisposable
{
    private readonly Stream stream;
    public string payload;

    public Reader(Stream stream)
    {
        this.stream = stream;
        this.payload = "";
    }

    public Reader(Stream stream, string payload)
    {
        this.stream = stream;
        this.payload = payload;
    }

    public Reader DecompressSection()
    {
        int uncompressed = ReadInt();
        int compressed = ReadInt();
        string p = "uc: " + uncompressed + ", c: " + compressed;

        if (compressed == 0 || compressed > uncompressed)
        {
            this.payload = p;
            return this;
        } else
        {
            SkipNBytes(2); // Remove 2 byte prefix to fix stuff???
            byte[] compressedBuf = new byte[compressed-2];
            stream.Read(compressedBuf, 0, compressed-2);
            Stream compressedStream = new MemoryStream(compressedBuf);
            DeflateStream decompressStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
            byte[] uncompressedBuf = new byte[uncompressed];
            decompressStream.Read(uncompressedBuf, 0, uncompressed);
            return new Reader(new MemoryStream(uncompressedBuf), p);
        } 
    }

    public int SkipSection()
    {
        int uncompressed = ReadInt();
        int compressed = ReadInt();

        if (compressed == 0)
        {
            SkipNBytes(uncompressed);
            return uncompressed;
        } else
        {
            SkipNBytes(compressed);
            return compressed;
        }
    }

    public Stream GetStream()
    {
        return stream;
    }

    public byte[] ReadNBytes(int n)
    {
        byte[] buf = new byte[n];
        stream.Read(buf, 0, n);
        return buf;
    }

    public void SkipNBytes(int n)
    {
        stream.Seek(n, SeekOrigin.Current);
    }

    public int ReadShort()
    {
        byte[] buf = ReadNBytes(2);
        return buf[1] << 8 | buf[0]; 
    }

    public int ReadInt()
    {
        byte[] buf = ReadNBytes(4);
        return buf[3] << 24 | buf[2] << 16 | buf[1] << 8 | buf[0];
    }

    public long ReadLong()
    {
        byte[] buf = ReadNBytes(8);
        return buf[7] << 56 | buf[6] << 48 | buf[5] << 40 | buf[4] << 32 | buf[3] << 24 | buf[2] << 16 | buf[1] << 8 | buf[0];
    }

    public string ReadString()
    {
        int len = ReadInt();
        if (len < 0)
        {
            len = -len;
            SkipNBytes(len);
            return "";
        }
        if (len == 0)
        {
            return "";
        }
        byte[] buf = ReadNBytes(len-1);
        SkipNBytes(1);
        return System.Text.Encoding.ASCII.GetString(buf);
    }

    public int ReadColor()
    {
        return ReadInt();
    }

    public List<T> Array<T>(Func<T> func) 
    {
        List<T> array = new List<T>();
        int len = ReadInt();
        for (int i=0; i < len; ++i)
        {
            array.Add(func());
        }
        return array;
    }

    public void Dispose()
    {

    }
}
