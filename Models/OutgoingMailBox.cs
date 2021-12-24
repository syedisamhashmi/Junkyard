namespace Junkyard;

public class OutgoingMailBox
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string AccessKey { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public bool UseSSL { get; set; }
}