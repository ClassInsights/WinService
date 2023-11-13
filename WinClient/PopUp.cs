using System.Diagnostics;
using System.IO.Pipes;
using System.Media;
using System.Security.Principal;

namespace WinClient;

// https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication
// https://gist.github.com/AArnott/0d5f4645ad7e9a765cee#file-namedpipes-cs
public partial class PopUp : Form
{
    public PopUp()
    {
        InitializeComponent();
        StartPipe();
    }

    private bool _allowVisible;
    protected override void SetVisibleCore(bool value)
    {
        if (!_allowVisible)
        {
            value = false;
            if (!IsHandleCreated) CreateHandle();
        }
        base.SetVisibleCore(value);
    }

    private async void StartPipe()
    {
        var username = WindowsIdentity.GetCurrent().Name;
        if (username.Contains('\\')) username = username.Split('\\')[1];

        await ServerThread($"AutoShutdown-{username}");
    }

    private async Task ServerThread(string pipeName)
    { 
        var serverPipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
        while (true)
        {
            await serverPipe.WaitForConnectionAsync();

            var writer = new StreamWriter(serverPipe) { AutoFlush = true };

            var reader = new StreamReader(serverPipe);
            await writer.WriteLineAsync("AutoShutdown");
            do
            {
                var line = await reader.ReadLineAsync();
                if (line is "BYE" or null)
                    break;

                switch (line)
                {
                    case "shutdown":
                        Process.Start("shutdown", "/s /f /t 300");
                        _allowVisible = true;
                        Show();
                        await writer.WriteLineAsync("OK");
                        break;
                    case "logoff":
                        Process.Start("shutdown", "/l");
                        await writer.WriteLineAsync("OK");
                        break;
                }
            } while (true);
            serverPipe.Disconnect();
        }
    }

    // Play Error Sound
    private void PopUpShown(object sender, EventArgs e)
    {
        SystemSounds.Hand.Play();
    }

    private void BtnNoClick(object sender, EventArgs e)
    {
        Process.Start("shutdown", "/a");
        _allowVisible = false;
        Hide();
    }

    private void BtnYesClick(object sender, EventArgs e)
    {
        Process.Start("shutdown", "/s /f /t 0");
    }
}