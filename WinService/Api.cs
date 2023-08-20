using System.Text;
using Newtonsoft.Json;
using WinService.Models;

namespace WinService;

public class Api
{
    private readonly WinService _winService;

    public Api(WinService winService)
    {
        _winService = winService;
    }

    public async Task<ApiModels.Room> GetRoomAsync(string name)
    {
        var response = await SendRequestAsync("Room", query: $"roomName={name}", requestMethod: RequestMethod.Get);
        return JsonConvert.DeserializeObject<ApiModels.Room>(response);
    }

    public async Task<List<ApiModels.Lesson>> GetLessonsAsync(int room)
    {
        var response = await SendRequestAsync("Lessons", query: $"roomId={room}", requestMethod: RequestMethod.Get);
        return JsonConvert.DeserializeObject<List<ApiModels.Lesson>>(response) ?? new List<ApiModels.Lesson>();
    }

    public async Task<ApiModels.Computer> UpdateComputer(ApiModels.Computer request)
    {
        var response = await SendRequestAsync("Computer", JsonConvert.SerializeObject(request),
            requestMethod: RequestMethod.Post);
        return JsonConvert.DeserializeObject<ApiModels.Computer>(response);
    }

    private async Task<string> SendRequestAsync(string endpoint, string body = "", string query = "", RequestMethod requestMethod = RequestMethod.Post)
    {
        if (_winService.Configuration["Api:BaseUrl"] is not { } baseUrl)
            throw new Exception("Api:BaseUrl configuration missing!");

        using var client = new HttpClient();
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var url = $"{baseUrl}{endpoint}?{query}";

        var response = requestMethod switch
        {
            RequestMethod.Get => await client.GetAsync(url),
            RequestMethod.Post => await client.PostAsync(url, content),
            _ => throw new ArgumentOutOfRangeException(nameof(requestMethod), requestMethod, null)
        };

        return await response.Content.ReadAsStringAsync();
    }

    public enum RequestMethod
    {
        Get = 1,
        Post = 2
    }
}