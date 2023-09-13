// See https://aka.ms/new-console-template for more information

using System.Drawing;
using Common;
using Colorful;
using Console = Colorful.Console;
using System.Net;
using System.Net.Sockets;

Console.WriteAscii("Line Reversal");

await CommonServer.NewUdp()
    .HandleString(HandleString);

async Task<bool> HandleString(UdpListener listener, Received data)
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

    switch (parts[1])
    {
        case "connect":
            await listener.Reply2($"/ack/{parts[2]}/0/\r\n", data.Sender);
            break;
    }
    return false;
}


void SendAcknowledgment(int packetNumber, IPEndPoint senderEndPoint)
{
    byte[] acknowledgmentData = BitConverter.GetBytes(packetNumber);
    using var udpClient = new UdpClient();
    udpClient.Send(acknowledgmentData, acknowledgmentData.Length, senderEndPoint);
}