using Microsoft.Win32;
using Timer = System.Timers.Timer;

namespace WinService.Manager;

public class UserManager
{
    private readonly WinService _winService;
    private string? _userName;
    private Timer? _timer;

    public UserManager(WinService winService)
    {
        _winService = winService;
    }

    public async Task StartWinAuthFlow(CancellationToken token)
    {
        await UpdateWinAuthToken(token);
        _timer = new Timer
        {
            Interval = 60000,
            Enabled = true
        };
        _timer.Elapsed += async (_, _) => await UpdateWinAuthToken(token);
        SystemEvents.SessionEnded += async (_, _) => await UpdateWinAuthToken(token);
        SystemEvents.SessionSwitch += async (_, _) => await UpdateWinAuthToken(token);
        _timer.Start();
    }

    public async Task UpdateWinAuthToken(CancellationToken token)
    {
#if DEBUG
        const string name = "PGI\\julian";
#else
        if (ShutdownManager.GetLoggedInUsername() is not { } name)
        {
            _winService.WinAuthToken = IntPtr.Zero; // reset token if no user is logged in
            return;
        }
#endif
        await Task.Run(() =>
        {
            // check if last access token is from currently logged in user
            if (name == _userName) return;
            
            var accessToken = IntPtr.Zero;
            Win32Api.GetSessionUserToken(ref accessToken);

            _winService.WinAuthToken = accessToken;
            _userName = name;
        }, token);
    }
}