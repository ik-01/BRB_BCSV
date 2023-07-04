using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Unicode;
using System.Xml;

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
        private bool showStringLookupInfo = true;

        public List<uint> NameHash = new List<uint>();
        public List<string> Name = new List<string>();
        public List<string> Text = new List<string>();
        public List<uint> FrameStart = new List<uint>();
        public List<uint> FrameEnd = new List<uint>();
        public void BCSVToXML(string filename)
        {
            NativeReader reader = new(new FileStream(filename, FileMode.Open));
            reader.IsBigEndian = true;

            EntryCount = reader.ReadUInt32();
            Type = (BCSVType)reader.ReadUInt32();

            if (Type == BCSVType.Strings)
            {
                // Always 0x0000000100000000;
                reader.ReadInt64();

                // Read hashes and names
                for (int i = 0; i < EntryCount; i++)
                {
                    NameHash.Add(reader.ReadUInt32());
                    Name.Add(CheckHash(NameHash[i]));
                }

                // Skipping offset table
                for (int i = 0; i < EntryCount; i++)
                    reader.ReadUInt32();

                // Read Unicode text
                for (int i = 0; i < EntryCount; i++)
                    Text.Add(ReadBEUnicodeString(reader));
            }
            else if (Type == BCSVType.Cutscene)
            {
                reader.ReadInt64(); // Always 0x0000000200000002;
                reader.ReadInt32(); // Always 0x00000001;

                // Read FrameStart
                for (int i = 0; i < EntryCount; i++)
                    FrameStart.Add(reader.ReadUInt32());

                // Read FrameEnd
                for (int i = 0; i < EntryCount; i++)
                    FrameEnd.Add(reader.ReadUInt32());

                // Read hashes and names
                for (int i = 0; i < EntryCount; i++)
                {
                    NameHash.Add(reader.ReadUInt32());
                    Name.Add(CheckHash(NameHash[i]));
                }    
            }

            // Create XML document
            var xmlWriterSettings = new XmlWriterSettings { Indent = true };

            using var writer = XmlWriter.Create(Path.GetDirectoryName(filename) + "\\" +
            Path.GetFileNameWithoutExtension(filename) + ".xml", xmlWriterSettings);

            writer.WriteStartDocument();

            writer.WriteStartElement("Captions");

            writer.WriteAttributeString("Type", Type.ToString());

            for (int i = 0; i < EntryCount; i++)
            {
                writer.WriteStartElement("Caption");

                if (Name[i] == null)
                     writer.WriteAttributeString("Hash", NameHash[i].ToString("X8"));
                else writer.WriteAttributeString("Name", Name[i]);

                if (Type == BCSVType.Strings)
                    writer.WriteString(Text[i].Replace("\n", "\\n"));
                else if (Type == BCSVType.Cutscene)
                {
                    writer.WriteAttributeString("FrameStart", FrameStart[i].ToString());
                    writer.WriteAttributeString("FrameEnd", FrameEnd[i].ToString());
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();

            return;
        }
        public void XMLToBCSV(string filename)
        {
            string filePath = Path.GetDirectoryName(filename) + "\\" + Path.GetFileNameWithoutExtension(filename);
                     
            // Reading XML File
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(filePath + ".xml");
            XmlElement? xRoot = xDoc.DocumentElement;

            if (xRoot != null && xRoot.Name  == "Captions")
            {
                EntryCount = (uint)xRoot.ChildNodes.Count;
                foreach (XmlElement node in xRoot)
                {
                    if (node.Name == "Caption")
                    {
                        // If caption have hash attribute we parsing it and put in file
                        // Else we computing hash for caption name
                        if (node.HasAttribute("Hash") && !node.HasAttribute("Name"))
                            NameHash.Add(uint.Parse(node.Attributes["Hash"]!.Value, System.Globalization.NumberStyles.HexNumber));
                        else if (!node.HasAttribute("Hash") && node.HasAttribute("Name"))
                            NameHash.Add(CRC32.Compute(node.Attributes["Name"]!.Value));

                        if (xRoot.Attributes["Type"]!.Value == BCSVType.Strings.ToString())
                        {
                            Type = BCSVType.Strings;
                            Text.Add(node.InnerText);
                        }
                        else if (xRoot.Attributes["Type"]!.Value == BCSVType.Cutscene.ToString())
                        {
                            Type = BCSVType.Cutscene;
                            FrameStart.Add(uint.Parse(node.Attributes["FrameStart"]!.Value));
                            FrameEnd.Add(uint.Parse(node.Attributes["FrameEnd"]!.Value));
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Not captions xml file.\n");
                return;
            }

            using (NativeWriter writer = new (File.Open(filePath + ".bcsv", FileMode.OpenOrCreate)))
            {
                writer.IsBigEndian = true;

                writer.Write(EntryCount);
                writer.Write((int)Type);

                if (Type == BCSVType.Cutscene)
                {
                    writer.Write(0x0000000200000002);
                    writer.Write(0x00000001);

                    // Write FrameStart
                    for (int i = 0; i < EntryCount; i++)
                        writer.Write(FrameStart[i]);

                    // Write FrameEnd
                    for (int i = 0; i < EntryCount; i++)
                        writer.Write(FrameEnd[i]);

                    // Write hashes
                    for (int i = 0; i < EntryCount; i++)
                        writer.Write(NameHash[i]);
                }
                else if (Type == BCSVType.Strings)
                {
                    writer.Write(0x0000000100000000);

                    // Write hashes
                    for (int i = 0; i < EntryCount; i++)
                        writer.Write(NameHash[i]);

                    // Write offsets
                    uint offset = 0;
                    for (int i = 0; i < EntryCount; i++)
                    {
                        writer.Write(offset);
                        Text[i] = Text[i].Replace("\\n", "\n");
                        offset += (uint)Text[i].Length + 1;
                    }

                    // Write text
                    for (int i = 0; i < EntryCount; i++)
                    {
                        writer.Write(Encoding.BigEndianUnicode.GetBytes(Text[i]));
                        writer.Write((short)0);
                    }
                }

                writer.Close();
            }

            return;
        }

        public string ReadBEUnicodeString(NativeReader stream)
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

        public string CheckHash(uint _hash)
        {
            uint hash;
            string correctLine = null;
            string stringLookup = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "\\StringList.txt";
            if (File.Exists(stringLookup))
            {
                string[] lines = File.ReadAllLines(stringLookup);

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
            else 
            {
                if (showStringLookupInfo) 
                    Console.WriteLine("StringList.txt not found.\n");
                showStringLookupInfo = false;
                return correctLine;
            } 
            
        }
    }
}
