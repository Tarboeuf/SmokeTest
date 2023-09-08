// See https://aka.ms/new-console-template for more information

using Common;
using System.Net.Sockets;
using System.Reactive.Linq;

internal class Program
{
    private static List<UserInTheMiddle> users = new();

    private static async Task Main(string[] args)
    {
        Console.WriteLine("Mob In The Middle");
        int port = 10001;
        var listener = TcpListener.Create(port);
        listener.Start();
        Console.WriteLine($"Listening on port {port}");


        while (true)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleConnection(client));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    private static void HandleConnection(TcpClient client)
    {
        TcpClient protohacker = new TcpClient("chat.protohackers.com", 16963);
        users.Add(new UserInTheMiddle(client, protohacker, Finalize));
    }

    private static void Finalize(UserInTheMiddle obj)
    {
        try
        {
            if (users.Contains(obj)) users.Remove(obj);
            obj.Dispose();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("kicked");
        }
        catch (Exception)
        {
        }
    }

    public class UserInTheMiddle : IDisposable
    {
        const string TonyAddress = "7YWHMfk9JZe0LM0g1ZauHuiSxhI";

        public UserInTheMiddle(TcpClient client, TcpClient protoHackerChat, Action<UserInTheMiddle> finalize)
        {
            Client = client;
            ProtoHackerChat = protoHackerChat;
            this._finalize = finalize;
            _inputStreamWriter = new StreamWriter(client.GetStream());
            
            _outputStreamWriter = new StreamWriter(protoHackerChat.GetStream());

            _inputStreamReader = InitializeDataStreamCopy(client.GetStream(), _outputStreamWriter, true);
            _outputStreamReader = InitializeDataStreamCopy(protoHackerChat.GetStream(), _inputStreamWriter, false);
        }

        public IDisposable InitializeDataStreamCopy(NetworkStream reader, StreamWriter writer, bool isInputToOutput)
        {
            return ObservableStreamReader.Create(reader, isInputToOutput ? "i" : "o")
                .Where(s => Client.Connected && ProtoHackerChat.Connected)
                .Select(ReplaceAddress)
                .Subscribe(line =>
                    {
                        writer.WriteLine(line);
                        writer.Flush();

                        Console.ForegroundColor = isInputToOutput ? ConsoleColor.Green : ConsoleColor.Blue;
                        Console.WriteLine(line);
                    },
                    e => _finalize(this),
                    () => _finalize(this));
            //Task.Factory.StartNew(() =>
            //{
            //    try
            //    {
            //        while (Client.Connected && ProtoHackerChat.Connected)
            //        {
            //            var line = ReadLine(reader);
            //            if (line == null)
            //            {
            //                break;
            //            }
            //            line = ReplaceAddress(line);
            //            writer.WriteLine(line);
            //            writer.Flush();

            //            Console.ForegroundColor = isInputToOutput ? ConsoleColor.Green : ConsoleColor.Blue;
            //            Console.WriteLine(line);
            //        }
            //    }
            //    catch
            //    {
            //        // ignored
            //    }
            //    finally
            //    {
            //        _finalize(this);
            //    }
            //});
        }

        private string ReplaceAddress(string line)
        {
            var values = line.Split(' ');
            return string.Join(' ', values.Select(v => IsAddress(v) ? TonyAddress : v));
        }

        private bool IsAddress(string v)
        {
            if(!v.StartsWith('7')) return false;
            if (v.Length < 26 || v.Length > 35) return false;
            if (!v.All(char.IsLetterOrDigit)) return false;
            return true;
        }

        public string? Name { get; set; }
        public TcpClient Client { get; }
        public TcpClient ProtoHackerChat { get; }
        private readonly IDisposable _inputStreamReader;
        private readonly IDisposable _outputStreamReader;
        private readonly StreamWriter _inputStreamWriter;
        private readonly StreamWriter _outputStreamWriter;
        private readonly Action<UserInTheMiddle> _finalize;

        public void Dispose()
        {
            _inputStreamReader.Dispose();
            _outputStreamReader.Dispose();
            _inputStreamWriter.Dispose();
            _outputStreamWriter.Dispose();
        }
    }
}