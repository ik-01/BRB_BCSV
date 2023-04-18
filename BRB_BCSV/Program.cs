using System;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace BRB_BCSV
{
    public enum BCSVType : int
    { 
        Strings = 0x2,
        Cutscene = 0x3
    }
    public class BCSV
    {
        public uint EntryCount;
        public BCSVType Type;
        public int field_08;
        public int field_0C;
        public int field_10;

        public List<uint> NameHash = new List<uint>();
        public List<string> Name = new List<string>();
        public List<uint> StringOffset = new List<uint>();
        public List<string> Text = new List<string>();
        public List<uint> FrameStart = new List<uint>();
        public List<uint> FrameEnd = new List<uint>();
        public void ReadBCSV(string filename)
        {
            using (NativeReader reader = new(new FileStream(filename, FileMode.Open)))
            {
                EntryCount = reader.ReadUInt(Endian.Big);
                Type = (BCSVType)reader.ReadUInt(Endian.Big);
                XmlDocument doc = new XmlDocument();
                var element1 = doc.CreateElement("Captions");
                XmlAttribute typeAttr;
                element1.SetAttribute("Type",Type.ToString());
                doc.AppendChild(element1); 
                if (Type == BCSVType.Strings)
                {
                    field_08 = reader.ReadInt(Endian.Big);
                    field_0C = reader.ReadInt(Endian.Big);
                    Console.WriteLine("Entries: {0}\nType: {1}\nfield_08: {2}\nfield_0C: {3}", EntryCount, Type, field_08, field_0C);
                    for (int i = 0; i < EntryCount; i++)
                    {
                        NameHash.Add(new uint());
                        Name.Add(new string(""));
                        NameHash[i] = reader.ReadUInt(Endian.Big);
                        Name[i] = CheckHash(NameHash[i]);
                    }
                    for (int i = 0; i < EntryCount; i++)
                    {
                        StringOffset.Add(new uint());
                        StringOffset[i] = reader.ReadUInt(Endian.Big);
                    }
                    for (int i = 0; i < EntryCount; i++)
                    {
                        Text.Add(new string(""));
                        byte[] v = Encoding.Unicode.GetBytes(reader.ReadNullTerminatedWideString());
                        string converted = Encoding.BigEndianUnicode.GetString(v);
                        Text[i] = converted;
                    }

                    for (int i = 0; i < EntryCount; i++)
                    {
                        Console.WriteLine("Entry: {0}, Offset: {1}\n\t {2}", Name[i], StringOffset[i].ToString("X8"), Text[i]);
                        var capElement = doc.CreateElement("Caption");
                        XmlAttribute attr;
                        if (Name[i] == null)
                            capElement.SetAttribute("Hash", NameHash[i].ToString("X8"));
                        else
                            capElement.SetAttribute("Name", Name[i]);
                        var captext = doc.CreateTextNode(Text[i]);
                        capElement.AppendChild(captext);
                        element1.AppendChild(capElement);
                    }
                    doc.Save(Path.GetDirectoryName(Environment.GetCommandLineArgs()[1]) + "\\strings.xml");
                }
                else if (Type == BCSVType.Cutscene)
                {
                    field_08 = reader.ReadInt(Endian.Big);
                    field_0C = reader.ReadInt(Endian.Big);
                    field_10 = reader.ReadInt(Endian.Big);
                    Console.WriteLine("Entries: {0}\nType: {1}\nfield_08: {2}\nfield_0C: {3}\nfield_10: {4}", EntryCount, Type, field_08, field_0C, field_10);
                    for (int i = 0; i < EntryCount; i++)
                    {
                        FrameStart.Add(new uint());
                        FrameStart[i] = reader.ReadUInt(Endian.Big);
                    }
                    for (int i = 0; i < EntryCount; i++)
                    {
                        FrameEnd.Add(new uint());
                        FrameEnd[i] = reader.ReadUInt(Endian.Big);
                    }
                    for (int i = 0; i < EntryCount; i++)
                    {
                        NameHash.Add(new uint());
                        Name.Add(new string(""));
                        NameHash[i] = reader.ReadUInt(Endian.Big);
                        Name[i] = CheckHash(NameHash[i]);
                    }
                    for (int i = 0; i < EntryCount; i++)
                    {
                        Console.WriteLine("Entry: {0}\n {1} - {2}", Name[i], FrameStart[i], FrameEnd[i]);
                        var capElement = doc.CreateElement("Caption");
                        XmlAttribute attr;
                        if (Name[i] == null)
                             capElement.SetAttribute("Hash", NameHash[i].ToString("X8"));
                        else capElement.SetAttribute("Name", Name[i]);

                        capElement.SetAttribute("FrameStart", FrameStart[i].ToString());
                        capElement.SetAttribute("FrameEnd", FrameEnd[i].ToString());
                        element1.AppendChild(capElement);
                    }
                    var outname = "\\" + Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[1]) + ".xml";
                    doc.Save(Path.GetDirectoryName(Environment.GetCommandLineArgs()[1]) + outname);
                }
                
            }
            
        }
        public string CheckHash(uint _hash)
        {
            uint hash;
            string correctLine = null;
            string[] lines = File.ReadAllLines(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "\\StringList.txt");
            foreach (string line in lines)
            {
                hash = CRC32.Compute(line);
                if (_hash == hash)
                {
                    correctLine = line;
                    break;
                }
            }
            return correctLine;
        }
    }

    class Program
    {
        static BCSV bcsv = new BCSV();
        public static void Main()
        {
            Console.WriteLine();
            //  Invoke this sample with an arbitrary set of command line arguments.
            string[] arguments = Environment.GetCommandLineArgs();
            Console.WriteLine("{0}\n", string.Join(", ", arguments[1]));

            bcsv.ReadBCSV(arguments[1]);
            Console.ReadKey();
        }
    }
}

