// See https://aka.ms/new-console-template for more information

using System.Drawing;
using System.Net;
using Common;
using Console = Colorful.Console;

namespace LineReversal;

public class Program
{
    static readonly Dictionary<int, Session> _sessions = new();

    private static async Task Main(string[] args)
    {
        Console.WriteAscii("Line Reversal");

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
            return true;
        }
        var parts = dataMessage.Split('/');
        if (parts.Length < 2)
        {
            return true;
        }

        var client = int.Parse(parts[2]);
        var kind = parts[1];
        switch (kind)
        {
            case "connect":
                await Send($"/ack/{client}/0/");
                _sessions.TryAdd(client, new Session());

                break;
            case "data":
                var messagePosition = int.Parse(parts[3]);
                if (!_sessions.ContainsKey(client))
                {
                    await Close();
                    break;
                }

                var session = _sessions[client];
                if (session.Messages.TryGetValue(messagePosition, out var sessionMessage))
                {
                    await Send($"/ack/{client}/{GetUnescapedLengthMessage(sessionMessage)}/");
                    break;
                }

                var message = parts[4];
                session.Messages.Add(messagePosition, message);
                await Send($"/ack/{client}/{GetUnescapedLengthMessage(message) + messagePosition}/");
                session.OnGoingLine += message;
                await SendLines(session, messagePosition);
                break;
            case "ack":
                var length = int.Parse(parts[3]);
                if (!_sessions.ContainsKey(client))
                {
                    await Close();
                    break;
                }

                if (length >= _sessions[client].Messages.Select(m => GetUnescapedLengthMessage(m.Value)).Max())
                {
                    break;
                }

                var totalPayloadSent = _sessions[client].Messages.Select(m => GetUnescapedLengthMessage(m.Value)).Sum();
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

        async Task Send(string message)
        {
            await listener.Reply2(message);
        }

        async Task SendLines(Session session, int messagePosition)
        {
            var values = session.OnGoingLine.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            var finishWithNewLine = session.OnGoingLine.Last() == '\n';
            var message = "";
            foreach (var line in values[..^1])
            {
                message += GetMessage(line);
            }

            if (finishWithNewLine)
            {
                message += GetMessage(values.Last());
                session.OnGoingLine = "";
            }
            else
            {
                session.OnGoingLine = values.Last();
            }

            if (message.Length > 0)
            {
                await Send($"/data/{client}/{messagePosition}/{message}/");
            }
        }

        async Task ReSendLines(Session session, int messagePosition)
        {
            var values = string.Concat(session.Messages.Values);
            session.OnGoingLine = values[messagePosition..];
            await SendLines(session, messagePosition);
        }
    }


    static string GetMessage(string value)
    {
        return string.Concat(value.Reverse()) + "\n";
        // var values = value.Split('\n');
        // return string.Concat(values.Select(v => v.Length == 0 ? "\n" : string.Concat(v.Reverse())));
    }

    static int GetUnescapedLengthMessage(string message)
    {
        return message.Length;
    }
}

public interface IReplier
{
    Task Reply2(string message);
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

    public async Task Reply2(string message)
    {
        await _listener.Reply2(message, _ipEndPoint);
    }
}

class Session
{
    public Dictionary<int, string> Messages { get; } = new();

    public string OnGoingLine = "";
}