using System.Runtime.InteropServices;

namespace WinService.Manager;

public class PowerManager
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    // Flag constants
    private const uint ES_CONTINUOUS = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;
    // ES_AWAYMODE_REQUIRED is useful for media applications on Windows Vista and later,
    // but you might omit it if it doesn't suit your needs.
    private const uint ES_AWAYMODE_REQUIRED = 0x00000040;

    public static void PreventSleep()
    {
        // Combine flags to continuously mark the system as in use
        SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_AWAYMODE_REQUIRED);
    }

    public static void RestoreSleepSettings()
    {
        // Reset to allow sleep again
        SetThreadExecutionState(ES_CONTINUOUS);
    }
}