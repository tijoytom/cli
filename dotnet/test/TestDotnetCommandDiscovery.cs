using Xunit;

namespace dotnet.test
{
    class TestDotnetCommandDiscovery
    {
        [Fact]
        public void NoCommand()
        {
            var args = new string[0];
            IBootstrapper bootstrapper = new Bootstrapper();
            Assert.Equal(0, bootstrapper.Start(args));
        }

        [Fact]
        public void CommandExists()
        {
            var args = new[] { "ls" };
            IBootstrapper bootstrapper = new Bootstrapper();
            Assert.Equal(0, bootstrapper.Start(args));
        }

        [Fact]
        public void CommandDoesNotExist()
        {
            var args = new [] { "commandNotFound" };
            IBootstrapper bootstrapper = new Bootstrapper();
            Assert.Equal(1, bootstrapper.Start(args));
        }
    }
}
