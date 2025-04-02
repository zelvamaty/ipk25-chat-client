// author: Matous Havlicek  (xhavli66)
// file to manage UDP chatting

namespace ipk25_chat;

// class for managing UDP chatting
public class Udp : IChatProtocol
{
    public void SendMessage(string message)
    {
        Console.WriteLine(message + "UDP");
    }
    
}