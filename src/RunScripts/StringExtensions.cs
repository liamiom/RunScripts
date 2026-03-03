namespace RunScripts;
internal static class StringExtensions
{
    public static List<string> SplitGoBy(this string Source)
    {
        List<string> output = new();
        string script = "";
        foreach (string line in Source.Replace("\r", "").Split('\n'))
        {
            if (line.Trim().ToLower() != "go")
            {
                script += line + "\r\n";
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(script))
                {
                    output.Add(script);
                }

                script = "";
            }
        }

        if (!string.IsNullOrWhiteSpace(script))
        {
            output.Add(script);
        }

        return output;
    }
}
