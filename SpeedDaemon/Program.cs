// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Common;

Console.WriteLine("Starting Speed Daemon...");
var dispatchers = new List<(IAmDispatcher, BinaryWriter)>();
var plates = new ConcurrentDictionary<string, ConcurrentDictionary<int, List<(Plate, IAmCamera)>>>();
var cameras = new ConcurrentDictionary<BinaryWriter, IAmCamera>();
var pendingTickets = new List<Ticket>();

var sentTickets = new ConcurrentDictionary<string, List<DateTime>>();

var port = 10001;
var listener = TcpListener.Create(port);
listener.Start();

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

async Task HandleConnection(TcpClient tcpClient)
{
    await using var networkStream = tcpClient.GetStream();
    using var reader = new BinaryReader(networkStream);
    await using var writer = new BinaryWriter(networkStream);
    var isStopped = false;
    while (!isStopped)
    {
        if (!networkStream.DataAvailable)
        {
            await Task.Delay(200);
            continue;
        }
        var type = reader.ReadByte();
        switch (type)
        {
            case 0x20:
                var plate = Deserialize<Plate>(reader);
                plates.TryAdd(plate.PlateNumber, new ConcurrentDictionary<int, List<(Plate, IAmCamera)>>());
                var platesByRoad = plates[plate.PlateNumber];
                lock (dispatchers)
                {
                    if (dispatchers.Any(d => d.Item2 == writer))
                    {
                        Serialize(new Error {Message = "plate from dispatche mf"}, 0x10, writer);
                        isStopped = true;
                        break;
                    }
                }

                var road = cameras[writer].Road;
                platesByRoad.TryAdd(road, new List<(Plate, IAmCamera)>());
                var platesOfRoad = platesByRoad[road];
                List<(Plate, IAmCamera)> clone;
                lock (platesOfRoad)
                {
                    clone = platesOfRoad.ToList();
                    tcpClient.Log($"New plate added {plate.PlateNumber}");
                    platesOfRoad.Add((plate, cameras[writer]));
                }
                CheckAndSendTickets(clone, plate, cameras[writer], () => isStopped = true);

                break;
            case 0x40:
                var whb = Deserialize<WantHeartbeat>(reader);
                tcpClient.Log($"Receive Heartbeat : Interval={whb.Interval}");
                if (whb.Interval == 0)
                    break;
                var thread = new Thread(start: () =>
                {
                    var delay = TimeSpan.FromSeconds((double)whb.Interval / 10);
                    while (true)
                    {
                        try
                        {
                            lock (writer)
                            {
                                writer.Write((byte)0x41);
                            }
                            Thread.Sleep(delay);
                        }
                        catch (Exception)
                        {
                            isStopped = true;
                            break;
                        }
                    }
                });
                thread.Start();
                break;
            case 0x80:
                try
                {
                    var camera = Deserialize<IAmCamera>(reader);
                    lock (dispatchers)
                    {
                        if (dispatchers.Any(d => d.Item2 == writer))
                        {
                            Serialize(new Error {Message = "camera not dispatcher mf"}, 0x10, writer);
                            isStopped = true;
                            break;
                        }
                    }

                    if (!cameras.TryAdd(writer, camera))
                    {

                        Serialize(new Error { Message = "marcelo" }, 0x10, writer);
                        isStopped = true;
                        break;
                    }
                    tcpClient.Log(camera.ToString());
                }
                catch (Exception e)
                {
                    Serialize(new Error { Message = "marcel" }, 0x10, writer);
                    isStopped = true;
                }
                break;
            case 0x81:
                var iAmDispatcher = Deserialize<IAmDispatcher>(reader);
                if (cameras.ContainsKey(writer))
                {
                    Serialize(new Error { Message = "dispatcher no camera mf" }, 0x10, writer);
                    isStopped = true;
                    break;
                }

                lock (dispatchers)
                {
                    if (dispatchers.Any(d => d.Item2 == writer))
                    {
                        Serialize(new Error {Message = "marcela"}, 0x10, writer);
                        isStopped = true;
                        break;
                    }
                }

                lock (dispatchers)
                {
                    dispatchers.Add((iAmDispatcher, writer));
                }

                tcpClient.Log(iAmDispatcher.ToString());
                foreach (var pendingTicket in pendingTickets.ToList())
                {
                    if (iAmDispatcher.Roads.Contains(pendingTicket.Road))
                    {
                        Send(writer, pendingTicket, () => isStopped = true);
                        pendingTickets.Remove(pendingTicket);
                    }
                }
                break;
            default:
                Console.WriteLine(type);
                Serialize(new Error { Message = "bad " + type }, 0x10, writer);
                isStopped = true;
                break;
        }
    }

    lock (dispatchers)
    {
        dispatchers.RemoveAll(p => p.Item2 == writer);
    }

    cameras.TryRemove(writer, out var _);

    tcpClient.Log("Stopped");
    tcpClient.Dispose();

}


