// author: Matous Havlicek  (xhavli66)
// file to manage chatting

using System.Text.RegularExpressions;

namespace ipk25_chat;

// class for managing chatting 
public class Chat
{
    // define the states of the FSM
    public enum ChatState
    {
        Start,
        Auth,
        Open,
        Join,
        EndSuccess,
        EndFailure
    }
    private static ChatState currentState; // current state of the chat
    private static IChatProtocol chat= null!; // current chat protocol
    private static ProgramArguments arguments = null!; // arguments for the chat    
    public static string displayName = "unknown"; // displayName of the user (unknown placeholder)
    public static int udpTimeout => arguments.Timeout; // timeout for UDP
    
    // method to begin chatting
    public static async Task BeginChatting(string[] args)
    {
        // parse the arguments
        arguments = ArgumentParsing.ParseArguments(args);
        
        
        // check which protocol to use
        if (arguments.TransportProtocol == "tcp")
        {
            chat = new Tcp();
        }
        else
        {
            chat = new Udp();
        }
        // chat protocol has been chosen, all chat.Lipsum commands will use this protocol
        currentState = ChatState.Start;
        await Fsm();
    }

    // method to handle the finite state machine
    public async static Task Fsm()
    {
        chat.Connect(arguments.Server!, arguments.Port); // connect to the server
        // terminate with ctrl+c (sends correct BYE message depending on the protocol)
        Console.CancelKeyPress += delegate
        {
            chat.Bye(displayName);
            chat.Close();
            System.Environment.Exit(0);

        };
        while (true)
        {
            // check the current state and perform the action
            switch (currentState)
            {
                case ChatState.Start:
                    Task<String?> userInput = Utils.ConsoleReadLineAsync();

                    while (true)
                    {
                        // check for any server response
                        if (chat is Tcp tcpChat)
                        {
                            string received = tcpChat.ReceiveMessage() ?? "";
                            received = received.Trim();
                            if (received != "")
                            {
                                Utils.PrintError(received);
                                currentState = ChatState.EndFailure;
                                break;
                            }
                        }
                        
                        // check if there is input from the user
                        if (userInput.IsCompleted)
                        {
                            // compare it with regex
                            string input = userInput.Result ?? "";
                            if (Regex.IsMatch(input, RegexPatterns.contentAuthRegex))
                            {
                                string [] parts = input.Split(' ');
                                if (parts.Length == 4)
                                {
                                    string username = parts[1];
                                    string secret = parts[2];
                                    displayName = parts[3];
                                
                                    // authenticate the user
                                    chat.Auth(username, secret, displayName);
                                    currentState = ChatState.Auth;
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Invalid input");
                                }
                            }
                            // c-d
                            if (input == "")
                            {
                                await chat.Bye(displayName);
                                chat.Close();
                                System.Environment.Exit(0);
                            }
                            // cannot send anything besides /auth command
                            Console.WriteLine("ERROR: " + input);
                            userInput = Utils.ConsoleReadLineAsync();
                        }
                    }
                    break;
                // check for auth response
                case ChatState.Auth:
                    currentState = await chat.ReceiveAuthResponse();
                    break;
                // get to this state after successful authentication
                case ChatState.Open:    
                    Task<String?> userInputOpen = Utils.ConsoleReadLineAsync();

                    while (true)
                    {
                        currentState = await chat.ReceiveOpen();

                        if (userInputOpen.IsCompleted)
                        {
                            string inputOpen = userInputOpen.Result ?? "";
                            
                            
                            if (Regex.IsMatch(inputOpen, RegexPatterns.contentJoinRegex))
                            {
                                string[] parts = inputOpen.Split(' ');
                                if (parts.Length == 2)
                                {
                                    string channelID = parts[1];
                                    // join the channel
                                    currentState = await chat.Join(channelID, displayName);
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Invalid input");
                                }
                            }
                            else if (Regex.IsMatch(inputOpen, RegexPatterns.contentRenameRegex))
                            {
                                string[] parts = inputOpen.Split(' ');
                                if (parts.Length == 2)
                                {
                                    string displayname = parts[1];
                                    // rename the user
                                    displayName = displayname;
                                    currentState = ChatState.Open;
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Invalid input");
                                }
                            }
                            else if (inputOpen != "")
                            {
                                // invalid command
                                if (inputOpen[0] == '/')
                                {
                                    Console.WriteLine("ERROR: " + inputOpen);
                                    userInputOpen = Utils.ConsoleReadLineAsync();
                                }
                                else
                                {
                                    // send the message to the server
                                    currentState = await chat.SendMessage(inputOpen, displayName);
                                    break;
                                }
                            }
                            else
                            {
                                await chat.Bye(displayName);
                                chat.Close();
                                System.Environment.Exit(0);
                            }
                        }
                        // check if the state has changed
                        if (currentState != ChatState.Open)
                        {
                            break;
                        }
                    }
                    
                    break;
                // check for join response and get back to open state after *reply
                case ChatState.Join:
                    currentState = await chat.ReceiveJoin();
                    break;
                // end here if the user has left
                case ChatState.EndSuccess:
                    chat.Close();
                    System.Environment.Exit(0);
                    break;
                // end here if there was an error
                case ChatState.EndFailure:
                    chat.Close();
                    System.Environment.Exit(1);
                    break;
            }
        }
    }
    
}