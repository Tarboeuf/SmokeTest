using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;

namespace LineReversal.Tests
{
    public class LineReversalFixture
    {
        [Fact]
        public async Task WhenDataIsReceived_DataIsSent()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await LineReversal.Program.ProcessKind(replier.Object, "/data/");
        }

        [Fact]
        public async Task BasicSession()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await LineReversal.Program.ProcessKind(replier.Object, "/connect/12345/");
            await LineReversal.Program.ProcessKind(replier.Object, "/data/12345/0/hello\n/");
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/12345/6/");
            await LineReversal.Program.ProcessKind(replier.Object, "/data/12345/6/Hello, world!\n/");
            await LineReversal.Program.ProcessKind(replier.Object, "/data/12345/6/!dlrow ,olleH\n/");
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/12345/20/");
            await LineReversal.Program.ProcessKind(replier.Object, "/close/12345/");

            replier.Verify(r => r.Reply2("/ack/12345/0/"));
            replier.Verify(r => r.Reply2("/ack/12345/6/"));
            replier.Verify(r => r.Reply2("/data/12345/0/olleh\n/"));
            replier.Verify(r => r.Reply2("/ack/12345/20/"));
            replier.Verify(r => r.Reply2("/data/12345/6/!dlrow ,olleH\n/"));
            replier.Verify(r => r.Reply2("/close/12345/"));
        }


        [Fact]
        public async Task TwoLineSession()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await LineReversal.Program.ProcessKind(replier.Object, "/connect/12345/");
            await LineReversal.Program.ProcessKind(replier.Object, "/data/12345/0/hello\n/");
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/12345/6/");
            await LineReversal.Program.ProcessKind(replier.Object, "/data/12345/6/Hello\n, world!\n/");
            await LineReversal.Program.ProcessKind(replier.Object, "/data/12345/6/!dlrow\n ,olleH\n/");
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/12345/20/");
            await LineReversal.Program.ProcessKind(replier.Object, "/close/12345/");

            replier.Verify(r => r.Reply2("/ack/12345/0/"));
            replier.Verify(r => r.Reply2("/ack/12345/6/"));
            replier.Verify(r => r.Reply2("/data/12345/0/olleh\n/"));
            replier.Verify(r => r.Reply2("/ack/12345/20/"));
            replier.Verify(r => r.Reply2("/data/12345/6/!dlrow ,olleH\n/"));
            replier.Verify(r => r.Reply2("/close/12345/"));
        }
    }
}