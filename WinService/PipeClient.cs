using System.IO.Pipes;

namespace WinService;

// https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication
// https://gist.github.com/AArnott/0d5f4645ad7e9a765cee#file-namedpipes-cs
public class PipeClient
{
    public static async Task SendCommand(string pipeName, string command, CancellationToken token)
    {
        var clientPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await clientPipe.ConnectAsync(token);

        var writer = new StreamWriter(clientPipe)
        {
            AutoFlush = true
        };

        var reader = new StreamReader(clientPipe);
        var line = await reader.ReadLineAsync(token);

        if (line != "ClassInsights")
        {
            Logger.Error($"{pipeName} is from another application!");
            return;
        }

        await writer.WriteLineAsync(command);

        line = await reader.ReadLineAsync(token);
        if (line is not "OK") Logger.Error($"{line} for {pipeName} did not succeed!");
        
        await writer.WriteLineAsync("BYE");
        clientPipe.WaitForPipeDrain();

        clientPipe.Close();
    }
}