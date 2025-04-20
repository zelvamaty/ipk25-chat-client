// author: Matous Havlicek  (xhavli66)
// file for chatprotocol interface

using System.Net.Sockets;

namespace ipk25_chat;

// interface for chat protocols methods
public interface IChatProtocol
{
    Task<Chat.ChatState> SendMessage(string message, string displayname);
    void Close();
    void Connect(string hostname, int port);
    void Auth(string username, string secret, string displayname);

    Task<Chat.ChatState> ReceiveAuthResponse();
    Task<Chat.ChatState> ReceiveOpen();
    Task<Chat.ChatState> ReceiveJoin();
    Task Bye(string username);
    Task<Chat.ChatState> Join(string channelID, string displayname);

}