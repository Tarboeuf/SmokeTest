using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace IntegrationTests
{
    public class PrimeTimeFixture
    {
        [Fact]
        public void PrimeTimeRequestOk()
        {
            TcpClient client = new TcpClient("localhost", 10001);
            
            Write(client.GetStream(), new Request { Method = "isPrime", Number = 216211});

            var response = Read(client.GetStream());

            Assert.Equal("isPrime", response!.Method);
            Assert.True(response.Prime);

            Write(client.GetStream(), new Request { Method = "isPrime", Number = 216212 });

            response = Read(client.GetStream());

            Assert.Equal("isPrime", response!.Method);
            Assert.False(response.Prime);
        }

        [Fact]
        public void PrimeTimeHttpHeader()
        {
            TcpClient client = new TcpClient("localhost", 10001);
            
            Write(client.GetStream(), new Request { Method = "isPrime", Number = 216211});

            var response = Read(client.GetStream());

            Assert.Equal("isPrime", response!.Method);
            Assert.True(response.Prime);
            string value = @"GET /prime/3064090 HTTP/1.1
Connection: close
User-Agent: protohackers
Accept: application/json
Host: localhost



";
            Write(client.GetStream(), value);

            //Assert.Equal("FAIL : GET /prime/3064090 HTTP/1.1", ReadString(client.GetStream()));
            //Assert.Equal("FAIL : Connection: close", ReadString(client.GetStream()));
            //Assert.Equal("FAIL : User-Agent: protohackers", ReadString(client.GetStream()));
            //Assert.Equal("FAIL : Accept: application/json", ReadString(client.GetStream()));
            //Assert.Equal("FAIL : Host: localhost", ReadString(client.GetStream()));
            Assert.Equal("{\"method\":null,\"prime\":false}\n", ReadString(client.GetStream()));
            Assert.Equal("{\"method\":null,\"prime\":false}\n", ReadString(client.GetStream()));
            Assert.Equal("{\"method\":null,\"prime\":false}\n", ReadString(client.GetStream()));
            Assert.Equal("{\"method\":null,\"prime\":false}\n", ReadString(client.GetStream()));
            Assert.Equal("{\"method\":null,\"prime\":false}\n", ReadString(client.GetStream()));

            Assert.Equal("isPrime", response!.Method);
            Assert.False(response.Prime);
        }

        void Write(NetworkStream stream, Request request)
        {
            string value = JsonSerializer.Serialize(request);
            var bytes = Encoding.UTF8.GetBytes(value);
            stream.Write(bytes);
        }

        void Write(NetworkStream stream, string request)
        {
            var bytes = Encoding.UTF8.GetBytes(request);
            stream.Write(bytes);
        }

        Response? Read(NetworkStream stream)
        {
            var bytes = new byte[1024 * 1024];

            var length = stream.Read(bytes);
            var value = Encoding.UTF8.GetString(bytes, 0, length);
            if (value != null)
            {
                return JsonSerializer.Deserialize<Response>(value);
            }
            return null;
        }

        string? ReadString(NetworkStream stream)
        {
            var bytes = new byte[1024 * 1024];

            var length = stream.Read(bytes);
            var value = Encoding.UTF8.GetString(bytes, 0, length);
            return value;
        }
    }
}