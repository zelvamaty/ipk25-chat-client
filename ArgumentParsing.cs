// author: Matous Havlicek  (xhavli66)
// file for argument parsing
namespace ipk25_chat;
// using System;
using CommandLine;

// class to handle argument parsing 
public class ArgumentParsing
{
    // parse the arguments and return the parsed arguments/ print out stuff
    public static ProgramArguments ParseArguments(string[] args)
    {
        ProgramArguments programArguments = new ProgramArguments();
        Parser.Default.ParseArguments<ProgramArguments>(args)
            .WithParsed(o =>
            {
                programArguments = o;
                
                if (o.Help)
                {
                    Utils.PrintUsage();
                    Environment.Exit(0);
                }
                
                if (o.TransportProtocol != "tcp" && o.TransportProtocol != "udp")
                {
                    Console.WriteLine("Invalid transport protocol. Must be 'tcp' or 'udp'.");
                    Environment.Exit(1);
                }
                
            });

        return programArguments;
    }
}

// class to store program arguments
public class ProgramArguments
{
    [Option('t', Required = true, HelpText = "Transport protocol used for connection (tcp or udp)")]
    public string? TransportProtocol { get; set; }

    [Option('s', Required = true, HelpText = "Server IP address or hostname")]
    public string? Server { get; set; }

    [Option('p', Required = false, Default = 4567, HelpText = "Server port (0-65535)")]
    public int Port { get; set; }

    [Option('d', Required = false, Default = 250, HelpText = "UDP confirmation timeout in milliseconds (default: 250)")]
    public int Timeout { get; set; }

    [Option('r', Required = false, Default = 3, HelpText = "Maximum number of UDP retransmissions")]
    public int Retries { get; set; }

    [Option('h', Required = false, HelpText = "Show help message")]
    public bool Help { get; set; }
    
    [Value(0, MetaName = "target", HelpText = "Target hostname or IPv4/IPv6 address")]
    public string? Target { get; set; }
}