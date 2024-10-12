using RunScripts.Entities;
using Spectre.Console;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace RunScripts;
internal partial class Database
{
    private static SqlConnection _DBConnection;
    private static SqlConnection GetDBConnection(Config config)
    {
        if (_DBConnection == null )
        {
            _DBConnection = new SqlConnection(config.ConnectionString);
            _DBConnection.Open();
        }    

        return _DBConnection;
    }

    public static bool TestConnectionString(Config config)
    {
        try
        {
            GetDBConnection(config);
            return true;
        }
        catch
        {
            Console.WriteLine($"Unable to connect using \"{config.ConnectionString}\"");
            return false;
        }
    }

    public static List<UpdatedObject> GetScriptListForDB(Config config, bool RunHashCheck)
    {
        List<UpdatedObject> databaseObjects = GetObjectsFromDB(config);

        string root = Environment.CurrentDirectory;
        List<string> sqlScripts = Files.OrderFiles(Files.GetFilesSafe(root), root);
        List<UpdatedObject> diskObjects = sqlScripts
            .Select(i => Path.GetRelativePath(Environment.CurrentDirectory, i))
            .Select(i => new UpdatedObject { ScriptName = i, Hash = Files.GetFileHashString(Path.Combine(Environment.CurrentDirectory, i)) })
            .ToList();

        List<UpdatedObject> trimmedScriptList = RunHashCheck
            ? diskObjects
                .Where(item => !databaseObjects.Exists(i => i.IsMatch(item)))
                .ToList()
            : diskObjects;

        return trimmedScriptList;
    }

    private static bool CheckForUpdatedObjectTable(Config config)
    {
        string sql =
            """
            --Create RunScripts_Cache Table
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE id = OBJECT_ID(N'[dbo].[RunScripts_Cache]'))
            CREATE TABLE [dbo].[RunScripts_Cache](
            	[ScriptName] [varchar](800) NOT NULL,
            	[Hash] [varchar](800) NOT NULL,
            )
            """;
        SqlConnection databaseConn = GetDBConnection(config);
        SqlTransaction sqlTran = databaseConn.BeginTransaction();
        if (!ExecuteScript(sql, databaseConn, sqlTran))
        {
            sqlTran.Rollback();
            return false;
        }

        sqlTran.Commit();

        return true;
    }

    private static string GetFieldByName(SqlDataReader reader, string fieldName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i) == fieldName)
            {
                return reader.GetString(i);
            }
        }

        return "";
    }

    public static List<UpdatedObject> GetObjectsFromDB(Config config)
    {
        if (!CheckForUpdatedObjectTable(config))
        {
            return [];
        }

        string script = "SELECT * FROM RunScripts_Cache";
        List<UpdatedObject> output = [];

        SqlCommand command = new(script, GetDBConnection(config));
        SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            output.Add(new UpdatedObject {
                ScriptName = GetFieldByName(reader, "ScriptName"),
                Hash = GetFieldByName(reader, "Hash"),
            });
        }

        reader.Close();

        return output;
    }

    public static void ClearDBCache(Config config)
    {
        string script = "DELETE RunScripts_Cache";
        
        SqlCommand command = new(script, GetDBConnection(config));
        command.ExecuteNonQuery();
    }

    public static List<string> GetDatabaseNames(Config config)
    {
        string script = "SELECT name FROM master.dbo.sysdatabases";
        List<string> output = [];

        SqlCommand command = new(script, GetDBConnection(config));
        SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            output.Add(reader.GetString(0));
        }

        reader.Close();

        return output;
    }

    public static bool RunScriptFileList(Config config, List<UpdatedObject> FileList)
    {
        if (FileList.Count == 0)
        {
            return true;
        }

        SqlConnection dbCon = GetDBConnection(config);
        SqlTransaction sqlTran = dbCon.BeginTransaction();
        foreach (UpdatedObject scriptFile in FileList)
        {
            if (!RunScriptFile(scriptFile, dbCon, sqlTran))
            {
                sqlTran.Rollback();
                return false;
            }
        }

        sqlTran.Commit();

        return true;
    }

    public static bool RunScriptFile(UpdatedObject fileItem, SqlConnection DatabaseConn, SqlTransaction SqlTran)
    {
        string fileName = fileItem.ScriptName;
        if (!File.Exists(fileName))
        {
            return false;
        }

        string relativeFileName = Path.GetRelativePath(Environment.CurrentDirectory, fileName);
        Console.WriteLine($"{DatabaseConn.Database} executing {relativeFileName} ");
        foreach (string sqlScript in File.ReadAllText(fileName).SplitGoBy())
        {
            if (!ExecuteScript(sqlScript, DatabaseConn, SqlTran))
            {
                return false;
            }
        }

        string hash = fileItem.Hash.Replace("'", "''");
        string tagUpdate = 
            $"DELETE [RunScripts_Cache] WHERE ScriptName = '{relativeFileName}'\r\n" +
            $"INSERT[RunScripts_Cache](ScriptName, [Hash]) VALUES('{relativeFileName}', '{hash}')";

        return ExecuteScript(tagUpdate, DatabaseConn, SqlTran);
    }

    public static bool ExecuteScript(string SQLScript, SqlConnection DatabaseConn, SqlTransaction SqlTran)
    {
        try
        {
            SqlCommand command = new(SQLScript, DatabaseConn, SqlTran)
            {
                CommandTimeout = 120
            };
            command.ExecuteNonQuery();

            return true;
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Error Running SQL[/] \n\n {HighlightErrorLine(SQLScript, e.Message)}\n\nError Message\n{e.Message}");

            return false;
        }
    }

    private static string HighlightErrorLine(string SQLScript, string ErrorMessage)
    {
        SQLScript = SQLScript.Replace("[", "[[").Replace("]", "]]");

        foreach (string item in ErrorMessage.Replace("\r", "").Split('\n').Where(i => i.StartsWith("Incorrect syntax near", StringComparison.OrdinalIgnoreCase)))
        {
            string section = GetSyntaxErrorSection().Match(item).Result("$2");
            if (section.Length < 2)
            {
                continue;
            }

            SQLScript = SQLScript.Replace(section, $"[red]{section}[/]");
        }

        return SQLScript;
    }

    [GeneratedRegex("(.*)'(.*)'")]
    private static partial Regex GetSyntaxErrorSection();
}
