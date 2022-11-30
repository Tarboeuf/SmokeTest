// See https://aka.ms/new-console-template for more information
using Common;
using System.Net.Sockets;
using System.Runtime.InteropServices;

await TcpServer.New()
    .HandleRaw(Handle);

async Task<bool> Handle(Socket socket, byte[] buffer, int size)
{
    List<Request> messages = new List<Request>();
    Console.WriteLine($"Receiving ({size}) : {string.Join(' ', buffer)}");
    for (int i = 0; i < size / 9; i++)
    {
        var data = buffer.AsSpan().Slice(i * 9, 9).ToArray();
        messages.Add(new Request
        {
            Type = data[0] == 73 ? RequestType.Insert : data[0] == 81 ? RequestType.Query : RequestType.Error,
            Value1 = MemoryMarshal.Read<int>(new ReadOnlySpan<byte>(data, 1, 4)),
            Value2 = MemoryMarshal.Read<int>(new ReadOnlySpan<byte>(data, 5, 4)),
        });
    }

    Dictionary<int, int> dataBase = new Dictionary<int, int>();

    foreach (var item in messages)
    {
        switch (item.Type)
        {
            case RequestType.Insert:
                dataBase.Add(item.Value1, item.Value2);
                break;
            case RequestType.Query:
                var values = dataBase.Where(v => v.Key >= item.Value1 && v.Key <= item.Value2).Select(v => v.Value).ToList();
                int result = 0;
                if (values.Count > 0)
                {
                    result = (int)values.Average();
                }
                var bytes = BitConverter.GetBytes(result);
                await socket.SendAsync(new ArraySegment<byte>(bytes), SocketFlags.None);
                break;
            case RequestType.Error:
                break;
            default:
                break;
        }
    }

    return true;
}

class Request
{
    public RequestType Type { get; set; }
    public int Value1 { get; set; }
    public int Value2 { get; set; }
}

enum RequestType
{
    Insert, 
    Query,
    Error
}