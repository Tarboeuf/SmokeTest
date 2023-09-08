// See https://aka.ms/new-console-template for more information

using System.Net.Sockets;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using Common;

Console.WriteLine("Starting Speed Daemon...");
List<(IAmDispatcher, TcpClient)> dispatchers = new List<(IAmDispatcher, TcpClient)>();
Dictionary<string, Dictionary<int, List<(Plate, IAmCamera)>>> plates =
    new Dictionary<string, Dictionary<int, List<(Plate, IAmCamera)>>>();
Dictionary<TcpClient, IAmCamera> cameras = new Dictionary<TcpClient, IAmCamera>();
await CommonServer.NewTcp()
    .HandleRaw((socket, buffer, received) =>
    {
        var span = new Span<byte>(buffer, 1, received - 1);
        span.Reverse();
        switch (buffer[0])
        {
            case 0x20:
                var plate = Deserialize<Plate>(span);
                if (!plates.ContainsKey(plate.PlateNumber))
                {
                    plates.Add(plate.PlateNumber, new Dictionary<int, List<(Plate, IAmCamera)>>());
                }
                var platesByRoad = plates[plate.PlateNumber];
                var road = cameras[socket].Road;
                if (!platesByRoad.ContainsKey(road))
                {
                    platesByRoad.Add(road, new List<(Plate, IAmCamera)>());
                }
                var platesOfRoad = platesByRoad[road];
                platesOfRoad.Add((plate, cameras[socket]));
                break;
            case 0x40:
                var whb = Deserialize<WantHeartbeat>(span);
                Console.WriteLine($"Receive Heartbeat from {socket.Client.RemoteEndPoint} : Interval={whb.Interval}");
                if (whb.Interval == 0)
                    break;
                var thread = new Thread(start: () =>
                {
                    using var stream = socket.GetStream();
                    var delay = TimeSpan.FromSeconds(whb.Interval / 10);
                    while (true)
                    {
                        try
                        {
                            stream.WriteByte(0x41);
                            Console.WriteLine($"Send Heartbeat to {socket.Client.RemoteEndPoint}");
                            Thread.Sleep(delay);
                        }
                        catch (Exception )
                        {
                            //Ignore
                        }
                    }
                });
                thread.Start();
                break;
            case 0x80:
                var camera = Deserialize<IAmCamera>(span);

                cameras.Add(socket, camera);
                break;
            case 0x81:
                var iAmDispatcher = Deserialize<IAmDispatcher>(span);
                dispatchers.Add((iAmDispatcher, socket));
                break;
        }

        return Task.FromResult(false);
    });

void CheckAndSendTickets(List<(Plate, IAmCamera)> plates, Plate newPlate, IAmCamera camera)
{
    foreach (var plate in plates)
    {
        var distance = Math.Abs(plate.Item2.Mile - camera.Mile);
        var speed = distance / Math.Abs((plate.Item1.Timestamp - newPlate.Timestamp).Hours);
        if (speed > camera.SpeedLimit)
        {
            var firstPlate = plate.Item1.Timestamp > newPlate.Timestamp ? (newPlate, camera) : plate;
            var endPlate = plate.Item1.Timestamp < newPlate.Timestamp ? (newPlate, camera) : plate;
            Ticket ticket = new Ticket
            {
                Road = camera.Road,
                PlateNumber = newPlate.PlateNumber,
                Start = firstPlate.Item1.Timestamp,
                End = endPlate.Item1.Timestamp,
                MileEnd = endPlate.Item2.Mile,
                MileStart = firstPlate.Item2.Mile,
                PlateNumberLength = newPlate.PlateNumberLength,
                Speed = speed * 100
            };
            var dispatcher = dispatchers.FirstOrDefault(d => d.Item1.Roads.Contains(camera.Road));
            if (dispatcher != (null, null))
            {
                Send(dispatcher.Item2, ticket);
            }
            else
            {

            }
        }
    }
}

void Send(TcpClient client, Ticket ticket)
{
    var data = Serialize(ticket, 0x21, 18 + ticket.PlateNumberLength);
    client.Client.Send(data);
}

