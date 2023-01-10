// See https://aka.ms/new-console-template for more information
using System.Net.Sockets;
using System.Net;
using System.Text;

Console.WriteLine("Hello, World!");
var server = new UdpClient("185.219.142.220", 10001);
IPEndPoint ep = new IPEndPoint(IPAddress.Any, 5170); // endpoint where server is listening
//client.Connect(ep);

// send data
string message = "version";
var bytes = Encoding.ASCII.GetBytes(message);
server.Send(bytes, bytes.Length);

// then receive data
var receivedData = server.Receive(ref ep);

Console.WriteLine($"receive data ({Encoding.ASCII.GetString(receivedData)}) from " + ep.ToString());


Task.Factory.StartNew(() =>
{
    while (true)
    {
        receivedData = server.Receive(ref ep);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(Encoding.ASCII.GetString(receivedData));
        Console.ResetColor();
    }
});


while (true)
{
    var line = Console.ReadLine()!;
    bytes = Encoding.ASCII.GetBytes(line);
    server.Send(bytes, bytes.Length);
}