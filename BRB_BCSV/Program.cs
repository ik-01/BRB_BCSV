using System;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

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
                element1.SetAttribute("Type", Type.ToString());
                doc.AppendChild(element1);
                if (Type == BCSVType.Strings)
                {
                    field_08 = reader.ReadInt(Endian.Big);
                    field_0C = reader.ReadInt(Endian.Big);
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
                        Text[i] = reader.ReadBigEndianUnicodeString();
                    }
                    for (int i = 0; i < EntryCount; i++)
                    {
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
                    var outname = "\\" + Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[1]) + ".xml";
                    doc.Save(Path.GetDirectoryName(Environment.GetCommandLineArgs()[1]) + outname);
                }
                else if (Type == BCSVType.Cutscene)
                {
                    field_08 = reader.ReadInt(Endian.Big);
                    field_0C = reader.ReadInt(Endian.Big);
                    field_10 = reader.ReadInt(Endian.Big);
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
        public void WriteBCSV(string filename)
        {

            XmlDocument doc = new XmlDocument();
            doc.Load(filename);
            var root = doc.DocumentElement;
            var typeAttr = root.GetAttribute("Type");
            int count = root.SelectNodes("Caption").Count;
            var outname = "\\" + Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[1]) + ".bcsv";
            using (NativeWriter writer = new(new FileStream(Path.GetDirectoryName(Environment.GetCommandLineArgs()[1]) + outname, FileMode.Create)))
            {
                writer.Write(count, Endian.Big);
                if (typeAttr == BCSVType.Strings.ToString())
                {
                    writer.Write((int)BCSVType.Strings, Endian.Big);
                    writer.Write(1, Endian.Big);
                    writer.Write(0, Endian.Big);
                    var capElement = root.GetElementsByTagName("Caption");
                    for (int i = 0; i < count; i++)
                    {
                        XmlElement elem = (XmlElement)capElement[i];
                        if (elem.HasAttribute("Hash") && !elem.HasAttribute("Name"))
                            writer.Write(Int32.Parse(elem.Attributes["Hash"].Value, System.Globalization.NumberStyles.HexNumber), Endian.Big);
                        else if (!elem.HasAttribute("Hash") && elem.HasAttribute("Name"))
                            writer.Write(CRC32.Compute(elem.Attributes["Name"].Value), Endian.Big);
                    }
                    uint offset = 0;
                    for (int i = 0; i < count; i++)
                    {
                        XmlElement elem = (XmlElement)capElement[i];
                        writer.Write(offset, Endian.Big);
                        offset += (uint)elem.InnerText.Length + 1;
                    }
                    for (int i = 0; i < count; i++)
                    {
                        XmlElement elem = (XmlElement)capElement[i];
                        var bytes = Encoding.BigEndianUnicode.GetBytes(elem.InnerText);
                        writer.Write(bytes);
                        writer.Write((short)0);
                    }
                }
                else if (typeAttr == BCSVType.Cutscene.ToString())
                {
                    writer.Write((int)BCSVType.Cutscene, Endian.Big);
                    writer.Write(2, Endian.Big);
                    writer.Write(2, Endian.Big);
                    writer.Write(1, Endian.Big);
                    var capElement = root.GetElementsByTagName("Caption");
                    for (int i = 0; i < count; i++)
                    {
                        XmlElement elem = (XmlElement)capElement[i];
                        if (elem.HasAttribute("FrameStart"))
                            writer.Write(Int32.Parse(elem.Attributes["FrameStart"].Value), Endian.Big);
                    }
                    for (int i = 0; i < count; i++)
                    {
                        XmlElement elem = (XmlElement)capElement[i];
                        if (elem.HasAttribute("FrameEnd"))
                            writer.Write(Int32.Parse(elem.Attributes["FrameEnd"].Value), Endian.Big);
                    }
                    for (int i = 0; i < count; i++)
                    {
                        XmlElement elem = (XmlElement)capElement[i];
                        if (elem.HasAttribute("Hash") && !elem.HasAttribute("Name"))
                            writer.Write(Int32.Parse(elem.Attributes["Hash"].Value, System.Globalization.NumberStyles.HexNumber), Endian.Big);
                        else if (!elem.HasAttribute("Hash") && elem.HasAttribute("Name"))
                            writer.Write(CRC32.Compute(elem.Attributes["Name"].Value), Endian.Big);
                    }
                }
                writer.Close();
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
            var extension = Path.GetExtension(arguments[1]);
            if (extension == ".bcsv")
                bcsv.ReadBCSV(arguments[1]);
            else if (extension == ".xml")
                bcsv.WriteBCSV(arguments[1]);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}

