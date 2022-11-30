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

        var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
        socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
        socket.Listen();
        Console.WriteLine($"Listening on {socket.LocalEndPoint}");

        while (true)
        {
            var connection = await socket.AcceptAsync();

            Console.WriteLine($"Connection accepted from {connection.RemoteEndPoint}");
            byte[] buffer = new byte[1024 * 1024];
            int received;
            do
            {
                received = await connection.ReceiveAsync(buffer, SocketFlags.None);
                if (received > 0)
                {
                    await connection.SendAsync(new ArraySegment<byte>(buffer, 0, received), SocketFlags.None);
                }
            } while (received > 0);

            Console.WriteLine($"Connection closed to {connection.RemoteEndPoint}");
            connection.Close();
        }
    }
}