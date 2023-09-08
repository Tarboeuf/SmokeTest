// See https://aka.ms/new-console-template for more information
using System.Net.Sockets;
using System.Net;
using System.Text;

Console.WriteLine("Hello, TCP World!");
var server = new TcpClient("chat.protohackers.com", 16963);
//var server = new TcpClient("2a02:8429:6051:cf01:e03:eee7:6b5c:cdb5", 10001);
using var sr = new StreamReader(server.GetStream());
using var sw = new StreamWriter(server.GetStream());

Task.Factory.StartNew(() =>
{
    while (true)
    {
        var line = sr.ReadLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(line);
        Console.ResetColor();
    }
});


while (true)
{
    var line = Console.ReadLine()!;
    sw.WriteLine(line);
    sw.Flush();
}