// author: Matous Havlicek  (xhavli66)
// file for chatprotocol interface

namespace ipk25_chat;

// interface for tracking chat protocol
public interface IChatProtocol
{
    //TODO: add other methods (join, leave, etc.)
    void SendMessage(string message);
    // string ReceiveMessage();
}