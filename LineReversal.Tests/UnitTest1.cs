using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;

namespace LineReversal.Tests
{
    public class LineReversalFixture
    {
        [Fact]
        public async Task BasicSession()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await Program.ProcessKind(replier.Object, "/connect/12345/");
            await Program.ProcessKind(replier.Object, "/data/12345/0/hello\n/");
            await Program.ProcessKind(replier.Object, "/ack/12345/6/");
            await Program.ProcessKind(replier.Object, "/data/12345/6/Hello, world!\n/");
            await Program.ProcessKind(replier.Object, "/ack/12345/20/");
            await Program.ProcessKind(replier.Object, "/close/12345/");

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
            await Program.ProcessKind(replier.Object, "/connect/12345/");
            await Program.ProcessKind(replier.Object, "/data/12345/0/hello\n/");
            await Program.ProcessKind(replier.Object, "/ack/12345/6/");
            await Program.ProcessKind(replier.Object, "/data/12345/6/Hello\n, world!\n/");
            await Program.ProcessKind(replier.Object, "/ack/12345/20/");
            await Program.ProcessKind(replier.Object, "/close/12345/");

            replier.Verify(r => r.Reply("/ack/12345/0/"));
            replier.Verify(r => r.Reply("/ack/12345/6/"));
            replier.Verify(r => r.Reply("/data/12345/0/olleh\n/"));
            replier.Verify(r => r.Reply("/ack/12345/21/"));
            replier.Verify(r => r.Reply("/data/12345/6/olleH\n!dlrow ,\n/"));
            replier.Verify(r => r.Reply("/close/12345/"));
        }

        [Fact]
        public async Task NewAckCase()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await Program.ProcessKind(replier.Object, "/connect/637914123/");
            await Program.ProcessKind(replier.Object, "/data/637914123/0/aid good giant intrusion casino aid for favicon of now the giant\nPROTOHACKERS of nasa giant prisoners quartz time integral of is quartz is something to party good now\nquartz now gia/");
            await Program.ProcessKind(replier.Object, "/data/637914123/181/nt/");
            await Program.ProcessKind(replier.Object, "/data/637914123/183/ royale integral good nasa the\ntime men prisoners men bluebell the is nasa sphinx love good to aid of the men peach something\ngood hypnotic quartz aid time favicon PROTOHACKERS sphinx love giant bluebell calculator my of fo\n/");
            await Program.ProcessKind(replier.Object, "/ack/637914123/407/");

            replier.Verify(r => r.Reply("/ack/637914123/0/"));
            replier.Verify(r => r.Reply("/ack/637914123/181/"));
            replier.Verify(r => r.Reply("/ack/637914123/183/"));
            replier.Verify(r => r.Reply("/ack/637914123/407/"));
            replier.Verify(r => r.Reply("/data/637914123/0/tnaig eht won fo nocivaf rof dia onisac noisurtni tnaig doog dia\nwon doog ytrap ot gnihtemos si ztrauq si fo largetni emit ztrauq srenosirp tnaig asan fo SREKCAHOTORP\neht asan doog largetni elayor tnaig won ztrauq\ngnihtemos hcaep nem eht fo dia ot doog evol xnihps asan si eht llebeulb nem srenosirp nem emit\nof fo ym rotaluclac llebeulb tnaig evol xnihps SREKCAHOTORP nocivaf emit dia ztrauq citonpyh doog\n/"));
        }

        [Fact]
        public async Task ShouldEndWithADash()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await LineReversal.Program.ProcessKind(replier.Object, "/connect/8484885/");
            replier.Verify(r => r.Reply("/ack/8484885/0/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/connect/8484885/");
            replier.Verify(r => r.Reply("/ack/8484885/0/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/8484885/0/the come my sphinx my the the party tim/");
            replier.Verify(r => r.Reply("/ack/8484885/39/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/8484885/39/e for integral\nbluebell something intrusion of love royale th/");
            replier.Verify(r => r.Reply("/ack/8484885/100/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/8484885/100/e all/");
            replier.Verify(r => r.Reply("/ack/8484885/105/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/8484885/105/\n");
            await LineReversal.Program.ProcessKind(replier.Object, "/close/8484885/");
            replier.Verify(r => r.Reply("/close/8484885/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/close/8484885/");
            replier.Verify(r => r.Reply("/close/8484885/"));

            replier.VerifyAll();
        }

        [Fact]
        public async Task Fail()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await LineReversal.Program.ProcessKind(replier.Object, "/connect/1734878917/");
            replier.Verify(r => r.Reply("/ack/1734878917/0/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1734878917/0/the for about sphinx is calculator to men peach giant aid hypnotic favicon\nthe casino royale of now party is to peach for the giant now for/");
            replier.Verify(r => r.Reply("/ack/1734878917/139/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1734878917/139/ co/");
            replier.Verify(r => r.Reply("/ack/1734878917/142/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1734878917/142/m/");
            replier.Verify(r => r.Reply("/ack/1734878917/143/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1734878917/143/e/");
            replier.Verify(r => r.Reply("/ack/1734878917/144/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1734878917/144/\n/");
            replier.Verify(r => r.Reply("/ack/1734878917/145/"));
            replier.Verify(r => r.Reply("/data/1734878917/0/nocivaf citonpyh dia tnaig hcaep nem ot rotaluclac si xnihps tuoba rof eht\nemoc rof won tnaig eht rof hcaep ot si ytrap won fo elayor onisac eht\n/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/1734878917/145/");
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1734878917/144/\n/");
            replier.Verify(r => r.Reply("/ack/1734878917/1/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/close/1734878917/");
            replier.Verify(r => r.Reply("/close/1734878917/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1734878917/144/\n/");
            replier.Verify(r => r.Reply("/close/1734878917/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1734878917/144/\n/");
            replier.Verify(r => r.Reply("/close/1734878917/"));

            replier.VerifyAll();
        }

        [Fact]
        public async Task MessageNotCompleteShouldNotBeenSent()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await Program.ProcessKind(replier.Object, "/connect/1/");
            replier.Verify(r => r.Reply("/ack/1/0/"));
            await Program.ProcessKind(replier.Object, "/data/1/0/A\nB/");
            replier.Verify(r => r.Reply("/ack/1/3/"));
            await Program.ProcessKind(replier.Object, "/data/1/3/C\n/");
            replier.Verify(r => r.Reply("/ack/1/5/"));
            replier.Verify(r => r.Reply("/data/1/0/A\nCB\n/"));
        }
    }
}