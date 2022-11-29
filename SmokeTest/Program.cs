using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

internal class Program
{
    static int log = 14;

    private static async Task Main(string[] args)
    {
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
            Write(clientNumber, "Started");
            TcpClient client = listener.AcceptTcpClient();

            Write(clientNumber, "Connected");

            NetworkStream stream = client.GetStream();

            //string phrase = "";

            byte[] b = new byte[1024*1024*128];
            UTF8Encoding temp = new UTF8Encoding(true);
            Span<byte> buffer = new Span<byte>(b);
            var length = stream.Read(b, 0, b.Length);
            //while (stream.Read(b, 0, b.Length) > 0)
            //{
            //    Write(13, $"_{Encoding.UTF8.GetString(b)}_");
            //    //phrase += Encoding.UTF8.GetString(b).Trim('\0');
            //}

            //var bytes = Encoding.UTF8.GetBytes(phrase);
            Write(log++, $"l = {length} lastChar = {b[length - 1]}");
            stream.Write(b, 0, length);
            client.Close();
            Write(clientNumber, $"Disconnected          ");


            HandleClient(listener, clientNumber);
        }

        static void Write(int clientNumber, string message)
        {
            Console.CursorLeft = 0;
            Console.CursorTop = clientNumber;
            Console.Write($"{clientNumber} : {message}                                                   ");

        }
    }
}