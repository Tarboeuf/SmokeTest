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
            replier.Verify(r => r.Reply("/ack/12345/0/"));

            await Program.ProcessKind(replier.Object, "/data/12345/0/hello\n/");
            replier.Verify(r => r.Reply("/ack/12345/6/"));

            replier.Verify(r => r.Reply("/data/12345/0/olleh\n/"));
            await Program.ProcessKind(replier.Object, "/ack/12345/6/");

            await Program.ProcessKind(replier.Object, "/data/12345/6/Hello, world!\n/");
            replier.Verify(r => r.Reply("/ack/12345/20/"));
            await Program.ProcessKind(replier.Object, "/ack/12345/20/");
            replier.Verify(r => r.Reply("/data/12345/6/!dlrow ,olleH\n/"));

            await Program.ProcessKind(replier.Object, "/close/12345/");
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
        public async Task ShouldNotRemoveData()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await LineReversal.Program.ProcessKind(replier.Object, "/connect/1659022309/");
            replier.Verify(r => r.Reply("/ack/1659022309/0/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1659022309/0/giant for\nnow favicon all to integral the giant come to come of aid\nPROTOHACKERS for\ngood jackdaws the integral intrusi/");
            replier.Verify(r => r.Reply("/ack/1659022309/119/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1659022309/119/on\ngood men the party sphinx royale now good hypnotic good casino sphinx/");
            replier.Verify(r => r.Reply("/ack/1659022309/191/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1659022309/119/on\ngood men the party sphinx royale now good hypnotic good casino sphinx my giant is\nmen now giant quartz time PROTOHACKERS quartz my bluebell bluebell integral party the aid my about intrusion quartz peach of\nnow my intrusion favicon giant\naid integral bluebell all all my royale to of favicon the giant PROTOHACKERS /");
            replier.Verify(r => r.Reply("/ack/1659022309/437/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1659022309/437/royale aid of for /");
            replier.Verify(r => r.Reply("/ack/1659022309/455/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1659022309/455/g/");
            replier.Verify(r => r.Reply("/ack/1659022309/456/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1659022309/456/oo/");
            replier.Verify(r => r.Reply("/ack/1659022309/458/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1659022309/458/d the\nprisoners intrusion my all/");
            replier.Verify(r => r.Reply("/ack/1659022309/490/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1659022309/490/ a/");
            replier.Verify(r => r.Reply("/ack/1659022309/492/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1659022309/492/i/");
            replier.Verify(r => r.Reply("/ack/1659022309/493/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1659022309/493/d sphin/");
            replier.Verify(r => r.Reply("/ack/1659022309/500/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1659022309/500/x/");
            replier.Verify(r => r.Reply("/ack/1659022309/501/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1659022309/501/\n/");
            replier.Verify(r => r.Reply("/ack/1659022309/502/"));
            replier.Verify(r => r.Reply("/data/1659022309/0/rof tnaig\ndia fo emoc ot emoc tnaig eht largetni ot lla nocivaf won\nrof SREKCAHOTORP\nnoisurtni largetni eht swadkcaj doog\nnoxnihps onisac doog citonpyh doog won elayor xnihps ytrap eht nem doog\nsi tnaig ym xnihps onisac doog citonpyh doog won elayor xnihps ytrap eht nem doog\nfo hcaep ztrauq noisurtni tuoba ym dia eht ytrap largetni llebeulb llebeulb ym ztrauq SREKCAHOTORP emit ztrauq tnaig won nem\ntnaig nocivaf noisurtni ym won\neht doog rof fo dia elayor SREKCAHOTORP tnaig eht nocivaf fo ot elayor ym lla lla llebeulb largetni dia\nxnihps dia lla ym noisurtni srenosirp\n/"));


            replier.VerifyAll();
        }

        [Fact]
        public async Task DataIsAppend()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await LineReversal.Program.ProcessKind(replier.Object, "/connect/1212542098/");
            replier.Verify(r => r.Reply("/ack/1212542098/0/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/connect/1212542098/");
            replier.Verify(r => r.Reply("/ack/1212542098/0/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1212542098/0/come the come hypnotic party casino peach hypnotic favicon\nprisoners of come sphinx calculator calcu/");
            replier.Verify(r => r.Reply("/ack/1212542098/100/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1212542098/0/come the come hypnotic party casino peach hypnotic favicon\nprisoners of come sphinx calculator calcu/");
            replier.Verify(r => r.Reply("/ack/1212542098/100/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1212542098/100/lator is for t/");
            replier.Verify(r => r.Reply("/ack/1212542098/114/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1212542098/100/lator is for the party nasa all time now /");
            replier.Verify(r => r.Reply("/ack/1212542098/141/"));


            replier.VerifyAll();
        }

        [Fact]
        public async Task DataAlreadyReceived()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await LineReversal.Program.ProcessKind(replier.Object, "/connect/1398743517/");
            replier.Verify(r => r.Reply("/ack/1398743517/0/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1398743517/0/giant my the\nfor prisoners integral the intrusion to to aid giant prisoners integral to/");
            replier.Verify(r => r.Reply("/ack/1398743517/87/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1398743517/0/giant my the\nfor prisoners integral the intrusion to to aid giant prisoners integral to to jackdaws about PROTOHACKERS nasa par/");
            replier.Verify(r => r.Reply("/ack/1398743517/87/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1398743517/87/ty l/");
            replier.Verify(r => r.Reply("/ack/1398743517/127/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1398743517/127/ov/");
            replier.Verify(r => r.Reply("/ack/1398743517/129/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1398743517/129/e/");
            replier.Verify(r => r.Reply("/ack/1398743517/130/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/1398743517/130/e\n/");
            replier.Verify(r => r.Reply("/ack/1398743517/132/"));
            replier.Verify(r => r.Reply("/data/1398743517/0/eht ym tnaig\neevorap asan SREKCAHOTORP tuoba swadkcaj ot ot largetni srenosirp tnaig dia ot ot noisurtni eht largetni srenosirp rof\n/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/1398743517/135/");
            await LineReversal.Program.ProcessKind(replier.Object, "/close/1398743517/");
            replier.Verify(r => r.Reply("/close/1398743517/"));


            replier.VerifyAll();
        }


        [Fact]
        public async Task AppendData()
        {
            Mock<IReplier> replier = new Mock<IReplier>();
            await LineReversal.Program.ProcessKind(replier.Object, "/connect/905728477/");
            replier.Verify(r => r.Reply("/ack/905728477/0/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/905728477/0/a/");
            replier.Verify(r => r.Reply("/ack/905728477/1/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/905728477/1/b/");
            replier.Verify(r => r.Reply("/ack/905728477/2/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/905728477/1/b/");
            replier.Verify(r => r.Reply("/ack/905728477/2/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/905728477/2/c\nd/");
            replier.Verify(r => r.Reply("/ack/905728477/5/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/905728477/5/e/");
            replier.Verify(r => r.Reply("/ack/905728477/6/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/905728477/5/ef/");
            replier.Verify(r => r.Reply("/ack/905728477/7/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/905728477/5/efg/");
            replier.Verify(r => r.Reply("/ack/905728477/8/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/905728477/8/h/");
            replier.Verify(r => r.Reply("/ack/905728477/9/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/data/905728477/9/\n/");
            replier.Verify(r => r.Reply("/ack/905728477/10/"));
            replier.Verify(r => r.Reply("/data/905728477/0/cba\nhgfed\n/"));
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/905728477/10/");
            await LineReversal.Program.ProcessKind(replier.Object, "/close/905728477/");
            replier.Verify(r => r.Reply("/close/905728477/"));


            replier.VerifyAll();
        }

        [Fact]
        public async Task Fail()
        {
            Mock<IReplier> replier = new Mock<IReplier>();

            await LineReversal.Program.ProcessKind(replier.Object, "/connect/1872234332/"); // 0
            replier.Verify(r => r.Reply("/ack/1872234332/0/")); // 0

            await LineReversal.Program.ProcessKind(replier.Object, "/data/1872234332/0/integral good all love all come th/"); // 34
            replier.Verify(r => r.Reply("/ack/1872234332/34/")); // 0

            await LineReversal.Program.ProcessKind(replier.Object, "/data/1872234332/34/e sphinx the party hypn/"); // 23
            replier.Verify(r => r.Reply("/ack/1872234332/57/")); // 0

            await LineReversal.Program.ProcessKind(replier.Object, "/data/1872234332/57/otic for for favicon abo/"); // 24
            replier.Verify(r => r.Reply("/ack/1872234332/81/")); // 0

            await LineReversal.Program.ProcessKind(replier.Object, "/data/1872234332/57/otic for for favicon abo/"); // 24
            replier.Verify(r => r.Reply("/ack/1872234332/81/")); // 0

            await LineReversal.Program.ProcessKind(replier.Object, "/data/1872234332/81/ut royale jackdaws giant jackdaws /"); // 34
            replier.Verify(r => r.Reply("/ack/1872234332/115/")); // 0

            await LineReversal.Program.ProcessKind(replier.Object, "/data/1872234332/115/goo/"); // 3
            replier.Verify(r => r.Reply("/ack/1872234332/118/")); // 0

            await LineReversal.Program.ProcessKind(replier.Object, "/data/1872234332/118/d/"); // 1
            replier.Verify(r => r.Reply("/ack/1872234332/119/")); // 0

            await LineReversal.Program.ProcessKind(replier.Object, "/data/1872234332/119/\n/"); // 1
            replier.Verify(r => r.Reply("/ack/1872234332/120/")); // 0
            replier.Verify(r => r.Reply("/data/1872234332/0/doog swadkcaj tnaig swadkcaj elayor tuoba nocivaf rof rof citonpyh ytrap eht xnihps eht emoc lla evol lla doog largetni\n/")); // 120
            await LineReversal.Program.ProcessKind(replier.Object, "/ack/1872234332/120/"); // 0
            await LineReversal.Program.ProcessKind(replier.Object, "/close/1872234332/"); // 0
            replier.Verify(r => r.Reply("/close/1872234332/")); // 0


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