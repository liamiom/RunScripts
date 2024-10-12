namespace RunScripts.Entities;
internal class UpdatedObject
{
    public string ScriptName { get; set; }
    public string Hash { get; set; }

    public bool IsMatch(UpdatedObject compare) => 
        ScriptName == compare.ScriptName && Hash == compare.Hash;
}
