// author: Matous Havlicek  (xhavli66)
// file to manage UDP chatting

using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ipk25_chat;

// class for managing UDP chatting
public class Udp : IChatProtocol
{
    private UdpClient? udpClient;
    private IPEndPoint? currendEndPoint;
    private int messageIdCounter;
    private List<int> receivedMessageIds = new List<int>(); //received ids from server
    private List<int> sentMessageIds = new List<int>(); //received confirms for sent messages
    
    // construct a message, send it to the server and wait for a confirm + raise messageIdCounter
    public async Task<Chat.ChatState> SendMessage(string message, string displayname)
    {
        // cut message to 60000 characters
        if (message.Length > 60000)
        {
            message = message.Substring(0, 60000);
        }
        
        using (MemoryStream mStream = new MemoryStream())
        using (BinaryWriter buildPacket = new BinaryWriter(mStream))
        {
            // convert username, secret and displayname to byte arrays
            byte[] bytesMessageContent = Encoding.ASCII.GetBytes(message);
            byte[] bytesDisplayname = Encoding.ASCII.GetBytes(displayname);
            
            buildPacket.Write((byte)0x04); // msg message
            
            int currentMessageId = messageIdCounter;
            byte[] messageId = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)currentMessageId));;
            buildPacket.Write(messageId);
            buildPacket.Write(bytesDisplayname);
            buildPacket.Write((byte)0x00); // null terminator
            buildPacket.Write(bytesMessageContent);
            buildPacket.Write((byte)0x00); 
            
            // convert to byte array and send to the server
            byte[] packet = mStream.ToArray();
            udpClient?.Send(packet, packet.Length, currendEndPoint);

            if (!await ReceiveConfirm(currentMessageId))
            {
                return Chat.ChatState.EndFailure;
            }
            messageIdCounter++;

            return Chat.ChatState.Open;
        }
    }
    // close the udp client
    public void Close()
    {
        udpClient?.Close();
    }
    
    // send bye message to the server and wait for confirm
    public async Task Bye(string username)
    {
        using (MemoryStream mStream = new MemoryStream())
        using (BinaryWriter buildPacket = new BinaryWriter(mStream))
        {
            // convert username, secret and displayname to byte arrays
            byte[] bytesUsername = Encoding.ASCII.GetBytes(username);
            buildPacket.Write((byte)0xFF); // bye message
            
            int currentMessageId = messageIdCounter;
            byte[] messageId = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)currentMessageId));
            buildPacket.Write(messageId);
            buildPacket.Write(bytesUsername);
            buildPacket.Write((byte)0x00); // null terminator
            
            // convert to byte array and send to the server
            byte[] packet = mStream.ToArray();
            udpClient?.Send(packet, packet.Length, currendEndPoint);
            await ReceiveConfirm(currentMessageId);

        }
    }
    // receive message from the server
    public async Task<Byte[]?> ReceiveMessage(bool updatePort = false)
    {
        try
        {
            if (udpClient == null)
            {
                throw new ArgumentException("Client is not a valid UdpClient", nameof(udpClient));

            }
            
            using var cts = new CancellationTokenSource(Chat.udpTimeout);
            var receiveTask = udpClient.ReceiveAsync(cts.Token);
            var receive =  await receiveTask;
            if (updatePort)
            {
                currendEndPoint = receive.RemoteEndPoint;
            }
            return receive.Buffer;
        }
        catch (Exception)
        {
            return null;
        }
    }
    
    // connect to server by udp
    public void Connect(string hostname, int port)
    {
        // create udpclient and connect to the srever
        udpClient = new UdpClient(0);
        // translate hostname to ip address (ipv4)
        IPAddress[] ipAddresses = Dns.GetHostAddresses(hostname, AddressFamily.InterNetwork);
        IPAddress ipAddress = ipAddresses[0];
        currendEndPoint = new IPEndPoint(ipAddress, port);
        udpClient.Client.ReceiveTimeout = Chat.udpTimeout;
        
    }
    
    // send auth message to the server
    public void Auth(string username, string secret, string displayname)
    {
        using (MemoryStream mStream = new MemoryStream())
        using (BinaryWriter buildPacket = new BinaryWriter(mStream))
        {
            // convert username, secret and displayname to byte arrays
            byte[] bytesUsername = Encoding.ASCII.GetBytes(username);
            byte[] bytesSecret = Encoding.ASCII.GetBytes(secret);
            byte[] bytesDisplayname = Encoding.ASCII.GetBytes(displayname);
            
            buildPacket.Write((byte)0x02); // auth message
            
            int currentMessageId = messageIdCounter;
            byte[] messageId = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)currentMessageId));;
            buildPacket.Write(messageId);
            buildPacket.Write(bytesUsername);
            buildPacket.Write((byte)0x00); // null terminator
            buildPacket.Write(bytesDisplayname);
            buildPacket.Write((byte)0x00);
            buildPacket.Write(bytesSecret);
            buildPacket.Write((byte)0x00); 
            
            // convert to byte array and send to the server
            byte[] packet = mStream.ToArray();
            udpClient?.Send(packet, packet.Length, currendEndPoint);
        }
    }

    // receive auth response from the server
    public async Task<Chat.ChatState> ReceiveAuthResponse()
    {
        if (!await ReceiveConfirm(messageIdCounter))
        {
            return Chat.ChatState.EndFailure;
        }
        while (true)
        {
            Byte[]? incomingMessage = await ReceiveMessage(true);
            if (incomingMessage == null)
            {
                continue;
            }
            
            string result = ParseMessage(incomingMessage, false);
            
            if (result == "malformed")
            {
                Utils.PrintErrorBytes(incomingMessage);
                await SendErrFrom(incomingMessage, Chat.displayName);
                return Chat.ChatState.EndFailure;
            }
            if (result == "BYE")
            {
                return Chat.ChatState.EndSuccess;
            }
            if (result == "ERROR")
            {
                return Chat.ChatState.EndFailure;
            }
            if (result == "NOK")
            {
                return Chat.ChatState.Start;
            }
            if (result == "OK")
            {
                return Chat.ChatState.Open;
            }
            if (result == "MESSAGE")
            {
                await SendErrFrom(incomingMessage, Chat.displayName);
                return Chat.ChatState.EndFailure;
            }
        }
    }
    
    // receive open state messages from the server
    public async Task<Chat.ChatState> ReceiveOpen()
    {
        // check for any server response and print it out when we get full message
        byte[]? incomingMessage = await ReceiveMessage();

        if (incomingMessage == null)
        {
            return Chat.ChatState.Open;
        }

        string result = ParseMessage(incomingMessage);
        if (result == "malformed")
        {
            Utils.PrintErrorBytes(incomingMessage);
            await SendErrFrom(incomingMessage, Chat.displayName);
            return Chat.ChatState.EndFailure;
        }
        if (result == "BYE")
        {
            return Chat.ChatState.EndSuccess;
        }
        if (result == "ERROR")
        {
            return Chat.ChatState.EndFailure;
        }
        if (result == "NOK")
        {
            await SendErrFrom(incomingMessage, Chat.displayName);
            return Chat.ChatState.EndFailure;
        }
        if (result == "OK")
        {
            await SendErrFrom(incomingMessage, Chat.displayName);
            return Chat.ChatState.EndFailure;
        }

        return Chat.ChatState.Open;
    }
    
    // receive join state messages from the server
    public async Task<Chat.ChatState> ReceiveJoin()
    {
        byte[]? incomingMessage = await ReceiveMessage();

        if (incomingMessage == null)
        {
            return Chat.ChatState.Open;
        }

        string result = ParseMessage(incomingMessage);
        if (result == "malformed")
        {
            Utils.PrintErrorBytes(incomingMessage);
            await SendErrFrom(incomingMessage, Chat.displayName);
            return Chat.ChatState.EndFailure;
        }
        if (result == "BYE")
        {
            return Chat.ChatState.EndSuccess;
        }
        if (result == "ERROR")
        {
            return Chat.ChatState.EndFailure;
        }

        return Chat.ChatState.Join;
    }
    
    // parse various messages from the server
    public string ParseMessage(byte[] message, bool allowMessage = true)
    {
        if (message.Length == 3 && message[0] == 0x00) // CONFIRM
        {
            int serverMessageId = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(message,1));
            if (sentMessageIds.Contains(serverMessageId))
            {
                return "NOTHING";
            }
            return "malformed";
        }
        else if (message.Length == 3 && message[0] == 0xFD) // PING
        {
            int serverMessageId = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(message,1));
            SendConfirm(serverMessageId);
            return "PING";
        }
        else if (message.Length >= 5 && message[0] == 0x04) // MSG
        {
            if (allowMessage)
            {
                int serverMessageId = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(message,1));
                // 3 = header
                byte[] displayNameBytes = message.Skip(3).TakeWhile(b => b != 0x00).ToArray();
                // 3 = header, 1 = null terminator
                byte[] contentBytes = message.Skip(3 + 1 + displayNameBytes.Length).TakeWhile(b => b != 0x00).ToArray();

                if (!receivedMessageIds.Contains(serverMessageId))
                {
                    Utils.PrintMessage(Encoding.ASCII.GetString(displayNameBytes), Encoding.ASCII.GetString(contentBytes));
                    receivedMessageIds.Add(serverMessageId);
                }
            
                SendConfirm(serverMessageId);
            }
            else
            {
                Utils.PrintErrorBytes(message);
            }

            
            return "MESSAGE";
        }
        else if (message.Length >= 7 && message[0] == 0x01) // REPLY
        {
            int serverMessageId = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(message,1));
            var result = message[3];
            
            int currentMessageId = messageIdCounter;
            byte[] expectedMessageIdBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)currentMessageId));
            if (!Utils.CheckByteArrayEquals(expectedMessageIdBytes, message.Skip(4).Take(2).ToArray()))
            { //check ref message ID
                return "malformed";
            }
            
            // extract the content from the message
            var bytesContent = message.Skip(6).Take(message.Length - 6 - 1).ToArray();
            string incomingMessageContent = Encoding.ASCII.GetString(bytesContent);
            
            if (result == 1)
            {
                Console.WriteLine("Action Success: " + incomingMessageContent);
                SendConfirm(serverMessageId);
                messageIdCounter++;
                return "OK";
            }
            else if (result == 0)
            {
                Console.WriteLine("Action Failure: " + incomingMessageContent);
                SendConfirm(serverMessageId);
                messageIdCounter++;
                return "NOK";
            }
            else
            {
                Utils.PrintErrorBytes(message);
                return "malformed";
            }
        }
        else if (message.Length >= 4 && message[0] == 0xFF) // BYE
        {
            int serverMessageId = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(message,1));
            // 3 = header
            byte[] displayNameBytes = message.Skip(3).TakeWhile(b => b != 0x00).ToArray();
            string displayname = Encoding.ASCII.GetString(displayNameBytes);
            Console.WriteLine("BYE FROM " + displayname);
            SendConfirm(serverMessageId);
            return "BYE";
        }
        else if (message.Length >= 5 && message[0] == 0xFE) // ERROR
        {
            int serverMessageId = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(message,1));
            // 3 = header
            byte[] displayNameBytes = message.Skip(3).TakeWhile(b => b != 0x00).ToArray();
            string displayname = Encoding.ASCII.GetString(displayNameBytes);
            byte[] contentBytes = message.Skip(3 + 1 + displayNameBytes.Length).TakeWhile(b => b != 0x00).ToArray();
            string content = Encoding.ASCII.GetString(contentBytes);
            Utils.PrintErrorFrom(displayname, content);
            SendConfirm(serverMessageId);
            return "ERROR";
        }
        return "malformed";
    }

    // send confirm message to the server with correct message ID
    public void SendConfirm(int messageId)
    {
        using (MemoryStream mStream = new MemoryStream())
        using (BinaryWriter buildPacket = new BinaryWriter(mStream))
        {
            buildPacket.Write((byte)0x00); // confirm message
            
            byte[] messageIdBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)messageId));
            buildPacket.Write(messageIdBytes); 
            
            // convert to byte array and send to the server
            byte[] packet = mStream.ToArray();
            udpClient?.Send(packet, packet.Length, currendEndPoint);
        }
    }

    // receive confirm message from the server with correct message ID
    public async Task<bool> ReceiveConfirm(int messageId)
    {
        Byte[]? incomingMessage = await ReceiveMessage();
        if (incomingMessage == null || 
            incomingMessage.Length != 3 || 
            incomingMessage[0] != 0x00)
        {
            Utils.PrintError("no confirm received");
            return false; // no message received or wrong format
        }
        
        int serverMessageId = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(incomingMessage,1));

        if (serverMessageId == messageId)
        {
            sentMessageIds.Add(messageId);
            return true;
        }
        Utils.PrintError("incorrect confirm received");
        return false;
    }

    // send join message to the server and receive confirm
    public async Task<Chat.ChatState> Join(string channelID, string displayname)
    {
        using (MemoryStream mStream = new MemoryStream())
        using (BinaryWriter buildPacket = new BinaryWriter(mStream))
        {
            // convert username, secret and displayname to byte arrays
            byte[] bytesChannelId = Encoding.ASCII.GetBytes(channelID);
            byte[] bytesDisplayname = Encoding.ASCII.GetBytes(displayname);
            
            buildPacket.Write((byte)0x03); // join message
            
            int currentMessageId = messageIdCounter;
            byte[] messageId = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)currentMessageId));
            buildPacket.Write(messageId);
            buildPacket.Write(bytesChannelId);
            buildPacket.Write((byte)0x00); // null terminator
            buildPacket.Write(bytesDisplayname);
            buildPacket.Write((byte)0x00); 
            
            // convert to byte array and send to the server
            byte[] packet = mStream.ToArray();
            udpClient?.Send(packet, packet.Length, currendEndPoint);

            if (!await ReceiveConfirm(currentMessageId))
            {
                return Chat.ChatState.EndFailure;
            }
        }
        return Chat.ChatState.Join;
    }
    
    // send error message to the server and receive confirm
    public async Task SendErrFrom(byte[] message, string displayname)
    {
        using (MemoryStream mStream = new MemoryStream())
        using (BinaryWriter buildPacket = new BinaryWriter(mStream))
        {
            // convert username, secret and displayname to byte arrays
            byte[] bytesDisplayname = Encoding.ASCII.GetBytes(displayname);
            
            buildPacket.Write((byte)0xFE); // ERR message
            
            int currentMessageId = messageIdCounter;
            byte[] messageId = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)currentMessageId));;
            buildPacket.Write(messageId);
            buildPacket.Write(bytesDisplayname);
            buildPacket.Write((byte)0x00); // null terminator
            buildPacket.Write(message);
            buildPacket.Write((byte)0x00);
            
            // convert to byte array and send to the server
            byte[] packet = mStream.ToArray();
            udpClient?.Send(packet, packet.Length, currendEndPoint);
            await ReceiveConfirm(currentMessageId);
        }
    }
    
    
}