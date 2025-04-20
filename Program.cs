// author: Matous Havlicek  (xhavli66)
// file for argument parsing
namespace ipk25_chat;
using CommandLine;
// program class, entry point of the program
public class Program
{
    // main function
    public static async Task Main(string[] args)
    {
        // begin chatting
        await Chat.BeginChatting(args);
    }
}