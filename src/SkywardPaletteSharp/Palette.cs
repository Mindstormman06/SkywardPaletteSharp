using System.Buffers.Binary;

namespace SkywardPaletteSharp;

/// <summary>A fixed-size block of big-endian words inside a palette file, read and written in place.</summary>
public sealed class PaletteRecord
{
    readonly byte[] _data;
    readonly int _at;

    internal PaletteRecord(byte[] data, int at, int size) { _data = data; _at = at; Size = size; }

    public int Size { get; }
    public int WordCount => Size / 4;
    public Span<byte> Bytes => _data.AsSpan(_at, Size);

    public uint UInt(int word) => BinaryPrimitives.ReadUInt32BigEndian(Bytes[(Check(word) * 4)..]);
    public void SetUInt(int word, uint value) => BinaryPrimitives.WriteUInt32BigEndian(Bytes[(Check(word) * 4)..], value);
    public float Float(int word) => BitConverter.UInt32BitsToSingle(UInt(word));
    public void SetFloat(int word, float value) => SetUInt(word, BitConverter.SingleToUInt32Bits(value));
    /// <summary>The word as RGBA8.</summary>
    public uint Color(int word) => UInt(word);
    public void SetColor(int word, uint rgba) => SetUInt(word, rgba);

    int Check(int word) => (uint)word < (uint)WordCount ? word : throw new ArgumentOutOfRangeException(nameof(word));
}

/// <summary>Common base: the file is kept as bytes and every view writes through.</summary>
public abstract class PaletteData
{
    protected PaletteData(byte[] data) => Data = data;

    public byte[] Data { get; }
    public byte[] Write() => Data;

    protected uint U32(int at) => BinaryPrimitives.ReadUInt32BigEndian(Data.AsSpan(at));
    protected void SetU32(int at, uint value) => BinaryPrimitives.WriteUInt32BigEndian(Data.AsSpan(at), value);
    protected List<PaletteRecord> Records(int start, int size, int count) =>
        Enumerable.Range(0, count).Select(i => new PaletteRecord(Data, start + i * size, size)).ToList();
}

/// <summary>A <c>.spf</c> or <c>.pal</c>: a u32 header, 520-byte palette records and any trailing bytes.</summary>
public sealed class PaletteFile : PaletteData
{
    public const int RecordSize = 520;

    public PaletteFile(byte[] data) : base(data)
    {
        if (data.Length < 4 + RecordSize) throw new InvalidDataException("palette file is too short");
        Palettes = Records(4, RecordSize, (data.Length - 4) / RecordSize);
    }

    public static PaletteFile FromBinary(byte[] data) => new(data);

    public uint Header { get => U32(0); set => SetU32(0, value); }
    public IReadOnlyList<PaletteRecord> Palettes { get; }
    public Span<byte> Tail => Data.AsSpan(4 + Palettes.Count * RecordSize);
}

/// <summary>An <c>.sff</c>: a u32 header and fog entries of a colour and two distances.</summary>
public sealed class PaletteFogFile : PaletteData
{
    public const int EntrySize = 12;

    public PaletteFogFile(byte[] data) : base(data)
    {
        if (data.Length < 4 || (data.Length - 4) % EntrySize != 0) throw new InvalidDataException("bad fog palette size");
        Entries = Enumerable.Range(0, (data.Length - 4) / EntrySize).Select(i => new PaletteFog(this, 4 + i * EntrySize)).ToList();
    }

    public static PaletteFogFile FromBinary(byte[] data) => new(data);

    public uint Header { get => U32(0); set => SetU32(0, value); }
    public IReadOnlyList<PaletteFog> Entries { get; }

    internal uint Get(int at) => U32(at);
    internal void Set(int at, uint value) => SetU32(at, value);
}

public sealed class PaletteFog
{
    readonly PaletteFogFile _f;
    readonly int _at;

    internal PaletteFog(PaletteFogFile f, int at) { _f = f; _at = at; }

    public uint Color { get => _f.Get(_at); set => _f.Set(_at, value); }
    public float Near { get => BitConverter.UInt32BitsToSingle(_f.Get(_at + 4)); set => _f.Set(_at + 4, BitConverter.SingleToUInt32Bits(value)); }
    public float Far { get => BitConverter.UInt32BitsToSingle(_f.Get(_at + 8)); set => _f.Set(_at + 8, BitConverter.SingleToUInt32Bits(value)); }

    public override string ToString() => $"{Color:X8} {Near}..{Far}";
}

/// <summary>An <c>.eaf</c>: 32 equal records (160 bytes in most files, 128 in one).</summary>
public sealed class PaletteEffectFile : PaletteData
{
    public const int RecordCount = 32;

    public PaletteEffectFile(byte[] data) : base(data)
    {
        if (data.Length == 0 || data.Length % (RecordCount * 4) != 0) throw new InvalidDataException("bad effect palette size");
        Records = Records(0, data.Length / RecordCount, RecordCount);
    }

    public static PaletteEffectFile FromBinary(byte[] data) => new(data);

    public new IReadOnlyList<PaletteRecord> Records { get; }
}

/// <summary>An <c>.mcf</c>: a table of RGBA8 colours.</summary>
public sealed class PaletteColorTable : PaletteData
{
    public PaletteColorTable(byte[] data) : base(data)
    {
        if (data.Length % 4 != 0) throw new InvalidDataException("bad colour table size");
    }

    public static PaletteColorTable FromBinary(byte[] data) => new(data);

    public int Count => Data.Length / 4;
    public uint this[int index]
    {
        get => U32(Check(index) * 4);
        set => SetU32(Check(index) * 4, value);
    }

    int Check(int index) => (uint)index < (uint)Count ? index : throw new ArgumentOutOfRangeException(nameof(index));
}
