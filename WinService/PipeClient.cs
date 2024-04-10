using System.IO.Pipes;
using TimeoutException = System.ServiceProcess.TimeoutException;

namespace WinService;

// https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication
// https://gist.github.com/AArnott/0d5f4645ad7e9a765cee#file-namedpipes-cs
public class PipeClient
{
    public static async Task SendCommand(string pipeName, string command, CancellationToken token)
    {
        var clientPipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await clientPipe.ConnectAsync(5000, token);

        var writer = new StreamWriter(clientPipe)
        {
            AutoFlush = true
        };

        var reader = new StreamReader(clientPipe);
        var readTask = reader.ReadLineAsync(token).AsTask();
        
        // check if pipe is from ClassInsights
        if (await ExecuteOperationWithTimeOut(readTask, 5000, token) != "ClassInsights")
        {
            Logger.Error($"{pipeName} is from another application!");
            return;
        }

        // send command to WinClient
        var writeTask = writer.WriteLineAsync(command);
        await ExecuteOperationWithTimeOut(writeTask, 5000, token);
        
        // check if command was successful
        if (await ExecuteOperationWithTimeOut(reader.ReadLineAsync(token).AsTask(), 5000, token) is { } line && line != "OK")
            Logger.Error($"{line} for {pipeName} did not succeed!");

        await ExecuteOperationWithTimeOut(writer.WriteLineAsync("BYE"), 5000, token);
        clientPipe.WaitForPipeDrain();

        clientPipe.Close();
    }
    

    private static async Task ExecuteOperationWithTimeOut(Task operationTask, int timeout, CancellationToken token)
    {
        var completedTask = await Task.WhenAny(operationTask, Task.Delay(timeout, token));
        if (completedTask != operationTask)
            throw new TimeoutException("Task timed out!");

        await operationTask;
    }
    
    private static async Task<T> ExecuteOperationWithTimeOut<T>(Task<T> operationTask, int timeout, CancellationToken token)
    {
        var completedTask = await Task.WhenAny(operationTask, Task.Delay(timeout, token));
        if (completedTask != operationTask)
            throw new TimeoutException("Task timed out!");

        return await operationTask;
    }
}