// author: Matous Havlicek  (xhavli66)
// file to manage TCP chatting

namespace ipk25_chat;

// class for managing TCP chatting
public class Tcp : IChatProtocol
{
    public void SendMessage(string message)
    {
        Console.WriteLine(message + "TCP");
    }
}