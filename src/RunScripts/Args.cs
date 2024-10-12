using System.Text.RegularExpressions;

namespace RunScripts;
internal class Args
{
    public enum Options { Run, Watch, Init, Config, ClearCache, Help }

    public static Options FromStringArray(string[] args)
    {
        if (args.Length == 0)
        {
            return Options.Help;
        }

        string firstArg = CleanUpArg(args[0]);
        return firstArg switch
        {
            "config" => Options.Config,
            "run" => Options.Run,
            "watch" => Options.Watch,
            "init" => Options.Init,
            "clear" => Options.ClearCache,
            _ => Options.Help
        };
    }

    private static string GetArgValue(string allArgs, string argName)
    {
        Match patternMatch = Regex.Match(allArgs, @$".* ?-{argName} (\w+).*", RegexOptions.IgnoreCase);

        return patternMatch.Success 
            ? patternMatch.Result("$1") 
            : "";
    }

    public static ConnectionOverrides GetConnectionOverrides(string[] args)
    {
        string allArgs = string.Join(" ", args);

        return new ConnectionOverrides()
        {
            Server = GetArgValue(allArgs,  "server"),
            DatabaseName = GetArgValue(allArgs, "dbname"),
            Username = GetArgValue(allArgs, "user"),
            Password = GetArgValue(allArgs, "pass"),
            RunHashCheck = !allArgs.Contains("ignorehash"),
        };
    }

    private static string CleanUpArg(string argString) =>
        argString
            .Replace("/", "")
            .Replace("-", "")
            .ToLower()
            .Trim();
}
