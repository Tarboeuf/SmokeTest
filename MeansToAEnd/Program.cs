﻿// See https://aka.ms/new-console-template for more information
using Common;
using System.Net.Sockets;
using System.Runtime.InteropServices;

await CommonServer.NewTcp()
    .HandleFixedSize<Dictionary<int, int>>(9, Handle);

async Task<bool> Handle(Socket socket, byte[] buffer, Dictionary<int, int> dataBase)
{

    var request = new Request
    {
        Type = buffer[0] == 73 ? RequestType.Insert : buffer[0] == 81 ? RequestType.Query : RequestType.Error,
        Value1 = BitConverter.ToInt32(buffer.Reverse().ToArray(), 4),
        Value2 = BitConverter.ToInt32(buffer.Reverse().ToArray(), 0),
    };

    Console.WriteLine($"Receiving ({9}) : {string.Join(' ', buffer)} ==> {request}");

    switch (request.Type)
    {
        case RequestType.Insert:
            dataBase.Add(request.Value1, request.Value2);
            break;
        case RequestType.Query:
            var values = dataBase.Where(v => v.Key >= request.Value1 && v.Key <= request.Value2).Select(v => v.Value).ToList();
            int result = 0;
            if (values.Count > 0)
            {
                result = (int)values.Average();
            }
            Console.WriteLine($"Send : {result}");
            var bytes = BitConverter.GetBytes(result).Reverse().ToArray();
            await socket.SendAsync(new ArraySegment<byte>(bytes), SocketFlags.None);
            break;
        case RequestType.Error:
            return true;
        default:
            break;
    }

    return false;
}

class Request
{
    public RequestType Type { get; set; }
    public int Value1 { get; set; }
    public int Value2 { get; set; }

    public override string ToString()
    {
        return $"{Type} {Value1} {Value2}";
    }
}

enum RequestType
{
    Insert, 
    Query,
    Error
}