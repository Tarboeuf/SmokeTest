using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Starting echo server...");

int port = 10001;
TcpListener listener = new TcpListener(IPAddress.Loopback, port);
listener.Start();

List<Task> tasks = new List<Task>();
tasks.Add(Task.Factory.StartNew(() => HandleClient(listener, tasks, true)));

await Task.WhenAll(tasks);

static void HandleClient(TcpListener listener, List<Task> tasks, bool isFirst)
{
    TcpClient client = listener.AcceptTcpClient();
    tasks.Add(Task.Factory.StartNew(() => HandleClient(listener, tasks, false)));
    NetworkStream stream = client.GetStream();
    StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
    StreamReader reader = new StreamReader(stream, Encoding.ASCII);

    bool isClosed = false;
    while (!isClosed)
    {
        string? inputLine = "";
        while (inputLine != null && !isClosed)
        {
            inputLine = reader.ReadLine();
            writer.WriteLine(inputLine);
            Console.WriteLine("Echoing string: " + inputLine);
            if (inputLine == "quit")
            {
                client.Close();
                isClosed = true;
            }
        }
        Console.WriteLine("Server saw disconnect from client.");
    }
    if(isFirst)
    {
        tasks.Add(Task.Factory.StartNew(() => HandleClient(listener, tasks, false)));
    }
}