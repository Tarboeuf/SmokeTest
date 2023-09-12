using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http;

namespace Common
{
    public static class CommonServer
    {
        public static TcpListener NewTcp()
        {
            int port = 10001;
            var listener = TcpListener.Create(port);
            listener.Start();
            //var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            //socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.ReuseAddress, true);
            //socket.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            //socket.Listen();
            Console.WriteLine($"Listening on {listener.LocalEndpoint}");
            return listener;
        }

        public static UdpListener NewUdp()
        {
            return new UdpListener();
        }

        public static async Task HandleString(this UdpListener server, 
            Func<UdpListener, Received, Task<bool>> func)
        {
            while (true)
            {
                var received = await server.Receive();
                if(received == null)
                {
                    continue;
                }
                await func(server, received.Value);
            }
        }


        public static async Task HandleRaw(this TcpListener socket, Func<TcpClient, byte[], int, Task<bool>> func, Action<TcpClient>? initilisation = null,
            Action<TcpClient>? finalisation = null)
        {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 150; i++)
            {
                tasks.Add(InternalHandleRaw(socket, func, tasks, initilisation, finalisation));
            }

            await Task.WhenAll(tasks);
        }

        public static async Task HandleFixedSize<T>(this TcpListener socket, int size, Func<Socket, byte[], T, Task<bool>> func)
            where T : new()
        {
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 15; i++)
            {
                tasks.Add(InternalFixedSizeHandleRaw<T>(socket, size, func, tasks));
            }

            await Task.WhenAll(tasks);
        }

        private static async Task InternalFixedSizeHandleRaw<T>(TcpListener socket, int size, Func<Socket, byte[], T, Task<bool>> func, List<Task> tasks)
            where T : new()
        {
            while (true)
            {
                var connection = await socket.AcceptSocketAsync();
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

        private static async Task InternalHandleRaw(TcpListener socket,
            Func<TcpClient, byte[], int, Task<bool>> func, 
            List<Task> tasks, 
            Action<TcpClient>? initialisation,
            Action<TcpClient>? finalisation)
        {
            while (true)
            {
                bool shouldClose = false;
                var connection = await socket.AcceptTcpClientAsync();
                initialisation?.Invoke(connection);

                Console.WriteLine($"Connection accepted from {connection.Client.RemoteEndPoint}");
                connection.HeartBeat(finalisation);
                byte[] buffer = new byte[1024 * 1024];
                int received;
                do
                {
                    try
                    {
                        if (!connection.Connected)
                        {
                            Console.WriteLine($"Connection closed");
                            tasks.Add(InternalHandleRaw(socket, func, tasks, initialisation, finalisation));
                            finalisation?.Invoke(connection);
                            return;
                        }
                        received = await connection.Client.ReceiveAsync(buffer, SocketFlags.None);
                        if (received > 0)
                        {
                            shouldClose = await func(connection, buffer, received);
                            if(shouldClose)
                            {
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        shouldClose = true;
                        break; ;
                    }
                } while (received > 0);

                if (shouldClose)
                {
                    Console.WriteLine($"Connection closed to {connection.Client.RemoteEndPoint}");
                    finalisation?.Invoke(connection);
                    connection.Close();
                }
            }
        }

        private static void HeartBeat(this TcpClient socket,
            Action<TcpClient>? finalisation)
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if(!socket.IsConnected())
                    {
                        finalisation?.Invoke(socket);
                        return;
                    }
                    Thread.Sleep(100);
                }
            });
        }

        public static Task HandleString(this TcpListener socket,
            Func<TcpClient, string, Task<bool>> func,
            Action<TcpClient>? initilisation = null,
            Action<TcpClient>? finalisation = null)
        {
            return socket.HandleRaw((socket, buffer, received) => func(socket, Encoding.ASCII.GetString(buffer, 0, received)), initilisation, finalisation);
        }

        public static async Task SendAsJson(this TcpClient socket, object response)
        {
            var value = JsonSerializer.Serialize(response) + Environment.NewLine;
            Console.WriteLine($"Response : {value}");
            var data = Encoding.ASCII.GetBytes(value);

            await socket.Client.SendAsync(data, SocketFlags.None);
        }

        public static async Task SendAsString(this TcpClient socket, string response)
        {
            var value = response + "\n";
            var data = Encoding.ASCII.GetBytes(value);

            await socket.Client.SendAsync(data, SocketFlags.None);
        }

        public static void Log(this TcpClient tcpClient, string message)
        {
            //Console.WriteLine($"{tcpClient.Client.RemoteEndPoint} : {message}");
        }

        public static async Task SendAsString(this IEnumerable<TcpClient> sockets, string response)
        {
            var value = response + "\n";
            var data = Encoding.ASCII.GetBytes(value);
            var fullData = new byte[Math.Max(0, data.Length)];
            data.CopyTo(fullData, 0);

            //Console.ForegroundColor = ConsoleColor.Green;
            //Console.WriteLine($"'{response}'");
            //Console.ResetColor();
            foreach (var socket in sockets)
            {
                if(socket.Connected)
                {
                    await socket.Client.SendAsync(new ArraySegment<byte>(fullData), SocketFlags.None);
                }
            }
        }

        public static bool IsConnected(this TcpClient socket)
        {
            try
            {
                return !(socket.Client.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (Exception) { return false; }
        }

        public static long ReadLong(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8).Reverse().ToArray();
            return BitConverter.ToInt64(bytes, 0);
        }
        public static ushort ReadUShort(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2).Reverse().ToArray();
            return BitConverter.ToUInt16(bytes, 0);
        }
        public static int ReadInt(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4).Reverse().ToArray();
            return BitConverter.ToInt32(bytes, 0);
        }
        public static uint ReadUInt(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4).Reverse().ToArray();
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static IEnumerable<DateTime> EachDaysTo(this DateTime start, DateTime end)
        {
            int diffDays = (end.Date - start.Date).Days;
            yield return start.Date;
            for (int i = 0; i < diffDays; i++)
            {
                yield return start.Date.AddDays(i + 1);
            }
        }
    }

    //Server
    public class UdpListener : UdpBase
    {
        public UdpListener()
        {
            Client = new UdpClient(10001);
        }

        public Task Reply(string message, IPEndPoint endpoint)
        {
            var datagram = Encoding.ASCII.GetBytes(message);
            return Client.SendAsync(datagram, datagram.Length, endpoint);
        }

    }

    public abstract class UdpBase
    {
        protected UdpClient Client;

        protected UdpBase()
        {
            Client = new UdpClient();
        }

        public async Task<Received?> Receive()
        {
            try
            {
                var result = await Client.ReceiveAsync();
                return new Received()
                {
                    Message = Encoding.ASCII.GetString(result.Buffer, 0, result.Buffer.Length),
                    Sender = result.RemoteEndPoint,
                };
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(ex.ToString());
                return null;
            }
            finally 
            {
                //Client.Close();
            }
        }
    }
    public struct Received
    {
        public IPEndPoint Sender;
        public string Message;
    }
}