using Amicitia.IO.Binary;
using System.IO.Hashing;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace BRB_BCSV;

public class BCSV
{
    public enum BCSVType : int
    {
        Strings = 0x2,
        Cutscene = 0x3
    }
    private int EntryCount { get; set; }
    public BCSVType Type { get; set; }
    private List<uint> NameHash { get; set; } = [];
    public List<string> Name { get; set; } = [];
    public List<string> Text { get; set; } = [];
    public List<uint> FrameStart { get; set; } = [];
    public List<uint> FrameEnd { get; set; } = [];

    public BCSV() { }
    public BCSV(string filename) => Read(new BinaryObjectReader(filename, Endianness.Big, Encoding.BigEndianUnicode));
    public BCSV(JsonDocument doc) => ReadJSON(doc);
    public void Read(string filename) => Read(new BinaryObjectReader(filename, Endianness.Big, Encoding.BigEndianUnicode));
    private void Read(BinaryObjectReader reader)
    {
        EntryCount = reader.Read<int>();
        Type = reader.Read<BCSVType>();

        if (Type == BCSVType.Strings)
        {
            // Always 0x0000000100000000;
            reader.Skip(8);

            // Read hashes and names
            NameHash.AddRange(reader.ReadArray<uint>(EntryCount));
            Name = Utils.UnhashNames(NameHash);

            // Skipping offset table
            reader.Skip(4 * EntryCount);

            // Read Unicode text
            for (int i = 0; i < EntryCount; i++)
                Text.Add(reader.ReadBEUnicodeString());
        }
        else if (Type == BCSVType.Cutscene)
        {
            // Always 0x000000020000000200000001;
            reader.Skip(12);

            // Read FrameStart
            FrameStart.AddRange(reader.ReadArray<uint>(EntryCount));

            // Read FrameEnd
            FrameEnd.AddRange(reader.ReadArray<uint>(EntryCount));

            // Read hashes and names
            NameHash.AddRange(reader.ReadArray<uint>(EntryCount));
            Name = Utils.UnhashNames(NameHash);
        }

        reader.Dispose();
    }
    public void Write(string filename) => Write(new BinaryObjectWriter(filename, Endianness.Big, Encoding.BigEndianUnicode));
    private void Write(BinaryObjectWriter writer)
    {
        writer.Write(EntryCount);
        writer.Write(Type);

        if (Type == BCSVType.Cutscene)
        {
            if (CompareFrameCount())
            {
                writer.Write(0x0000000200000002);
                writer.Write(0x00000001);

                // Write FrameStart
                writer.WriteCollection(FrameStart);

                // Write FrameEnd
                writer.WriteCollection(FrameEnd);

                // Write hashes
                writer.WriteCollection(NameHash);
            }
            else
            {
                Console.WriteLine("ERROR: Frame timecode mismatch");
                writer.Dispose();
                Thread.Sleep(3000);
                return;
            }
        }
        else if (Type == BCSVType.Strings)
        {
            writer.Write(0x0000000100000000);

            // Write hashes
            writer.WriteCollection(NameHash);

            // Write offsets
            uint offset = 0;
            for (int i = 0; i < EntryCount; i++)
            {
                writer.Write(offset);
                Text[i] = Text[i].Replace("&nbsp;", "\u00A0");
                offset += (uint)Text[i].Length + 1;
            }

            // Write text
            for (int i = 0; i < EntryCount; i++)
            {
                writer.WriteArray(Encoding.BigEndianUnicode.GetBytes(Text[i]));
                writer.Write((short)0);
            }
        }

        writer.Dispose();
    }

    public void ReadJSON(JsonDocument doc)
    {
        JsonElement root;

        if (doc.RootElement.TryGetProperty("strings", out root))
            Type = BCSVType.Strings;
        else if (doc.RootElement.TryGetProperty("cutscene", out root))
            Type = BCSVType.Cutscene;

        EntryCount = root.GetArrayLength();
        using (var strings = root.EnumerateArray())
        {
            foreach (var str in strings)
            {
                JsonElement element;
                if (str.TryGetProperty("name", out element))
                    NameHash.Add(ComputeHash(element.GetString()!));

                if (str.TryGetProperty("hash", out element))
                    NameHash.Add(uint.Parse(element.GetString()!, System.Globalization.NumberStyles.HexNumber));

                if (str.TryGetProperty("frameStart", out element) && Type == BCSVType.Cutscene)
                    FrameStart.Add(element.GetUInt32()!);

                if (str.TryGetProperty("frameEnd", out element) && Type == BCSVType.Cutscene)
                    FrameEnd.Add(element.GetUInt32()!);

                if (str.TryGetProperty("value", out element) && Type == BCSVType.Strings)
                    Text.Add(element.GetString()!);
            }
        }
    }

    public void WriteJSON(string filename)
    {
        var jsonWriterOptions = new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, Indented = true };

        using var json = new Utf8JsonWriter(File.Create($@"{Path.GetDirectoryName(filename)}\{Path.GetFileNameWithoutExtension(filename)}.json"), jsonWriterOptions);

        json.WriteStartObject();
        json.WritePropertyName(Type.ToString().ToLower());
        json.WriteStartArray();

        for (int i = 0; i < EntryCount; i++)
        {
            json.WriteStartObject();

            if (Name[i] == null)
                json.WriteString("hash", NameHash[i].ToString("X8"));
            else json.WriteString("name", Name[i]);

            if (Type == BCSVType.Strings)
                json.WriteString("value", Text[i].Replace("\u00A0", "&nbsp;"));
            else if (Type == BCSVType.Cutscene)
            {
                json.WriteNumber("frameStart", FrameStart[i]);
                json.WriteNumber("frameEnd", FrameEnd[i]);
            }

            json.WriteEndObject();
        }
        json.WriteEndArray();

        json.WriteEndObject();
        json.Dispose();

    }
    private bool CompareFrameCount()
    {
        return FrameEnd.Count == FrameStart.Count ? true : false;
    }
    private uint ComputeHash(string str)
    {
        return Crc32.HashToUInt32(Encoding.UTF8.GetBytes(str));
    }

}

