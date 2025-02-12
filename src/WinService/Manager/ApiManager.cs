using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
    public ApiModels.Room Room { get; }
    public ApiModels.Computer? Computer {get; set; }

    public ApiManager(ILogger<ApiManager> logger)
    {
        _logger = logger;
        
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ClassInsights");
        ApiUrl = key?.GetValue("ApiUrl")?.ToString();
        if (ApiUrl == null)
            throw new Exception("API url could not be found");
        
        _httpClient.BaseAddress = new Uri($"{ApiUrl}/api/");
        Room = Task.Run(GetRoomAsync).GetAwaiter().GetResult();
        Computer = Task.Run(GetComputerAsync).GetAwaiter().GetResult();
    }

    private async Task<ApiModels.Room> GetRoomAsync()
    {
        if (await GetRoomAsync(Environment.MachineName) is { } room)
        {
            _logger.LogInformation("Room: {roomName} with Id {roomId}", room.Name, room.RoomId);
            return room;
        }
        
        _logger.LogCritical("No room found");
        throw new Exception("No room found");
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
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.Room);
    }

    private async Task<ApiModels.Computer?> GetComputerAsync(string name)
    {
        var response = await CallApiEndpointAsync($"computers/{name}", HttpMethod.Get);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.Computer);
    }

    public async Task<List<ApiModels.Lesson>> GetLessonsAsync(int? room = null)
    {
        var response = await CallApiEndpointAsync($"rooms/{room ?? Room.RoomId}/lessons", HttpMethod.Get);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.ListLesson) ?? [];
    }

    public async Task<ApiModels.Computer?> UpdateComputer(ApiModels.Computer request)
    {
        var response = await CallApiEndpointAsync("computers", HttpMethod.Post, new StringContent(JsonSerializer.Serialize(request, SourceGenerationContext.Default.Computer), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.Computer);
    }

    private async Task<HttpResponseMessage> CallApiEndpointAsync(string endpoint, HttpMethod method, HttpContent? content = null)
    {
        for (var i = 0; i < 3; i++)
        {
            var accessToken = await GetAccessTokenAsync();

            var request = new HttpRequestMessage(method, endpoint) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) return response;
            _logger.LogWarning("Access token expired. Retrying...");
            lock (_lock) Token = null; // Force re-authentication
        }
        throw new HttpRequestException("Credentials invalid");
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
        
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token);
        var response = await _httpClient.GetAsync("login/computer");
        response.EnsureSuccessStatusCode();
        
        var authToken = await response.Content.ReadAsStringAsync();
        if (authToken == null || string.IsNullOrEmpty(authToken))
            throw new InvalidOperationException("Authentication failed");
        
        return authToken;
    }
}