void CheckAndSendTickets(List<(Plate, IAmCamera)> platesByRoad, Plate newPlate, IAmCamera camera, Func<bool> stopOnCrash)
{
    foreach (var plate in platesByRoad)
    {
        var distance = Math.Abs(plate.Item2.Mile - camera.Mile);
        var time = plate.Item1.Timestamp > newPlate.Timestamp
            ? plate.Item1.Timestamp - newPlate.Timestamp
            : newPlate.Timestamp - plate.Item1.Timestamp;
        var speed = distance / time.TotalHours;
        if ((speed) > camera.SpeedLimit + 0.5)
        {
            var firstPlate = plate.Item1.Timestamp > newPlate.Timestamp ? (newPlate, camera) : plate;
            var endPlate = plate.Item1.Timestamp < newPlate.Timestamp ? (newPlate, camera) : plate;
            if (plate.Item2.Road != camera.Road)
            {
                throw new Exception("Why ?");
            }
            var ticket = new Ticket
            {
                Road = camera.Road,
                PlateNumber = newPlate.PlateNumber,
                Start = firstPlate.Item1.Timestamp,
                End = endPlate.Item1.Timestamp,
                MileEnd = endPlate.Item2.Mile,
                MileStart = firstPlate.Item2.Mile,
                Speed = (ushort)(speed * 100)
            };
            (IAmDispatcher, BinaryWriter) dispatcher;
            lock (dispatchers)
            {
                dispatcher = dispatchers.ToArray().FirstOrDefault(d => d.Item1.Roads.Contains(camera.Road));
            }
            if (dispatcher != (null, null))
            {
                Send(dispatcher.Item2, ticket, stopOnCrash);
            }
            else
            {
                pendingTickets.Add(ticket);
            }
        }
    }
}

void Send(BinaryWriter writer, Ticket ticket, Func<bool> stopOnCrash)
{
    try
    {
        lock (sentTickets)
        {
            if (sentTickets.TryGetValue(ticket.PlateNumber, out var sentTicket))
            {
                foreach (var date in ticket.Start.EachDaysTo(ticket.End))
                {
                    if (sentTicket.Contains(date))
                        return;
                }
            }
            else
            {
                sentTickets.TryAdd(ticket.PlateNumber, new List<DateTime>());
            }

            foreach (var date in ticket.Start.EachDaysTo(ticket.End))
            {
                sentTickets[ticket.PlateNumber].Add(date);
            }
        }

        //Console.WriteLine($"Ticket sending ... {ticket}");
        Serialize(ticket, 0x21, writer);
    }
    catch (Exception e)
    {
        stopOnCrash();
    }
}

