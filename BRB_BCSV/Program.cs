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
    

    class Program
    {
        static BCSV bcsv = new BCSV();
        public static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                var extension = Path.GetExtension(args[0]);
                if (extension == ".bcsv")
                    bcsv.ReadBCSV(args[0]);
                else if (extension == ".xml")
                    bcsv.WriteBCSV(args[0]);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("BRB_BCSV - A tool for converting Sonic Boom: Rise of Lyric localization file to .xml and vice versa\n" +
                                  "Created by ik-01\n\n" +
                    "Usage: \n" + 
                    "bcsv to xml: BRB_BCSV.exe file.bcsv\n" +
                    "xml to bcsv: BRB_BCSV.exe file.xml\n");
                Console.ReadKey();
            }
        }
    }
}

