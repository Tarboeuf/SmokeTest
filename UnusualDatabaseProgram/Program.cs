// See https://aka.ms/new-console-template for more information


using Common;
using System.Net.Sockets;
using System.Reflection.Metadata;

internal class Program
{
    static Dictionary<string, string> MyDatabase = new();

    private static async Task Main(string[] args)
    {
        Console.Clear();
        Console.WriteLine("Starting Unusual Database Program");
        MyDatabase.Add("version", "Tarboeuf's db");
        await CommonServer.NewUdp().HandleString(Handle);


        async Task<bool> Handle(UdpListener server, Received received)
        {
            string data = received.Message;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"--> {data} ({received.Sender})");
            if (data.Contains('='))
            {
                int indexOfEqual = data.IndexOf("=");
                string key = data.Substring(0, indexOfEqual);
                if (key == "version")
                {
                    return true;
                }
                string value = data.Substring(indexOfEqual + 1);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"store value {value}");
                if (MyDatabase.ContainsKey(key))
                {
                    MyDatabase[key] = value;
                }
                else
                {
                    MyDatabase.Add(key, value);
                }
            }
            else
            {
                if (MyDatabase.ContainsKey(data))
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"<-- {data}={MyDatabase[data]} ({received.Sender})");
                    await server.Reply($"{data}={MyDatabase[data]}", received.Sender);
                }
            }
            return false;
        }
    }
}