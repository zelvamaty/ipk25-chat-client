// author: Matous Havlicek  (xhavli66)
// file for utilities used in the project
namespace ipk25_chat;


// class for utilities used in the project
public class Utils
{
    // print usage method
    public static void PrintUsage()
    {
        Console.WriteLine("Usage: ipk25chat-client -t [tcp|udp] -p [port] -h [hostname] -n [nickname] -r [retries] -d [timeout]");
        Environment.Exit(0);
    }
    
    // async read input line 
    public static Task<String?> ConsoleReadLineAsync()
    {
        return Task.Run(() =>
        {
            return Console.ReadLine();
        });
    }
    // print out error from message type
    public static void PrintErrorFrom(string displayname, string message)
    {
        Console.WriteLine("ERROR FROM " + displayname + ": "  + message);
    }
    // print out normal message
    public static void PrintMessage(string displayname, string message)
    {
        Console.WriteLine(displayname + ": "  + message);
    }
    // print out error message type
    public static void PrintError(string message)
    {
        Console.WriteLine("ERROR: " + message);
    }
    
    // print out error message type from byte array
    public static void PrintErrorBytes(byte[] message)
    {
       Console.WriteLine("ERROR: " + BitConverter.ToString(message));
    }
    
    // check if two byte arrays are equal
    public static bool CheckByteArrayEquals(byte[] arr1, byte[] arr2)
    {
        if (arr1.Length != arr2.Length)
        {
            return false;
        }

        for (int i = 0; i < arr1.Length; i++)
        {
            if (arr1[i] != arr2[i])
            { 
                return false;
            }
        }
        return true;
    }
}