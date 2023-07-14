using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WinService.Models;

namespace WinService;

public class Api
{
#if DEBUG
    public const string BaseUrl = "https://localhost:7061/api/";
#else
    public const string BaseUrl = "https://srv-iis.projekt.lokal/api/";
#endif

    public static async Task<DbModels.TabRooms> GetRoomAsync(string name)
    {
        var response = await SendRequestAsync("Room", query: $"roomName={name}", requestMethod: RequestMethod.Get);
        return JsonConvert.DeserializeObject<DbModels.TabRooms>(response) ?? new DbModels.TabRooms();
    }

    public static async Task<List<DbModels.TabLessons>> GetLessonsAsync(int room)
    {
        var response = await SendRequestAsync("Lessons", query: $"roomId={room}", requestMethod: RequestMethod.Get);
        return JsonConvert.DeserializeObject<List<DbModels.TabLessons>>(response) ?? new List<DbModels.TabLessons>();
    }

    public static async Task<DbModels.TabComputers> UpdateComputer(DbModels.TabComputers request)
    {
        var response = await SendRequestAsync("Computer", JsonConvert.SerializeObject(request),
            requestMethod: RequestMethod.Post);
        return JsonConvert.DeserializeObject<DbModels.TabComputers>(response) ?? new DbModels.TabComputers();
    }

    private static async Task<string> SendRequestAsync(string endpoint, string body = "", string query = "", RequestMethod requestMethod = RequestMethod.Post)
    {
        using var client = new HttpClient();
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var url = $"{BaseUrl}{endpoint}?{query}";

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