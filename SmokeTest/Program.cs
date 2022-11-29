using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Starting echo server...");

int port = 10001;
TcpListener listener = new TcpListener(IPAddress.Any, port);
listener.Start();

List<Task> tasks = new List<Task>();
Console.Clear();
for (int i = 0; i < 10; i++)
{
    tasks.Add(GetPool(listener, i));
}


await Task.WhenAll(tasks);


static Task GetPool(TcpListener listener, int number)
{
    Console.CursorLeft = 0;
    Console.CursorTop = number;
    Console.Write($"{number} creation          ");
    return Task.Factory.StartNew(() => HandleClient(listener, number));
}

static void HandleClient(TcpListener listener, int clientNumber)
{
    Console.CursorLeft = 0;
    Console.CursorTop = clientNumber;
    Console.Write($"{clientNumber} : Started        ");
    TcpClient client = listener.AcceptTcpClient();

    Console.CursorLeft = 0;
    Console.CursorTop = clientNumber;
    Console.Write($"{clientNumber} : Connected           ");

    NetworkStream stream = client.GetStream();

    string phrase = "";

    byte[] b = new byte[1024];
    UTF8Encoding temp = new UTF8Encoding(true);
    while (stream.Read(b, 0, b.Length) > 0)
    {
        Console.CursorTop = 13;
        Console.CursorLeft = 0;
        Console.WriteLine(temp.GetString(b));
        phrase += Encoding.UTF8.GetString(b);
    }

    var bytes = Encoding.UTF8.GetBytes(phrase);
    stream.Write(bytes, 0, bytes.Length);
    client.Close();
    Console.CursorLeft = 0;
    Console.CursorTop = clientNumber;
    Console.Write($"{clientNumber} : Disconnected           {phrase}");
    HandleClient(listener, clientNumber);
}