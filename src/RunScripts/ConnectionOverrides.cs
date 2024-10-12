namespace RunScripts;
internal class ConnectionOverrides
{
    public bool RunHashCheck { get; set; }
    public required string Server { get; set; }
    public required string DatabaseName { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public bool IsSet => string.IsNullOrWhiteSpace(Server) || string.IsNullOrWhiteSpace(DatabaseName);
}
