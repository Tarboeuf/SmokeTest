// See https://aka.ms/new-console-template for more information

using System.Drawing;
using Common;
using Colorful;
using Console = Colorful.Console;
using System.Net;
using System.Net.Sockets;

internal class Program
{
    static Dictionary<int, Session> sessions = new Dictionary<int, Session>();

    private static async Task Main(string[] args)
    {
        Console.WriteAscii("Line Reversal");

        await CommonServer.NewUdp().HandleString(HandleString);

    }
    async static Task<bool> HandleString(UdpListener listener, Received data)
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

        int client = int.Parse(parts[2]);

        switch (parts[1])
        {
            case "connect":
                await Send($"/ack/{client}/0/");
                if (!sessions.ContainsKey(client))
                {
                    sessions.Add(client, new Session(client));
                }
                break;
            case "data":
                int messagePosition = int.Parse(parts[3]);
                if (!sessions.ContainsKey(client))
                {
                    await Close(client);
                    break;
                }
                var session = sessions[client];
                if (session.Messages.ContainsKey(messagePosition))
                {
                    await Send($"/ack/{client}/{GetUnescapedLengthMessage(session.Messages[messagePosition])}/");
                    break;
                }
                string message = parts[4];
                session.Messages.Add(messagePosition, message);
                await Send($"/ack/{client}/{GetUnescapedLengthMessage(message)}/");
                session.OnGoingLine += message;
                await SendLines(session, messagePosition);
                break;
            case "ack":
                int length = int.Parse(parts[3]);
                if (!sessions.ContainsKey(client))
                {
                    await Close(client);
                    break;
                }
                if (length >= sessions[client].Messages.Select(m => GetUnescapedLengthMessage(m.Value)).Max())
                {
                    break;
                }
                int totalPayloadSent = sessions[client].Messages.Select(m => GetUnescapedLengthMessage(m.Value)).Sum();
                if (length > totalPayloadSent)
                {
                    await Close(client);
                    break;
                }
                if (length < totalPayloadSent)
                {
                    if (!sessions.ContainsKey(client))
                    {
                        await Close(client);
                        break;
                    }
                    await ReSendLines(sessions[client], length);
                    break;
                }
                break;
            case "close":
                await Close(client);
                break;
        }
        return false;
        async Task Close(int client)
        {
            await Send($"/close/{client}/");
            sessions.Remove(client);
        }
        async Task Send(string message)
        {
            await listener.Reply2(message, data.Sender);
        }
        async Task SendLines(Session session, int messagePosition, bool newSending = true)
        {
            var values = session.OnGoingLine.Split(new []{'\n'}, StringSplitOptions.RemoveEmptyEntries);
            bool finishWithNewLine = session.OnGoingLine.Last() == '\n';
            string message = "";
            foreach (string line in values[..^1])
            {
                message += GetMessage(line);
            }
            if(finishWithNewLine)
            {
                message += GetMessage(values.Last());
                session.OnGoingLine = "";
            }
            else
            {
                session.OnGoingLine = values.Last();
            }
            if(message.Length >= 0)
            {
                await Send($"/data/{client}/{messagePosition}/{message}/");
            }
        }
        async Task ReSendLines(Session session, int messagePosition)
        {
            var values = string.Concat(session.Messages.Values);
            session.OnGoingLine = values[messagePosition..];
            await SendLines(session, messagePosition, false);
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

class Session
{
    public int Client { get; set; }

    public Session(int client)
    {
        Client = client;
    }

    public Dictionary<int, string> Messages { get; } = new Dictionary<int, string>();

    public string OnGoingLine = "";

    public string SentData = "";
}