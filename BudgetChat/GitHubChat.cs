using System.Net.Sockets;

namespace BudgetChat
{

    public class GitHubChat
    {
        Dictionary<string, StreamWriter> clients = new Dictionary<string, StreamWriter>();
        const int port = 10001;

        public async Task Start() 
        {

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

        async Task HandleConnection(TcpClient client)
        {
            Console.WriteLine("Client connected");
            string name;

            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream);
                using StreamWriter writer = new(stream);

                try
                {
                    await SendToClient("Welcome to budgetchat! What shall I call you?", writer);
                    var line = await reader.ReadLineAsync();
                    var proposedName = line?.Trim();
                    if (string.IsNullOrEmpty(proposedName) || !IsNameValid(proposedName))
                        return;

                    var clientNames = GetClientNames();
                    await SendToClient($"* The room contains: {clientNames}", writer);

                    name = proposedName;
                    clients?.Add(name, writer);
                    await SendToAllClients($"* {name} has entered the room", name);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }

                while (client.Connected)
                {
                    try
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            await DisconnectClient(name);
                            break;
                        }

                        await SendToAllClients($"[{name}] {line}", name);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        await DisconnectClient(name);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                client.Close();
            }

            Console.WriteLine("Client disconnected");
        }

        async Task SendToClient(string message, StreamWriter writer)
        {
            await writer.WriteLineAsync(message);
            await writer.FlushAsync();
        }

        async Task SendToAllClients(string message, string except)
        {
            if (clients?.Any() != true)
                return;

            var recipients = clients.Where(client => client.Key != except);
            var writers = recipients.Select(client => client.Value);
            foreach (var writer in writers)
                await SendToClient(message, writer);

            Console.WriteLine(message);
        }

        string GetClientNames()
        {
            return clients?.Any() == true ? string.Join(", ", clients.Keys) : string.Empty;
        }

        async Task DisconnectClient(string name)
        {
            if (clients != null && name != null)
            {
                await SendToAllClients($"* {name} has left the room", name);
                clients.Remove(name);
            }
        }

        bool IsNameValid(string name)
        {
            if (clients?.Any() == true && clients.ContainsKey(name))
                return false;

            return name.All(char.IsLetterOrDigit);
        }
    }
}