using Common;
using System.Net.Sockets;

Console.WriteLine("Starting echo server...");

await TcpServer.New()
    .HandleRaw(async (socket, buffer, received) =>
    {
        await socket.SendAsync(new ArraySegment<byte>(buffer, 0, received), SocketFlags.None);
        return true;
    });