static T Deserialize<T>(BinaryReader reader) where T : new()
{
    var result = new T();

    var fields = typeof(T).GetProperties();

    foreach (var field in fields)
    {
        if (field.PropertyType == typeof(byte))
        {
            var value = reader.ReadByte();
            field.SetValue(result, value);
        }
        else if (field.PropertyType == typeof(string))
        {
            var size = reader.ReadByte();
            var chars = reader.ReadBytes(size);
            var value = Encoding.ASCII.GetString(chars);
            field.SetValue(result, value);
        }
        else if (field.PropertyType == typeof(DateTime))
        {
            var ticks = reader.ReadUInt();
            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(ticks);
            var dateTime = dateTimeOffset.UtcDateTime;
            field.SetValue(result, dateTime);
        }
        else if (field.PropertyType == typeof(ushort))
        {
            var value = reader.ReadUShort();
            field.SetValue(result, value);
        }
        else if (field.PropertyType == typeof(int))
        {
            var value = reader.ReadInt();
            field.SetValue(result, value);
        }
        else if (field.PropertyType == typeof(long))
        {
            var value = reader.ReadLong();
            field.SetValue(result, value);
        }
        else if (field.PropertyType == typeof(ushort[]))
        {
            var size = reader.ReadByte();
            var values = new ushort[size];

            for (var i = 0; i < size; i++)
            {
                values[i] = reader.ReadUShort();
            }
            field.SetValue(result, values);
        }
    }

    return result;
}

static void Serialize<T>(T obj, byte code, BinaryWriter writer) where T : new()
{
    lock (writer)
    {
        writer.Write(code);
        var fields = typeof(T).GetProperties();
        var offset = 1;
        var lastByte = byte.MinValue;
        foreach (var field in fields)
        {
            if (field.PropertyType == typeof(byte))
            {
                var value = (byte)field.GetValue(obj);
                writer.Write(value);
            }
            else if (field.PropertyType == typeof(string))
            {
                var value = (string)field.GetValue(obj);
                writer.Write((byte)value.Length);
                writer.Write(Encoding.ASCII.GetBytes(value));
            }
            else if (field.PropertyType == typeof(DateTime))
            {
                var value = (DateTime)field.GetValue(obj);
                var unixTimestamp = (long)(value - new DateTime(1970, 1, 1)).TotalSeconds;

                // Convert the Unix timestamp to an integer (you may need to cast or truncate it)
                var timestampInteger = (uint)unixTimestamp;

                // Serialize the integer into a 4-byte array
                writer.Write(BitConverter.GetBytes(timestampInteger).Reverse().ToArray());

            }
            else if (field.PropertyType == typeof(int))
            {
                var value = (int)field.GetValue(obj);
                writer.Write(BitConverter.GetBytes(value).Reverse().ToArray());
            }
            else if (field.PropertyType == typeof(ushort))
            {
                var value = (ushort)field.GetValue(obj);
                writer.Write(BitConverter.GetBytes(value).Reverse().ToArray());
            }
            else
            {
                throw new Exception("Type not implemented");
            }
        }
    }
}

class Error
{
    public string Message { get; set; }
}

class Plate
{
    public string PlateNumber { get; set; }
    public DateTime Timestamp { get; set; }
}

class Ticket
{
    public string PlateNumber { get; set; }
    public ushort Road { get; set; }
    public ushort MileStart { get; set; }
    public DateTime Start { get; set; }
    public ushort MileEnd { get; set; }
    public DateTime End { get; set; }
    public ushort Speed { get; set; }
    public override string ToString()
    {
        return $"Ticket Road:{Road} PlateNumber:{PlateNumber} Speed:{Speed}";
    }
}

class WantHeartbeat
{
    public int Interval { get; set; }
}

class Heartbeat
{
}

class IAmCamera
{
    public ushort Road { get; set; }
    public ushort Mile { get; set; }
    public ushort SpeedLimit { get; set; }

    public override string ToString()
    {
        return $"Camera Road:{Road} Mile:{Mile} SpeedLimit:{SpeedLimit}";
    }
}

class IAmDispatcher
{
    public ushort[] Roads { get; set; }

    public override string ToString()
    {
        return $"Dispatcher Roads:{string.Join(",", Roads)}";
    }
}