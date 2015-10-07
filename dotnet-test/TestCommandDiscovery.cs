using System.Diagnostics;
using Autofac.Extras.Moq;
using dotnet_lib;
using Moq;
using Xunit;

namespace dotnet_test
{
    public class TestCommandDiscovery
    {
        [Fact]
        public void NoCommand()
        {
            using (var container = AutoMock.GetStrict())
            {
                var bootStrapper = container.Create<Bootstrapper>();
                int actual = bootStrapper.Start(new string[0]);
                Assert.Equal(0, actual);
            }
        }

        [Fact]
        public void CommandExists()
        {
            using (var container = AutoMock.GetStrict())
            {
                var startProc = new ProcessStartInfo();
                container.Mock<IProcessStarter>().Setup(ps => ps.Start(It.IsAny<ProcessStartInfo>())).Returns(
                    (ProcessStartInfo psi) =>
                    {
                        startProc = psi;
                        return 0;
                    });

                var bootStrapper = container.Create<Bootstrapper>();
                int actual = bootStrapper.Start(new[] {"dir"});
                Assert.Equal(startProc.FileName, "dotnet-dir");
                Assert.Equal(startProc.Arguments, "");
                Assert.Equal(0, actual);
            }
        }

        [Fact]
        public void CommandExistsWithArgs()
        {
            using (var container = AutoMock.GetStrict())
            {
                var startProc = new ProcessStartInfo();
                container.Mock<IProcessStarter>().Setup(ps => ps.Start(It.IsAny<ProcessStartInfo>())).Returns(
                    (ProcessStartInfo psi) =>
                    {
                        startProc = psi;
                        return 2;
                    });

                var bootStrapper = container.Create<Bootstrapper>();
                int actual = bootStrapper.Start(new[] {"dir", "testArg"});
                Assert.Equal(startProc.FileName, "dotnet-dir");
                Assert.Equal(startProc.Arguments, "testArg");
                Assert.Equal(2, actual);
            }
        }

        [Fact]
        public void CommandDoesNotExist()
        {
            using (var container = AutoMock.GetStrict())
            {
                var bootStrapper = container.Create<Bootstrapper>();
                int actual = bootStrapper.Start(new[] {"ls"});
                Assert.Equal(1, actual);
            }
        }
    }
}
