// See https://aka.ms/new-console-template for more information
using BudgetChat;
using Common;
using System.Net.Sockets;

internal class Program
{
    static object _lockObject = new object();

    private static async Task Main(string[] args)
    {
        var otherTask = new GitHubChat().Start();

        List<User> users = new List<User>();
        var socket = TcpServer.New();

        var task = socket.HandleString((socket, message) => Handle(socket, message, users),
            async socket => await Init(socket, users),
            async socket =>
            {
                var user = GetUser(users, socket);
                await RemoveUser(user, users);
            });
        var keyHandler = KeyHandler(users);
        await task;
    }

    static Task KeyHandler(List<User> users)
    {
        return Task.Factory.StartNew(() =>
        {
            while (true)
            {
                var key = Console.ReadKey();
                if(key.Key == ConsoleKey.Escape)
                {
                    lock(_lockObject)
                    {
                        foreach (var user in users)
                        {
                            Console.WriteLine($"* Close > '{user.Name}'");
                            user.Socket.Close();
                        }
                        users.Clear();
                    }

                    Console.WriteLine("Users cleared");
                }
            }
        });
    }

    static Task RemoveUser(User user, List<User> users)
    {
        lock (_lockObject)
        {
            users.Remove(user);
        }
        return Broadcast($"* {user.Name} has left the room", users.ToArray());
    }

    static Task Init(Socket socket, List<User> users)
    {
        users.Add(new User(socket));
        return socket.SendAsString("--> Welcome to budgetchat! What shall I call you?");
    }

    static async Task<bool> Handle(Socket socket, string message, List<User> users)
    {
        message = message.Trim('\n');
        var user = GetUser(users, socket);
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"<-- {message} ({user.Name}) {DateTime.Now.ToString("O")}");
        Console.ResetColor();
        if (string.IsNullOrEmpty(user.Name))
        {
            if (await HandleUserName(socket, message, users, user))
            {
                return true;
            }
            return false;
        }
        if(string.IsNullOrWhiteSpace(message))
        {
            return true;
        }
        string completeMessage = $"[{user.Name}] {message}";
        User[] otherUsers = users.Where(u => !string.IsNullOrEmpty(u.Name) && u.Name != user.Name).ToArray();
        if(otherUsers.Length > 0)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                /* run your code here */
                Thread.Sleep(500);
                var tast = Broadcast(completeMessage, otherUsers);
            }).Start();
        }
        return true;
    }

    static User GetUser(List<User> users, Socket socket)
    {
        lock (_lockObject)
        {
            return users.First(u => u.Socket == socket);
        }
    }

    static async Task<bool> HandleUserName(Socket socket, string message, List<User> users, User user)
    {
        if (message.Length < 1 || message.Length > 16 || users.Any(u => u.Name == message))
        {
            await socket.SendAsString("User name is illegal, care to the prison");
            return true;
        }
        user.Name = message;

        var otherUsers = users.Where(u => u.Socket != socket && !string.IsNullOrEmpty(u.Name)).ToArray();
        await Broadcast($"* {user.Name} has entered the room", otherUsers);
        await Broadcast($"* The room contains: {string.Join(", ", otherUsers.Select(u => u.Name))}", user);
        return false;
    }

    static async Task Broadcast(string message, params User[] users)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"--> {message} ({string.Join(", ", users.Where(u => !string.IsNullOrEmpty(u.Name)).Select(u => u.Name))})");
        Console.ResetColor();
        await users.Where(u => !string.IsNullOrEmpty(u.Name)).Select(u => u.Socket).Where(s => s.IsConnected()).SendAsString(message);
    }
}

public class User
{
    public Socket Socket { get; init; }

    public User(Socket socket)
    {
        Socket = socket;
    }

    public string? Name { get; set; }
}