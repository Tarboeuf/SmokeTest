using Common;
using System.Net.Sockets;

Console.WriteLine("Starting echo server...");

await CommonServer.NewTcp()
    .HandleRaw(async (socket, buffer, received) =>
    {
        await socket.Client.SendAsync(new ArraySegment<byte>(buffer, 0, received), SocketFlags.None);
        return true;
    });
