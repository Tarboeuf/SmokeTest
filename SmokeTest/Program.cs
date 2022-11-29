using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Starting echo server...");

int port = 10001;
TcpListener listener = new TcpListener(IPAddress.Any, port);
listener.Start();

List<Task> tasks = new List<Task>();

for (int i = 0; i < 5; i++)
{
    Console.WriteLine("line");
}
for (int i = 0; i < 5; i++)
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

    bool isClosed = false;
    while (!isClosed)
    {
        do
        {
            if(!client.Connected)
            {
                isClosed = true;
                continue;
            }
            try
            {
                byte input = (byte)stream.ReadByte();
                if (!client.Connected)
                {
                    isClosed = true;
                    continue;
                }
                stream.WriteByte(input);
                Console.CursorTop = clientNumber;
                Console.Write(Encoding.UTF8.GetString(new byte[] { input }));
                if (input < 0)
                {
                    client.Close();
                    isClosed = true;
                }
            }
            catch (Exception) { }
        }
        while (!isClosed);
        Console.CursorLeft = 0;
        Console.CursorTop = clientNumber;
        Console.Write($"{clientNumber} : Disconnected           ");
    }
    HandleClient(listener, clientNumber);
}