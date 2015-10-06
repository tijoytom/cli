namespace dotnet
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var bootstrapper = new Bootstrapper();
            return bootstrapper.Start(args);
        }
    }
}
