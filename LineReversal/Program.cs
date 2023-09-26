// See https://aka.ms/new-console-template for more information

using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using Common;
using Console = Colorful.Console;

namespace LineReversal;

public class Program
{
    static readonly Dictionary<int, Session> _sessions = new();
    static bool _shouldWriteInFile = false;

    private static async Task Main(string[] args)
    {
        Console.WriteAscii("Line Reversal");
        if(Directory.Exists("output"))
        {
            Directory.Delete("output", true);
            Directory.CreateDirectory("output");
        }
        else
        {
            Directory.CreateDirectory("output");
        }

        _shouldWriteInFile = true;
        await CommonServer.NewUdp().HandleString(HandleString);

    }
    static async Task<bool> HandleString(UdpListener listener, Received data)
    {
        Console.Write($"<< ", Color.DarkGray);
        Console.Write(data.Message, Color.GreenYellow);
        Console.WriteLine($" {data.Sender}", Color.Gray);
        if (data.Message.FirstOrDefault() != '/')
        {
            return true;
        }
        var parts = data.Message.Split('/');
        if (parts.Length < 2)
        {
            return true;
        }
        
        return await ProcessKind(new Replier(listener, data.Sender), data.Message);
    }

    public static async Task<bool> ProcessKind(IReplier listener, string dataMessage)
    {
        if (dataMessage.FirstOrDefault() != '/')
        {
            return false;
        }
        var parts = dataMessage.Split('/');
        if (parts.Length < 2)
        {
            return false;
        }

        var client = int.Parse(parts[2]);
        var message = parts.Length > 4 ? parts[4] : "";

        WriteInFile($"await LineReversal.Program.ProcessKind(replier.Object, \"{Regex.Unescape(dataMessage)}\"); // {message.Length}", client);
        if (dataMessage.Last() != '/')
        {
            return false;
        }

        var kind = parts[1];
        switch (kind)
        {
            case "connect":
                await Send($"/ack/{client}/0/");
                _sessions.TryAdd(client, new Session());

                break;
            case "data":
                if(parts.Length != 6) // ""/"data"/"client"/"pos"/"message"/"" 
                {
                    return false;
                }
                var messagePosition = int.Parse(parts[3]);
                if (!_sessions.ContainsKey(client))
                {
                    await Close();
                    break;
                }

                var session = _sessions[client];
                
                int finalPosition = message.Length + messagePosition;
                if (finalPosition <= session.Message.Length || session.Message.Length < messagePosition)
                {
                    await Send($"/ack/{client}/{session.Message.Length}/");
                    break;
                }
                
                await Send($"/ack/{client}/{finalPosition}/");

                var cuttedMessage = message[(session.Message.Length - messagePosition)..];
                session.Message += cuttedMessage;
                
                if (cuttedMessage.Length > 0)
                {
                    session.OnGoingLine += cuttedMessage;
                    await SendLines(session);
                }

                break;
            case "ack":
                var length = int.Parse(parts[3]);
                if (!_sessions.ContainsKey(client))
                {
                    await Close();
                    break;
                }



                if (length >= _sessions[client].MessagesToAck.Select(m => m.Value.Length).Max())
                {
                    break;
                }

                var totalPayloadSent = _sessions[client].Message.Length;
                if (length > totalPayloadSent)
                {
                    await Close();
                    break;
                }

                if (length < totalPayloadSent)
                {
                    if (!_sessions.ContainsKey(client))
                    {
                        await Close();
                        break;
                    }

                    await ReSendLines(_sessions[client], length);
                }

                break;
            case "close":
                await Close();
                break;
        }

        return false;

        async Task Close()
        {
            await Send($"/close/{client}/");
            _sessions.Remove(client);
        }

        async Task Send(string message, int dataLength = 0)
        {
            WriteInFile("replier.Verify(r => r.Reply(\"" + Regex.Unescape(message) + $"\")); // {dataLength}", client);
            await listener.Reply(message);
        }

        async Task SendLines(Session session)
        {
            var values = session.OnGoingLine.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            var finishWithNewLine = session.OnGoingLine.Last() == '\n';
            if (finishWithNewLine)
            {
                session.OnGoingLine = "";
                foreach (var line in values)
                {
                    session.OnGoingLine += GetMessage(line);
                }

                var messagePos = session.Message.Length - session.OnGoingLine.Length;
                await Send($"/data/{client}/{messagePos}/{session.OnGoingLine}/", session.OnGoingLine.Length);
                session.MessagesToAck.Add(messagePos, session.OnGoingLine);
                session.OnGoingLine = "";
            }
        }

        async Task ReSendLines(Session session, int messagePosition)
        {
            session.OnGoingLine = session.Message[messagePosition..];
            await SendLines(session);
        }
    }

    private static void WriteInFile(string message, int client)
    {
        if (!_shouldWriteInFile)
        {
            return;
        }

        using var sw = File.AppendText(Path.Combine("output", $"{client}.txt"));
        sw.WriteLine(message);
    }


    static string GetMessage(string value)
    {
        return string.Concat(value.Reverse()) + "\n";
    }
}

public interface IReplier
{
    Task Reply(string message);
}

public class Replier : IReplier
{
    private readonly UdpListener _listener;
    private readonly IPEndPoint _ipEndPoint;

    public Replier(UdpListener listener, IPEndPoint ipEndPoint)
    {
        _listener = listener;
        _ipEndPoint = ipEndPoint;
    }

    public async Task Reply(string message)
    {
        await _listener.Reply2(message, _ipEndPoint);
    }
}

class Session
{
    public Dictionary<int, string> MessagesToAck { get; } = new();

    public string Message { get; set; } = "";

    public string OnGoingLine = "";
}