﻿using Common;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("Starting prime server");
await CommonServer.NewTcp()
    .HandleString(Handle);

async Task<bool> Handle(TcpClient socket, string data)
{
    Console.WriteLine($"Request : {data}");
    bool result = false;
    foreach (var item in data.Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
        result |= await HandleSingleRequest(socket, item);
    }
    return result;
}

async Task<bool> HandleSingleRequest(TcpClient socket, string data)
{
    try
    {
        var request = JsonSerializer.Deserialize<Request>(data);
        if (request?.Method != "isPrime" || !request.Number.HasValue)
        {
            await Stop(socket);
            return true;
        }
        var response = HandlePrimeRequest(request.Number.Value);
        await socket.SendAsJson(response);
        return false;
    }
    catch (JsonException)
    {
        await Stop(socket);
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        await Stop(socket);
        return true;
    }
}

Response HandlePrimeRequest(double number)
{
    return new Response
    {
        Method = "isPrime",
        Prime = IsPrime(number),
    };
}

static bool IsPrime(double number)
{
    if((int)number != number)
    {
        return false;
    }

    if (number <= 1) return false;
    if (number == 2) return true;
    if (number % 2 == 0) return false;

    var boundary = (int)Math.Floor(Math.Sqrt(number));

    for (int i = 3; i <= boundary; i += 2)
        if (number % i == 0)
            return false;

    return true;
}

static async Task Stop(TcpClient socket)
{
    await socket.SendAsJson(new Response());
}

public class Request
{
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("number")]
    public double? Number { get; set; }
}

public class Response
{
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("prime")]
    public bool Prime { get; set; }
}
