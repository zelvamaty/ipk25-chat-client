// author: Matous Havlicek  (xhavli66)
// file to manage TCP chatting

using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace ipk25_chat;

// class for managing TCP chatting
public class Tcp : IChatProtocol
{
    private TcpClient? tcpClient;
    private static string messageBuffer = "";
    
    public void Close()
    {
        tcpClient?.Close();
    }
    
    // send bye message
    public Task Bye(string username)
    {
        string sendMessage = "BYE FROM " + username + "\r\n";
        byte[] data = Encoding.ASCII.GetBytes(sendMessage);
        NetworkStream? stream = tcpClient?.GetStream();
        stream?.Write(data, 0, data.Length);
        return Task.CompletedTask;
    }

    // receive message from the server
    public String? ReceiveMessage()
    {
        try
        {
            if (tcpClient == null)
                throw new ArgumentException("Client is not a valid TcpClient", nameof(tcpClient));
            
            NetworkStream stream = tcpClient.GetStream();
            
            // check if data is available 
            if (!stream.DataAvailable)
            {
                return string.Empty;
            }
                
            // buffer to store the incoming data
            byte[] buffer = new byte[4096];
            
            // read the data
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            
            // convert to a string and return
            return Encoding.ASCII.GetString(buffer, 0, bytesRead);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving TCP message: {ex.Message}");
            System.Environment.Exit(1);
            return string.Empty;
        }
        
    }

    
    // connect to server by tcp
    public void Connect(string hostname, int port)
    {
        // connect to the server
        try 
        {
            tcpClient = new TcpClient(hostname, port);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Error connecting to server: {ex.Message}");
            return;
        }
       
        
    }

    // authenticate the user /AUTH (username) (secret) (displayname)
    public void Auth(string username, string secret, string displayname)
    {
        string sendMessage = "AUTH " + username + " AS " + displayname + " USING " + secret + "\r\n";
        byte[] data = Encoding.ASCII.GetBytes(sendMessage);
        NetworkStream? stream = tcpClient?.GetStream();
        stream?.Write(data, 0, data.Length);
    }

    // receive auth response from the server and parse it
    public Task<Chat.ChatState> ReceiveAuthResponse()
    {
        while (true)
        {
            string? incomingMessage =  ReceiveMessage();
            if (incomingMessage == null)
            {
                continue; // no message received try again
            }
            //trim incoming message
            incomingMessage = incomingMessage.Trim();
            
            // if we match OK -> success, NOK -> failure, other nonempty response is an error
            if (Regex.IsMatch(incomingMessage, RegexPatterns.contentReplyOK))
            {
                // extract the content from the message
                string incomingMessageContent = Regex.Match(incomingMessage, RegexPatterns.contentReplyOK).Groups[1].Value;
                Console.WriteLine("Action Success: " + incomingMessageContent);
                return Task.FromResult(Chat.ChatState.Open);
            }
            else if (Regex.IsMatch(incomingMessage, RegexPatterns.contentReplyNOK))
            {
                string incomingMessageContent = Regex.Match(incomingMessage, RegexPatterns.contentReplyNOK).Groups[1].Value;
                Console.WriteLine("Action Failure: " + incomingMessageContent);
                return Task.FromResult(Chat.ChatState.Start);
            }
            else if (Regex.IsMatch(incomingMessage, RegexPatterns.contentError))
            {
                string displayname = Regex.Match(incomingMessage, RegexPatterns.contentError).Groups[1].Value;
                string message = Regex.Match(incomingMessage, RegexPatterns.contentError).Groups[2].Value;
                Console.WriteLine("ERROR FROM " + displayname + ": "  + message);
            }
            else if (incomingMessage != "")
            {
                Console.WriteLine("ERROR: " + incomingMessage);
                return Task.FromResult(Chat.ChatState.EndFailure);
            }
        }
    }

    public Task<Chat.ChatState> ReceiveOpen()
    {
        // check for any server response and print it out when we get full message
        string? received = ReceiveMessage();
        messageBuffer += received;
        while (true)
        {
            // split the message by \r\n and check if we have a full message, leftovers get saved to the buffer
            string[] splitMessages = messageBuffer.Split("\r\n", 2);
            if (splitMessages.Length > 1)
            {
                messageBuffer = splitMessages[1];
                string result = ParseMessage(splitMessages[0]);
                if (result == "malformed")
                {
                    Utils.PrintError(splitMessages[0]);
                    SendErrFrom(splitMessages[0], Chat.displayName);
                    return Task.FromResult(Chat.ChatState.EndFailure);
                }
                if (result == "BYE")
                {
                    string displayname = Regex.Match(splitMessages[0], RegexPatterns.contentBye).Groups[1].Value;
                    Console.WriteLine("BYE FROM " + displayname);
                    return Task.FromResult(Chat.ChatState.EndSuccess);
                }
                if (result == "ERROR")
                {
                    string displayname = Regex.Match(splitMessages[0], RegexPatterns.contentError).Groups[1].Value;
                    string message = Regex.Match(splitMessages[0], RegexPatterns.contentError).Groups[2].Value;
                    Utils.PrintErrorFrom(displayname, message);
                    return Task.FromResult(Chat.ChatState.EndFailure);
                }
                if (result == "NOK")
                {
                    SendErrFrom(splitMessages[0], Chat.displayName);
                    return Task.FromResult(Chat.ChatState.EndFailure);
                }
                if (result == "OK")
                {
                    SendErrFrom(splitMessages[0], Chat.displayName);
                    return Task.FromResult(Chat.ChatState.EndFailure);
                }
            }
            else
            {
                break;
            }

        }
        return Task.FromResult(Chat.ChatState.Open);
    }
    
