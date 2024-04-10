﻿using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;
using WinService.Models;

namespace WinService;

public class Api(WinService winService)
{
    public string? JwtToken;

    public async Task<ApiModels.Room?> GetRoomAsync(string name)
    {
        var response = await SendRequestAsync($"rooms/{name}", requestMethod: RequestMethod.Get);
        return JsonConvert.DeserializeObject<ApiModels.Room>(response);
    }

    public async Task<ApiModels.Computer?> GetComputerAsync(string name)
    {
        var response = await SendRequestAsync($"computers/{name}", requestMethod: RequestMethod.Get);
        return JsonConvert.DeserializeObject<ApiModels.Computer>(response);
    }

    public async Task<List<ApiModels.Lesson>> GetLessonsAsync(int room)
    {
        var response = await SendRequestAsync($"rooms/{room}/lessons", requestMethod: RequestMethod.Get);
        return JsonConvert.DeserializeObject<List<ApiModels.Lesson>>(response) ?? new List<ApiModels.Lesson>();
    }

    public async Task<ApiModels.Computer?> UpdateComputer(ApiModels.Computer request)
    {
        var settings = new JsonSerializerSettings{DateFormatString ="yyyy-MM-ddTHH:mm:ss.fffZ"};
        var response = await SendRequestAsync("computers", JsonConvert.SerializeObject(request, settings),
            requestMethod: RequestMethod.Post);
        return JsonConvert.DeserializeObject<ApiModels.Computer>(response);
    }

    public async Task Authorize()
    {
        if (winService.Configuration["Api:CertificateSubject"] is not { } certSubject)
            throw new Exception("Api:CertificateSubject configuration missing!");

        var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        var certs = store.Certificates.Find(X509FindType.FindBySubjectName, certSubject, false);
        store.Close();

        if (certs.Count < 1)
            throw new Exception("No certificate found in Store!");

        var handler = new HttpClientHandler
        {
            UseDefaultCredentials = true, // send winAuth token
            ClientCertificateOptions = ClientCertificateOption.Manual,
#if DEBUG
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
#endif
        };

        handler.ClientCertificates.AddRange(certs);

        JwtToken = await SendRequestAsync("login/pc", requestMethod: RequestMethod.Get, handler: handler);
    }

    private async Task<string> SendRequestAsync(string endpoint, string body = "", string query = "",
        RequestMethod requestMethod = RequestMethod.Post, HttpClientHandler? handler = null)
    {
        // try 3 times to access endpoint
        for (var i = 0; i < 3; i++)
        {
            if (winService.Configuration["Api:BaseUrl"] is not { } baseUrl)
                throw new Exception("Api:BaseUrl configuration missing!");

            handler ??= new HttpClientHandler();
            using var client = new HttpClient(handler);

            if (JwtToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtToken);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(query)) query = $"?{query}";
            var url = $"{baseUrl}{endpoint}{query}";

            var response = requestMethod switch
            {
                RequestMethod.Get => await client.GetAsync(url),
                RequestMethod.Post => await client.PostAsync(url, content),
                _ => throw new ArgumentOutOfRangeException(nameof(requestMethod), requestMethod, null)
            };

            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return await response.Content.ReadAsStringAsync();

            // if status code 401, authorize again
            await Authorize();
            await Task.Delay(500);
        }

        throw new Exception("No Permissions for this Endpoint!");
    }

    private enum RequestMethod
    {
        Get = 1,
        Post = 2
    }
}