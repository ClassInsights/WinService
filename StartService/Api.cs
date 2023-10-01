using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using StartService.Models;

namespace StartService;

public class Api
{
    private readonly string _baseUrl;
    private string? _jwtToken;

    public Api(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    public async Task<List<ApiModels.Lesson>?> GetLessonsAsync()
    {
        var response = await SendRequestAsync("lessons", requestMethod: RequestMethod.Get);
        return JsonConvert.DeserializeObject<List<ApiModels.Lesson>>(response);
    }

    public async Task<List<ApiModels.Computer>?> GetComputersAsync(int room)
    {
        var response = await SendRequestAsync($"rooms/{room}/computers", requestMethod: RequestMethod.Get);
        return JsonConvert.DeserializeObject<List<ApiModels.Computer>>(response);
    }

    public async Task Authorize()
    {
        _jwtToken = await SendRequestAsync("login/pc", requestMethod: RequestMethod.Get);
    }

    private async Task<string> SendRequestAsync(string endpoint, string body = "", string query = "", RequestMethod requestMethod = RequestMethod.Post)
    {
        using var client = new HttpClient(new HttpClientHandler
        {
            UseDefaultCredentials = true // send winAuth token
        });
        
        for (var i = 0; i < 3; i++)
        {
            if (_jwtToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);

            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}{endpoint}?{query}";

            var response = requestMethod switch
            {
                RequestMethod.Get => await client.GetAsync(url),
                RequestMethod.Post => await client.PostAsync(url, content),
                _ => throw new ArgumentOutOfRangeException(nameof(requestMethod), requestMethod, null)
            };
            // return on success
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return await response.Content.ReadAsStringAsync();

            // authorize again if unauthorized
            await Authorize();
            await Task.Delay(500);
        }

        throw new Exception("No Permissions for this Endpoint!");
    }

    private enum RequestMethod
    {
        Get,
        Post
    }
}