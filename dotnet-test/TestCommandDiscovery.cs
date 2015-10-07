using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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
                //SetupTest();
                int actual = bootStrapper.Start(new[] {"dir"});
                Assert.Equal(startProc.FileName, "dotnet-dir");
                Assert.Equal(startProc.Arguments, "");
                Assert.Equal(0, actual);
                //CleanupTest();
            }
            
        }

        private static void SetupTest()
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
                
            var fileName = Path.Combine(dir, "dotnet-dir.exe");
  
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            using (File.Create(fileName)) { }

            var value = Environment.GetEnvironmentVariable("PATH") + ";" + dir;
            if (!value.Contains(";" + dir))
            {
                Console.WriteLine(value);
                Environment.SetEnvironmentVariable("PATH", value);
            }
        }

        private static void CleanupTest()
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }

            var value = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(value)) return;

            value = value.Replace(";" + dir, "");
            Console.WriteLine(value);
            Environment.SetEnvironmentVariable("PATH", value);
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
                //SetupTest();
                int actual = bootStrapper.Start(new[] {"dir", "testArg"});
                Assert.Equal(startProc.FileName, "dotnet-dir");
                Assert.Equal(startProc.Arguments, "\"testArg\"");
                Assert.Equal(2, actual);
                //CleanupTest();
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