static T Deserialize<T>(Span<byte> binaryData) where T : new()
{
    var result = new T();

    using var stream = new MemoryStream(binaryData.ToArray());
    using var reader = new BinaryReader(stream);

    var fields = typeof(T).GetProperties();

    var lastByte = byte.MinValue;
    foreach (var field in fields)
    {
        if (field.PropertyType == typeof(byte))
        {
            var value = reader.ReadByte();
            field.SetValue(result, value);
            lastByte = value;
        }
        else if (field.PropertyType == typeof(string))
        {
            var chars = reader.ReadBytes(lastByte);
            var value = Encoding.ASCII.GetString(chars);
            field.SetValue(result, value);
        }
        else if (field.PropertyType == typeof(DateTime))
        {
            var ticks = reader.ReadInt64();
            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(ticks);
            var dateTime = dateTimeOffset.UtcDateTime;
            field.SetValue(result, dateTime);
        }
        else if (field.PropertyType == typeof(int))
        {
            int value = reader.ReadInt16();
            field.SetValue(result, value);
        }
        else if (field.PropertyType == typeof(long))
        {
            int value = reader.ReadInt32();
            field.SetValue(result, value);
        }
        else if (field.PropertyType == typeof(int[]))
        {
            int[] values = new int[lastByte];

            for (int i = 0; i < lastByte; i++)
            {
                values[i] = reader.ReadInt32();
            }
            field.SetValue(result, values);
        }
    }

    return result;
}

static byte[] Serialize<T>(T obj, byte code, int size) where T : new()
{
    byte[] data = new byte[size];
    data[0] = code;
    var fields = typeof(T).GetProperties();
    int offset = 1;
    var lastByte = byte.MinValue;
    foreach (var field in fields)
    {
        byte[] addedBytes;
        if (field.PropertyType == typeof(byte))
        {
            var value = (byte)field.GetValue(obj);
            lastByte = value;

            addedBytes = new byte[] { value };
        }
        else if (field.PropertyType == typeof(string))
        {
            var value = (string)field.GetValue(obj);
            addedBytes = Encoding.ASCII.GetBytes(value);
        }
        else if (field.PropertyType == typeof(DateTime))
        {
            var value = (DateTime)field.GetValue(obj);
            long unixTimestamp = (long)(value - new DateTime(1970, 1, 1)).TotalSeconds;

            // Convert the Unix timestamp to an integer (you may need to cast or truncate it)
            int timestampInteger = (int)unixTimestamp;

            // Serialize the integer into a 4-byte array
            addedBytes = BitConverter.GetBytes(timestampInteger);

        }
        else if (field.PropertyType == typeof(int))
        {
            var value = (int)field.GetValue(obj);
            addedBytes = BitConverter.GetBytes(value);
        }
        else if (field.PropertyType == typeof(long))
        {
            var value = (long)field.GetValue(obj);
            addedBytes = BitConverter.GetBytes(value);
        }
        else
        {
            throw new Exception("Type not implemented");
        }

        for (int i = 0; i < addedBytes.Length; i++)
        {
            data[offset + i] = addedBytes[i];
        }

        offset += addedBytes.Length;
    }

    return data;
}



class Error
{
    public byte MessageLength { get; set; }
    public string Message { get; set; }
}

class Plate
{
    public byte PlateNumberLength { get; set; }
    public string PlateNumber { get; set; }
    public DateTime Timestamp { get; set; }
}

class Ticket
{
    public byte PlateNumberLength { get; set; }
    public string PlateNumber { get; set; }
    public int Road { get; set; }
    public int MileStart { get; set; }
    public DateTime Start { get; set; }
    public int MileEnd { get; set; }
    public DateTime End { get; set; }
    public int Speed { get; set; }
}

class WantHeartbeat
{
    public long Interval { get; set; }
}

class Heartbeat
{
}

class IAmCamera
{
    public int Road { get; set; }
    public int Mile { get; set; }
    public int SpeedLimit { get; set; }
}

class IAmDispatcher
{
    public byte NumRoads { get; set; }
    public int[] Roads { get; set; }
}