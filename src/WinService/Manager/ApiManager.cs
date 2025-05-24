using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinService.Interfaces;
using WinService.Models;

namespace WinService.Manager;

public class ApiManager: IApiManager
{
    public string? Token { get; private set; }
    public string? ApiUrl { get; }
    
    private readonly HttpClient _httpClient = new();
    private readonly Lock _lock = new();
    private readonly ILogger<ApiManager> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    public ApiModels.Room? Room { get; private set; }
    public ApiModels.Computer? Computer {get; set; }

    public ApiManager(ILogger<ApiManager> logger, IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _appLifetime = appLifetime;

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ClassInsights");
        ApiUrl = key?.GetValue("ApiUrl")?.ToString();
        if (ApiUrl == null)
            throw new Exception("API url could not be found");
        
        _httpClient.BaseAddress = new Uri($"{ApiUrl}/api/");
        // start searching for room
        _ = Task.Run(FindRoomAsync);
        Computer = Task.Run(GetComputerAsync).GetAwaiter().GetResult();
    }

    private async Task FindRoomAsync()
    {
        try
        {
            do
            {
                if (await GetRoomAsync(Environment.MachineName) is not { } room)
                {
                    await Task.Delay(60 * 1000 * 5); // wait 5 minutes before trying again
                    continue;
                }
                
                Room = room;
                _logger.LogInformation("Room: {roomName} with Id {roomId}", room.DisplayName, room.RoomId);
                if (!room.Enabled)
                {
                    _logger.LogInformation("Room: {roomName} is disabled", room.DisplayName);
                    _appLifetime.StopApplication();
                }
                break;
            } while (Room == null);
        }
        catch (HttpRequestException)
        {
            _logger.LogCritical("Error while searching for room, stopping service");
            _appLifetime.StopApplication(); // maybe send user an information via WinClient as this is unexpected?
        }
    }
    
    private async Task<ApiModels.Computer?> GetComputerAsync()
    {
        try
        {
            return await GetComputerAsync(Environment.MachineName);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<ApiModels.Room?> GetRoomAsync(string name)
    {
        var response = await CallApiEndpointAsync($"rooms/{name}", HttpMethod.Get);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.Room);
    }

    private async Task<ApiModels.Computer?> GetComputerAsync(string name)
    {
        var response = await CallApiEndpointAsync($"computers/{name}", HttpMethod.Get);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.Computer);
    }

    public async Task<List<ApiModels.Lesson>> GetLessonsAsync(int? room = null)
    {
        if (room == null && Room == null) return [];
        var response = await CallApiEndpointAsync($"rooms/{room ?? Room!.RoomId}/lessons", HttpMethod.Get);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.ListLesson) ?? [];
    }

    public async Task<ApiModels.Computer?> UpdateComputer(ApiModels.Computer request)
    {
        var response = await CallApiEndpointAsync("computers", HttpMethod.Post, new StringContent(JsonSerializer.Serialize(request, SourceGenerationContext.Default.Computer), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.Computer);
    }

    public async Task<ApiModels.Settings?> GetSettingsAsync()
    {
        var response = await CallApiEndpointAsync("settings/dashboard", HttpMethod.Get);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.Settings);
    }

    public async Task<ApiModels.Client?> GetClientAsync()
    {
        var response = await CallApiEndpointAsync("client/version", HttpMethod.Get);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.Client);
    }

    public async Task<byte[]> GetClientInstallerAsync()
    {
        var response = await CallApiEndpointAsync("client/download", HttpMethod.Get);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    private async Task<HttpResponseMessage> CallApiEndpointAsync(string endpoint, HttpMethod method, HttpContent? content = null)
    {
        for (var i = 0; i < 3; i++)
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();

                var request = new HttpRequestMessage(method, endpoint) { Content = content };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) return response;
                _logger.LogWarning("Access token expired. Retrying...");
                lock (_lock) Token = null; // Force re-authentication
            }
            catch (HttpRequestException ex)
            {
                if (ex.HttpRequestError == HttpRequestError.ConnectionError)
                {
                    _logger.LogCritical("Local API is unavailable");
                    _appLifetime.StopApplication();
                }
            }
        }
        _logger.LogCritical("Credentials are invalid");
        _appLifetime.StopApplication();
        throw new ApplicationException();
    }
    
    private async Task<string> GetAccessTokenAsync()
    {
        lock (_lock)
        {
            if (Token != null)
            {
                return Token;
            }
        }
            
        var newAuthResponse = await AuthenticateAsync();

        lock (_lock)
        {
            Token = newAuthResponse;
        }

        return Token;
    }
    
    private async Task<string> AuthenticateAsync()
    {
        //logger.LogInformation("Fetching a new access token for school {School}...", _untisTenant.SchoolName);
        using var key = Registry.LocalMachine.OpenSubKey(@"Software\ClassInsights");
        var token = key?.GetValue("ApiToken")?.ToString();
        
        if (string.IsNullOrEmpty(token))
            throw new Exception("Token not found");
        
        var response = await _httpClient.PostAsJsonAsync("login/computer", new
        {
            computer_token = token
        });
        response.EnsureSuccessStatusCode();
        
        var authToken = await response.Content.ReadAsStringAsync();
        if (authToken == null || string.IsNullOrEmpty(authToken))
            throw new InvalidOperationException("Authentication failed");
        
        return authToken;
    }
}