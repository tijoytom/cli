using Autofac;
using dotnet_lib;

namespace dotnet
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder
                .RegisterType<Bootstrapper>()
                .As<IBootstrapper>();
            builder
                .RegisterType<ProcessStarter>()
                .As<IProcessStarter>();
            var container = builder.Build();
            return container.Resolve<IBootstrapper>().Start(args);
        }
    }
}
