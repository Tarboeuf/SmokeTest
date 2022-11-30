// See https://aka.ms/new-console-template for more information
using Common;
using System.Net.Sockets;

List<User> users = new List<User>();
var socket = TcpServer.New();

var task = socket.HandleString((socket, message) => Handle(socket, message, users), 
    async socket => await Init(socket, users),
    async socket =>
    {
        var user = GetUser(users, socket);
        await RemoveUser(user, users);
    });
var stayAlive = Task.Factory.StartNew(async () =>
{
    while(true)
    {
        foreach (var user in users.ToList())
        {
            if (!user.Socket.IsConnected())
            {
                await RemoveUser(user, users);
            }
        }
    }
});
await task;

Task RemoveUser(User user, List<User> users)
{
    users.Remove(user);
    return Broadcast($"* {user.Name} has left the room", users.ToArray());
}

Task Init(Socket socket, List<User> users)
{
    users.Add(new User(socket));
    return socket.SendAsString("--> Welcome to budgetchat! What shall I call you?");
}

async Task<bool> Handle(Socket socket, string message, List<User> users)
{
    message = message.Trim('\n');
    var user = GetUser(users, socket);
    Console.WriteLine($"<-- : {message} ({user.Name})");
    if (string.IsNullOrEmpty(user.Name))
    {
        if(await HandleUserName(socket, message, users, user))
        {
            return true;
        }
        return false;
    }
    string completeMessage = $"[{user.Name}] {message}";
    Console.WriteLine(completeMessage);
    await Broadcast(completeMessage, users.Where(u => !string.IsNullOrEmpty(u.Name) && u.Name != user.Name).ToArray());
    return true;
}

User GetUser(List<User> users, Socket socket)
{
    return users.First(u => u.Socket == socket);
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
    await Broadcast($"* The room contains: {string.Join(", ", otherUsers.Select(u => u.Name))}", user);
    await Broadcast($"* {user.Name} has entered the room", otherUsers);
    return false;
}

static async Task Broadcast(string message, params User[] users)
{
    Console.WriteLine($"--> {message} ({string.Join(", ", users.Select(u => u.Name))})");
    await users.Select(u => u.Socket).SendAsString(message);
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