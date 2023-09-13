// See https://aka.ms/new-console-template for more information
using System.Net.Sockets;
using System.Net;
using System.Text;

Console.WriteLine("Hello, World!");
var server = new UdpClient(AddressFamily.InterNetworkV6);
IPEndPoint ep = new IPEndPoint(IPAddress.Parse("2a02:8429:6051:cf01:e03:eee7:6b5c:cdb5"), 10001); // endpoint where server is listening
server.Connect(ep);

// send data
string message = "/connect/646510383/";
var bytes = Encoding.ASCII.GetBytes(message);
server.Send(bytes, bytes.Length);

//// then receive data
//var receivedData = server.Receive(ref ep);

//Console.WriteLine($"receive data ({Encoding.ASCII.GetString(receivedData)}) from " + ep.ToString());


Task.Factory.StartNew(() =>
{
    while (true)
    {
        var receivedData = server.Receive(ref ep);

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