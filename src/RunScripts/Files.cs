using Spectre.Console;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace RunScripts;
internal class Files
{
    public static List<string> GetFilesSafe(string Path)
    {
        if (!Directory.Exists(Path))
        {
            return new List<string>();
        }

        List<string> output = Directory.GetFiles(Path, "*.sql").ToList();
        foreach (string subPath in Directory.GetDirectories(Path))
        {
            output.AddRange(GetFilesSafe(subPath));
        }

        return output;
    }

    // Put the named directories at the top 
    public static List<string> OrderFiles(List<string> FileList, string Root)
    {
        List<string> output = new();

        output.AddRange(FileList.Where(i => i.StartsWith(Root + "\\Tables")));
        output.AddRange(FileList.Where(i => i.StartsWith(Root + "\\Views")));
        output.AddRange(FileList.Where(i => i.StartsWith(Root + "\\Functions")));
        output.AddRange(FileList.Where(i => i.StartsWith(Root + "\\Procedures")));

        output.AddRange(FileList.Where(i =>
                !i.StartsWith(Root + "\\Tables") &&
                !i.StartsWith(Root + "\\Views") &&
                !i.StartsWith(Root + "\\Functions") &&
                !i.StartsWith(Root + "\\Procedures")
            ));

        return output;
    }

    public static byte[] GetFileHash(string FileName, int TryNumber = 0)
    {
        if (TryNumber > 100)
        {
            AnsiConsole.Markup($"[red]Unable to create hash for {FileName}[/]");
            return [];
        }

        try
        {
            using var md5 = MD5.Create();
            using var stream = new FileStream(FileName, FileMode.Open, FileAccess.Read);
            return md5.ComputeHash(stream);
        }
        catch (IOException)
        {
            string relativeFileName = Path.GetRelativePath(Environment.CurrentDirectory, FileName);
            Console.WriteLine($"{relativeFileName} is currently locked. Retrying in two seconds");
            Thread.Sleep(2000);
            return GetFileHash(FileName, TryNumber + 1);
        }
    }

    public static string GetAssemblyResource(string FileName)
    {
        string sqLScript;
        using (Stream ScriptStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RunScripts.Sql." + FileName))
        {
            using StreamReader sr = new StreamReader(ScriptStream);
            sqLScript = sr.ReadToEnd();
        }

        return sqLScript;
    }

    public static void SaveAssemblyResource(string FileName, string SaveTo)
    {
        using Stream scriptStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RunScripts.Sql." + FileName);
        using var fileStream = File.Create(SaveTo);
        scriptStream.Seek(0, SeekOrigin.Begin);
        scriptStream.CopyTo(fileStream);
    }

    public static string GetFileHashString(string FileName) =>
        Encoding.ASCII.GetString(GetFileHash(FileName));
}
