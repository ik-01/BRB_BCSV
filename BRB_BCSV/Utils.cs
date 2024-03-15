using Amicitia.IO.Binary;
using System.Globalization;
using System.Text;

namespace BRB_BCSV;

public static class Utils
{
    public static string ReadBEUnicodeString(this BinaryObjectReader stream)
    {
        var chars = new List<byte>();

        while (true)
        {
            byte b1 = stream.ReadByte();
            byte b2 = stream.ReadByte();
            if (b1 == 0 && b2 == 0)
                break;

            chars.Add(b1);
            chars.Add(b2);
        }

        return Encoding.BigEndianUnicode.GetString(chars.ToArray());
    }

    public static List<string> UnhashNames(List<uint> hashes)
    {
        var hashDictFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hashes.txt");

        var hashDict = new Dictionary<uint, string>();

        List<string> unhashedNames = new List<string>();

        if (File.Exists(hashDictFile))
        {
            string[] lines = File.ReadAllLines(hashDictFile);

            foreach (var line in lines)
            {
                try
                {
                    // Comma separated string parsing
                    var firstComma = line.IndexOf(',');
                    if (firstComma == -1) continue;

                    var _name = line.Substring(firstComma + 1);
                    var _hash = uint.Parse(line.Substring(0, firstComma), NumberStyles.HexNumber);

                    // Adding string to dictionary if it unhashed
                    if (_name.Trim().Length > 0)
                        hashDict.Add(_hash, _name);
                }
                catch { }
            }
        }
        else Console.WriteLine("WARNING: hashes.txt not found.\n");

        foreach (var hash in hashes)
        {
            if (hashDict.ContainsKey(hash))
                unhashedNames.Add(hashDict[hash]);
            else unhashedNames.Add(null);
        }

        return unhashedNames;
    }
}



