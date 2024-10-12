namespace RunScripts;
internal static class UI
{
    public static Task PrintHelp()
    {
        string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
        Console.WriteLine(
            $"""

            RunScripts {version}

            RunScripts will apply any SQL scripts in the current directory to a given database. This can be 
            helpful for making database changes in a declarative way. Once scripts have been applied to the 
            database the file hash is stored in a cache table so any future runs will only run the changed 
            scripts. On larger projects this can significantly speed up execution time.

            Options:
                -Init           Setup the database connection
                -Config         Change the database connection
                -Clear          Clear the database hash cache table
                -Run            Apply the updated scripts to the database
                -Watch          Watch for file changes and apply them to the database automatically

            Optional Overrides: These apply to the -Run and -Watch options
                -dbname "DatabaseName"
                -server "SqlInstanceName"
                -user "SqlUserName" (Only required when using a SQL user)
                -pass "SqlPassword" (Only required when using a SQL user)
                -ignorehash (Ignore the file Hash check so the all the script files are run)
            
            Usage Examples:
            	rs -init (Map the current directory to a database)
            	rs -run (Run all updated scripts against the configured database)
            	rs -watch (Watch the current directory and run any scripts that change against the database)
            """);

        return Task.CompletedTask;
    }
}

