using System.IO.Pipes;

namespace WinService;

// https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication
// https://gist.github.com/AArnott/0d5f4645ad7e9a765cee#file-namedpipes-cs
public class PipeClient
{
    public static async Task SendShutdown(string pipeName, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new NotImplementedException();

        var clientPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await clientPipe.ConnectAsync(token);

        var writer = new StreamWriter(clientPipe)
        {
            AutoFlush = true
        };

        var reader = new StreamReader(clientPipe);
        var line = await reader.ReadLineAsync();

        if (line != "AutoShutdown") throw new ApplicationException("Error");

        await writer.WriteLineAsync("shutdown");

        line = await reader.ReadLineAsync();
        if (line is not "OK") throw new ApplicationException("Error");

        await writer.WriteLineAsync("BYE");
        clientPipe.WaitForPipeDrain();

        clientPipe.Close();
    }
}