namespace ipk25_chat;
// author: Matous Havlicek  (xhavli66)
// file for argument parsing
public class Utils
{
    // print usage 
    public static void PrintUsage()
    {
        Console.WriteLine("Usage: ipk25-chat -t [tcp|udp] -p [port] -h [hostname] -n [nickname]");
        Environment.Exit(0);
    }
    
    // print help message for the user
    public static void PrintHelpCommand()
    {
        Console.Write("Supported local commands:");
        Console.Write("/auth (username) (secret) (displayname) - sends auth message to the server");
        Console.Write("/join (channelID) - sends join message with channel name to the server");
        Console.Write("/rename (displayname) - locally changes display name of the user");
        
    }
}