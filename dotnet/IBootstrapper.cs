using System.Diagnostics;

namespace dotnet
{
    public interface IBootstrapper
    {
        int Start(string[] args);
        bool ExecuteCommand(string[] args);
        bool ProcessStart(ProcessStartInfo processSettings);
    }
}
