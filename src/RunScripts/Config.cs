using Spectre.Console;

namespace RunScripts;
internal class Config
{
    public string ConfigFolder => Environment.CurrentDirectory + "\\.cfig";
    public string ConfigFile => ConfigFolder + "\\RunScripts.conf";

    public string ConnectionString { get; set; } = "";

    public Config()
    {
        if (!File.Exists(ConfigFile))
        {
            return;
        }

        List<string> lines = File
            .ReadAllLines(ConfigFile)
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .ToList();

        ConnectionString = GetValue("ConnectionString", lines);
    }

    private string SerializeToString() => 
        $"ConnectionString: {ConnectionString} \r\n";

    private static string GetValue(string fieldName, List<string> lines, string defaultValue = "")
    {
        fieldName += ":";

        return lines
            .Where(line => line.StartsWith(fieldName))
            .Select(line => line.Substring(fieldName.Length).Trim())
            .FirstOrDefault(defaultValue);
    }

    public void Save()
    {
        if (!Directory.Exists(ConfigFolder))
        {
            DirectoryInfo info = new(ConfigFolder);
            info.Create();
            info.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
        }

        File.WriteAllText(ConfigFile, SerializeToString());
    }

    public static string MakeConnectionString(string serverName, string userName = "", string password = "", string database = "master") =>
        string.IsNullOrWhiteSpace(userName)
            ? $"Data Source={serverName};Initial Catalog={database};Integrated Security=SSPI;"
            : $"Data Source={serverName};Initial Catalog={database};User id={userName};Password={password}";

    private static void CreateDirectoryIfDoesntExist(string path)
    {
        path = Path.Combine(Environment.CurrentDirectory, path);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public static Task CreateFolders()
    {
        CreateDirectoryIfDoesntExist("");
        CreateDirectoryIfDoesntExist("Tables");
        CreateDirectoryIfDoesntExist("Views");
        CreateDirectoryIfDoesntExist("Functions");
        CreateDirectoryIfDoesntExist("Procedures");
        CreateDirectoryIfDoesntExist("Templates");

        Console.WriteLine("Folders created");

        return Task.CompletedTask;
    }

    public static Task Set()
    {
        Config config = new();
        
        if (!string.IsNullOrWhiteSpace(config.ConnectionString) &&
            !AnsiConsole.Confirm(
                $"The current ConnectionString is: " +
                $"[green]{config.ConnectionString}[/]\n" +
                $"Do you want to change it?", defaultValue: false))
        {
            return Task.CompletedTask;
        }

        string serverName = AnsiConsole.Ask("SQL Server:", defaultValue: "localhost");
        string userName = AnsiConsole.Ask("SQL UserName: (Leave blank for windows auth)", defaultValue: "");
        string password = !string.IsNullOrWhiteSpace(userName)
            ? AnsiConsole.Ask<string>("SQL Password:")
            : "";

        config.ConnectionString = MakeConnectionString(serverName, userName, password);

        bool connected = false;
        AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Star)
            .Start("Testing connection string...", ctx => {
                connected = Database.TestConnectionString(config); 
            });

        if (!connected)
        {
            AnsiConsole.Markup("Database connection failed");
            return Task.CompletedTask;
        }


        List<string> databases = Database.GetDatabaseNames(config);
        string databaseName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a database")
                .AddChoiceGroup("Tags", databases)
            );
        config.ConnectionString = MakeConnectionString(serverName, userName, password, databaseName);

        config.Save();
        Console.WriteLine("New config saved");

        if (AnsiConsole.Confirm("Do you want to create template folders in the current directory?", defaultValue: false))
        {
            CreateFolders();
        }

        return Task.CompletedTask;
    }
}
