using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Common
{
    public static class TcpServer
    {
        public static Socket New()
        {
            int port = 10001;

            var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen();
            Console.WriteLine($"Listening on {socket.LocalEndPoint}");
            return socket;
        }

        public static async Task HandleRaw(this Socket socket, Func<Socket, byte[], int, Task<bool>> func)
        {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 15; i++)
            {
                tasks.Add(InternalHandleRaw(socket, func, tasks));
            }

            await Task.WhenAll(tasks);
        }


        public static async Task HandleFixedSize<T>(this Socket socket, int size, Func<Socket, byte[], T, Task<bool>> func)
            where T : new()
        {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 15; i++)
            {
                tasks.Add(InternalFixedSizeHandleRaw<T>(socket, size, func, tasks));
            }

            await Task.WhenAll(tasks);
        }

        private static async Task InternalFixedSizeHandleRaw<T>(Socket socket, int size, Func<Socket, byte[], T, Task<bool>> func, List<Task> tasks)
            where T : new()
        {
            while (true)
            {
                var connection = await socket.AcceptAsync();
                Console.WriteLine($"Connection accepted from {connection.RemoteEndPoint}");

                T context = new();
                bool shouldClose = false;
                while (!shouldClose && connection.Connected)
                {
                    byte[] buffer = new byte[size];
                    for (int i = 0; i < size; i++)
                    {
                        byte[] tmp = new byte[1];
                        await connection.ReceiveAsync(tmp, SocketFlags.None);
                        buffer[i] = tmp[0];
                    }

                    shouldClose = await func(connection, buffer, context);

                    if (shouldClose)
                    {
                        Console.WriteLine($"Connection closed to {connection.RemoteEndPoint}");
                        tasks.Add(InternalFixedSizeHandleRaw<T>(socket, size, func, tasks));
                        connection.Close();
                    }
                }
            }
        }

        private static async Task InternalHandleRaw(Socket socket, Func<Socket, byte[], int, Task<bool>> func, List<Task> tasks)
        {
            while (true)
            {
                bool shouldClose = false;
                var connection = await socket.AcceptAsync();

                Console.WriteLine($"Connection accepted from {connection.RemoteEndPoint}");
                byte[] buffer = new byte[1024 * 1024];
                int received;
                do
                {
                    try
                    {
                        if (!connection.Connected)
                        {
                            Console.WriteLine($"Connection closed");
                            tasks.Add(InternalHandleRaw(socket, func, tasks));
                            return;
                        }
                        received = await connection.ReceiveAsync(buffer, SocketFlags.None);
                        if (received > 0)
                        {
                            shouldClose = await func(connection, buffer, received);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        return;
                    }
                } while (received > 0);

                if (shouldClose)
                {
                    Console.WriteLine($"Connection closed to {connection.RemoteEndPoint}");
                    connection.Close();
                }
            }
        }

        public static Task HandleString(this Socket socket, Func<Socket, string, Task<bool>> func)
        {
            return socket.HandleRaw((socket, buffer, received) => func(socket, Encoding.UTF8.GetString(buffer, 0, received)));
        }

        public static async Task SendAsJson(this Socket socket, object response)
        {
            var value = JsonSerializer.Serialize(response) + "\n";
            Console.WriteLine($"Response : {value}");
            var data = Encoding.UTF8.GetBytes(value);

            await socket.SendAsync(data, SocketFlags.None);
        }

        public static async Task SendAsString(this Socket socket, string response)
        {
            var value = response + "\n";
            Console.WriteLine($"Response : {value}");
            var data = Encoding.UTF8.GetBytes(value);

            await socket.SendAsync(data, SocketFlags.None);
        }
    }
}