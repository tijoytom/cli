using System.Diagnostics;

namespace dotnet_lib
{
    public interface IProcessStarter
    {
        int Start(ProcessStartInfo startInfo);
    }
}
