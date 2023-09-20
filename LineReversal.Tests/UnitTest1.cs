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
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/12345/20/");
            await LineReversal.Program.ProcessKind(replier.Object, "/close/12345/");

            replier.Verify(r => r.Reply("/ack/12345/0/"));
            replier.Verify(r => r.Reply("/ack/12345/6/"));
            replier.Verify(r => r.Reply("/data/12345/0/olleh\n/"));
            replier.Verify(r => r.Reply("/ack/12345/20/"));
            replier.Verify(r => r.Reply("/data/12345/6/!dlrow ,olleH\n/"));
            replier.Verify(r => r.Reply("/close/12345/"));
        }


        [Fact]
        public async Task TwoLineSession()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await LineReversal.Program.ProcessKind(replier.Object, "/connect/12345/");
            await LineReversal.Program.ProcessKind(replier.Object, "/data/12345/0/hello\n/");
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/12345/6/");
            await LineReversal.Program.ProcessKind(replier.Object, "/data/12345/6/Hello\n, world!\n/");
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/12345/20/");
            await LineReversal.Program.ProcessKind(replier.Object, "/close/12345/");

            replier.Verify(r => r.Reply("/ack/12345/0/"));
            replier.Verify(r => r.Reply("/ack/12345/6/"));
            replier.Verify(r => r.Reply("/data/12345/0/olleh\n/"));
            replier.Verify(r => r.Reply("/ack/12345/21/"));
            replier.Verify(r => r.Reply("/data/12345/6/olleH\n!dlrow ,\n/"));
            replier.Verify(r => r.Reply("/close/12345/"));
        }

        [Fact]
        public async Task FailingCase()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await LineReversal.Program.ProcessKind(replier.Object, "/connect/637914123/");
            await LineReversal.Program.ProcessKind(replier.Object, "/data/637914123/0/aid good giant intrusion casino aid for favicon of now the giant\nPROTOHACKERS of nasa giant prisoners quartz time integral of is quartz is something to party good now\nquartz now gia/");
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/637914123/167/");
            await LineReversal.Program.ProcessKind(replier.Object, "/data/637914123/181/nt/");
            await LineReversal.Program.ProcessKind(replier.Object, "/data/637914123/183/ royale integral good nasa the\ntime men prisoners men bluebell the is nasa sphinx love good to aid of the men peach something\ngood hypnotic quartz aid time favicon PROTOHACKERS sphinx love giant bluebell calculator my of fo/");
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/637914123/167/");

            replier.Verify(r => r.Reply("/ack/637914123/0/"));
            replier.Verify(r => r.Reply("/ack/637914123/181/"));
            replier.Verify(r => r.Reply("/data/637914123/0/tnaig eht won fo nocivaf rof dia onisac noisurtni tnaig doog dia\nwon doog ytrap ot gnihtemos si ztrauq si fo largetni emit ztrauq srenosirp tnaig asan fo SREKCAHOTORP\n/"));
            replier.Verify(r => r.Reply("/ack/637914123/183/"));
            replier.Verify(r => r.Reply("/ack/637914123/406/"));
            replier.Verify(r => r.Reply("/data/637914123/167/eht asan doog largetni elayor tnaig won ztrauq\ngnihtemos hcaep nem eht fo dia ot doog evol xnihps asan si eht llebeulb nem srenosirp nem emit\n/"), Times.Exactly(2));
        }
    }
}