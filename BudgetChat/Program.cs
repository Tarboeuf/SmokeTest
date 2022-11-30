// See https://aka.ms/new-console-template for more information
using Common;
using System.Net.Sockets;

List<User> users = new List<User>();
var socket = TcpServer.New();

await socket.HandleString((socket, message) => Handle(socket, message, users), 
    async socket => await Init(socket, users),
    async socket =>
    {
        var user = GetUser(users, socket);
        users.Remove(user);
        await users.Select(u => u.Socket).SendAsString($"* {user.Name} has left the room");
    });

Task Init(Socket socket, List<User> users)
{
    users.Add(new User(socket));
    return socket.SendAsString("Welcome to budgetchat! What shall I call you?");
}

async Task<bool> Handle(Socket socket, string message, List<User> users)
{
    message = message.Trim('\n');
    Console.WriteLine($"Incoming : {message}");
    var user = GetUser(users, socket);
    if(string.IsNullOrEmpty(user.Name))
    {
        if(await HandleUserName(socket, message, users, user))
        {
            return true;
        }
        return false;
    }
    string completeMessage = $"[{user.Name}] {message}";
    Console.WriteLine(completeMessage);
    await users
        .Where(u => !string.IsNullOrEmpty(u.Name) && u.Name != user.Name)
        .Select(u => u.Socket)
        .SendAsString(completeMessage);
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

    var otherUsers = users.Where(u => u.Socket != socket);
    await socket.SendAsString($"* The room contains: {string.Join(", ", otherUsers.Select(u => u.Name))}");
    await otherUsers.Select(u => u.Socket).SendAsString($"* {user.Name} has entered the room");
    return false;
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