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
        // parse the arguments
        ArgumentParsing.ParseArguments(args);
    }
}