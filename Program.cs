// author: Matous Havlicek  (xhavli66)
// file for argument parsing
namespace ipk25_chat;
using CommandLine;
// make simple main class
public class Program
{
    // main function
    public static void Main(string[] args)
    {
        // terminate with ctrl+c
        Console.CancelKeyPress += delegate
        {
            //TOOD: add closing of the client and sending bye here
            // if (client != null)
            // {
            //     HelperFunctions.SendMessageBye(client);
            //     client.Close();
            // }
            System.Environment.Exit(0);

        };
        
        // begin chatting
        Chat.BeginChatting(args);
    }
}