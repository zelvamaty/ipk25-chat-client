// author: Matous Havlicek  (xhavli66)
// file to manage which chatting protocol to start

namespace ipk25_chat;

// class for managing which chat to start
public class Chat
{
    public static void BeginChatting(string[] args)
    {
        // parse the arguments
        ProgramArguments arguments = ArgumentParsing.ParseArguments(args);
        // create the chat protocol    
        IChatProtocol chat;
        
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
        //TODO implement FSM here
        chat.SendMessage("Hello world!");
    }
}