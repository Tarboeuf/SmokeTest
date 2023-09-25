﻿// See https://aka.ms/new-console-template for more information

using System.Drawing;
using System.Net;
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
            File.Delete("output/*");
        }
        else
        {
            Directory.CreateDirectory("ouput");
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
        
        WriteInFile($"await LineReversal.Program.ProcessKind(replier.Object, \"{dataMessage.Replace("\n", "\\n")}\");", client);
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
                var message = parts[4];
                if (session.Messages.TryGetValue(messagePosition, out var sessionMessage))
                {
                    session.Messages.Remove(messagePosition);
                }
                
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
            WriteInFile("replier.Verify(r => r.Reply(\"" + message.Replace("\n", "\\n") + "\"));", client);
            await listener.Reply(message);
        }

        async Task SendLines(Session session, int messagePosition)
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
            
                await Send($"/data/{client}/{session.Messages.Values.Sum(v => v.Length) - session.OnGoingLine.Length}/{session.OnGoingLine}/");
                session.OnGoingLine = "";
            }
        }

        async Task ReSendLines(Session session, int messagePosition)
        {
            var values = string.Concat(session.Messages.Values);
            session.OnGoingLine = values[messagePosition..];
            await SendLines(session, session.Messages.Values.Sum(v => v.Length) - messagePosition - session.OnGoingLine.Length);
        }
    }

    private static void WriteInFile(string message, int client)
    {
        if (!_shouldWriteInFile)
        {
            return;
        }

        using var sw = File.AppendText(Path.Combine("ouput", $"{client}.txt"));
        sw.WriteLine(message);
    }


    static string GetMessage(string value)
    {
        return string.Concat(value.Reverse()) + "\n";
    }

    static int GetUnescapedLengthMessage(string message)
    {
        return message.Length;
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
    public Dictionary<int, string> Messages { get; } = new();

    public string OnGoingLine = "";
}