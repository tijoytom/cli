using Xunit;

namespace dotnet
{
    class TestDotnetCommandDiscovery
    {
        [Fact]
        public void NoArguments()
        {
            var bootstrapper = new Bootstrapper();
            var args = new string[0];
            Assert.Equal(0, bootstrapper.Start(args));
        }

        [Fact]
        public void CommandExists()
        {
            var bootstrapper = new Bootstrapper();
            var args = new string[1];
            args[0] = "ls";
            Assert.Equal(0, bootstrapper.Start(args));
        }

        [Fact]
        public void CommandDoesNotExist()
        {
            var bootstrapper = new Bootstrapper();
            var args = new string[1];
            args[0] = "commandNotFound";
            Assert.Equal(1, bootstrapper.Start(args));
        }
    }
}