    // receive join response from the server and parse it
    public Task<Chat.ChatState> ReceiveJoin()
    { 
        string? received = ReceiveMessage();
        messageBuffer += received;
        while (true)
        {
            string[] splitMessages = messageBuffer.Split("\r\n", 2);
            if (splitMessages.Length > 1)
            {
                // save the rest of the message to the buffer
                messageBuffer = splitMessages[1];
                
                string result = ParseMessage(splitMessages[0]);
                if (result == "malformed")
                {
                    Utils.PrintError(splitMessages[0]);
                    SendErrFrom(splitMessages[0], Chat.displayName);
                    return Task.FromResult(Chat.ChatState.EndFailure);
                }
                // check if the message is a reply to the join command
                if (result == "OK")
                {
                    string incomingMessageContent = Regex.Match(splitMessages[0], RegexPatterns.contentReplyOK).Groups[1].Value;

                    Console.WriteLine("Action Success: " + incomingMessageContent);
                    return Task.FromResult(Chat.ChatState.Open);
                }
                if (result == "NOK")
                {
                    string incomingMessageContent = Regex.Match(splitMessages[0], RegexPatterns.contentReplyNOK).Groups[1].Value;
                    Console.WriteLine("Action Failure: " + incomingMessageContent);
                    return Task.FromResult(Chat.ChatState.Open);
                }
                if (result == "BYE")
                {
                    string displayname = Regex.Match(splitMessages[0], RegexPatterns.contentBye).Groups[1].Value;
                    Console.WriteLine("BYE FROM " + displayname);
                    return Task.FromResult(Chat.ChatState.EndSuccess);
                }
                if (result == "ERROR")
                {
                    string displayname = Regex.Match(splitMessages[0], RegexPatterns.contentError).Groups[1].Value;
                    string message = Regex.Match(splitMessages[0], RegexPatterns.contentError).Groups[2].Value;
                    Utils.PrintErrorFrom(displayname, message);
                    return Task.FromResult(Chat.ChatState.EndFailure);
                }
            }
            else
            {
                break;
            }
        }

        return Task.FromResult(Chat.ChatState.Join);
    }

    // parse message from the server
    public string ParseMessage(string message)
    {
        if (Regex.IsMatch(message, RegexPatterns.contentMessage))
        {
            string displayname = Regex.Match(message, RegexPatterns.contentMessage).Groups[1].Value;
            string content = Regex.Match(message, RegexPatterns.contentMessage).Groups[2].Value;
            Utils.PrintMessage(displayname, content);
            return "MESSAGE";
        }
        else if (Regex.IsMatch(message, RegexPatterns.contentReplyNOK))
        {
            return "NOK";
        }
        else if (Regex.IsMatch(message, RegexPatterns.contentReplyOK))
        {
            return "OK";
        }
        else if (Regex.IsMatch(message, RegexPatterns.contentBye))
        {
            return "BYE";
        }
        else if (Regex.IsMatch(message, RegexPatterns.contentError))
        {
            return "ERROR";
        }
        return "malformed";
    }
    
    // send join message to the server
    public Task<Chat.ChatState> Join(string channelID, string displayname)
    {
        string sendMessage = "JOIN " + channelID + " AS " + displayname + "\r\n";
        byte[] data = Encoding.ASCII.GetBytes(sendMessage);
        NetworkStream? stream = tcpClient?.GetStream();
        stream?.Write(data, 0, data.Length);
        return Task.FromResult(Chat.ChatState.Join);
    }
    
    // send message to the server
    public Task<Chat.ChatState> SendMessage(string message, string displayname)
    {
        // cut message to 60000 characters
        if (message.Length > 60000)
        {
            message = message.Substring(0, 60000);
        }
        string sendMessage = "MSG FROM " + displayname + " IS " + message + "\r\n";
        byte[] data = Encoding.ASCII.GetBytes(sendMessage);
        NetworkStream? stream = tcpClient?.GetStream();
        stream?.Write(data, 0, data.Length);
        return Task.FromResult(Chat.ChatState.Open);
    }
    
    // send error message to the server
    public void SendErrFrom(string message, string displayname)
    {
        // cut message to 60000 characters
        if (message.Length > 60000)
        {
            message = message.Substring(0, 60000);
        }
        string sendMessage = "ERR FROM " + displayname + " IS " + message + "\r\n";
        byte[] data = Encoding.ASCII.GetBytes(sendMessage);
        NetworkStream? stream = tcpClient?.GetStream();
        stream?.Write(data, 0, data.Length);
    }

}