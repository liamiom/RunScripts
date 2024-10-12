using Spectre.Console;

namespace RunScripts;
internal static class Functions
{
    private static readonly FileSystemWatcher _fileWatcher = new();

    public static Task RunAll(ConnectionOverrides overrides)
    {
        Config config = new();
        if (!overrides.IsSet)
        {
            config.ConnectionString = Config.MakeConnectionString(overrides.Server, overrides.Username, overrides.Password, overrides.DatabaseName);
        }

        RunAll(config, overrides.RunHashCheck);

        return Task.CompletedTask;
    }

    public static Task Watch()
    {
        AnsiConsole.Markup($"Watching [green]{Environment.CurrentDirectory}[/]\n");
        Config config = new();
        RunAll(config, ShowNoItemsToUpdateText: false);

        _fileWatcher.Path = Environment.CurrentDirectory;
        _fileWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;
        _fileWatcher.Filter = "*.sql";
        _fileWatcher.IncludeSubdirectories = true;
        _fileWatcher.Changed += FileWatcher_Changed; 
        _fileWatcher.EnableRaisingEvents = true;

        // wait - not to end
        new AutoResetEvent(false).WaitOne();

        return Task.CompletedTask;
    }

    private static void FileWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        _fileWatcher.EnableRaisingEvents = false;
        Config config = new();
        RunAll(config, ShowNoItemsToUpdateText: false);
        _fileWatcher.EnableRaisingEvents = true;
    }

    public static bool RunAll(Config config, bool RunHashCheck = true, bool ShowNoItemsToUpdateText = true)
    {
        List<Entities.UpdatedObject> scriptList = Database.GetScriptListForDB(config, RunHashCheck);
        if (scriptList.Count == 0)
        {
            if (ShowNoItemsToUpdateText)
            {
                Console.WriteLine($"There are no items to update");
            }

            return true;
        }

        return Database.RunScriptFileList(config, scriptList);
    }

    public static Task ClearDBCache()
    {
        Config config = new();
        if (string.IsNullOrWhiteSpace(config.ConnectionString))
        {
            Console.WriteLine("No database connection set. Use the -config argument to setup the database connection");
            return Task.CompletedTask;
        }

        Database.ClearDBCache(config);
        Console.WriteLine("Cache cleared");

        return Task.CompletedTask;
    }
}
