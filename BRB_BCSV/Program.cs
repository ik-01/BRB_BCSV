using System.Text.Json;

namespace BRB_BCSV;

class Program
{
    public static string GetOutput(string input, string extension)
    {
        if (extension == ".bcsv")
            return Path.ChangeExtension(input,"json");
        else return Path.ChangeExtension(input, "bcsv");
    }
    
    public static void Main(string[] args)
    {
        if (args.Length != 0)
        {
            BCSV bcsv;
            var input = args[0];
            var extension = Path.GetExtension(input);

            string output;
            if (args.Length > 1)
                output = args[1];
            else output = GetOutput(input, extension);

            if (!Path.IsPathFullyQualified(input))
                input = Path.GetFullPath(input);

            if (!Path.IsPathFullyQualified(output))
                output = Path.Combine(Path.GetDirectoryName(input), Path.GetFileName(output));

            if (extension == ".bcsv")
            {
                bcsv = new(input);
                bcsv.WriteJSON(output);
            }
            else if (extension == ".json")
            {
                bcsv = new(JsonDocument.Parse(File.OpenRead(input)));
                bcsv.Write(output);
            }
        }
        else
        {
            Console.WriteLine("BRB_BCSV - A tool for converting Sonic Boom: Rise of Lyric localization file to .json and vice versa\n" +
                              "Created by ik-01\n\n" +
                "Usage: \n" +
                "bcsv to json: BRB_BCSV.exe input.bcsv output.json\n" +
                "json to bcsv: BRB_BCSV.exe input.json output.bcsv \n");
            Console.ReadKey();
        }
    }
}