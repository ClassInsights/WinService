using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;
using StartService.Models;

namespace StartService;

public class Api
{
    private readonly string _baseUrl;
    private readonly string _certSubject;
    private string? _jwtToken;

    public Api(string baseUrl, string certSubject)
    {
        _baseUrl = baseUrl;
        _certSubject = certSubject;
    }

    public async Task<List<ApiModels.Lesson>?> GetLessonsAsync()
    {
        var response = await SendRequestAsync("lessons", requestMethod: RequestMethod.Get);
        return JsonConvert.DeserializeObject<List<ApiModels.Lesson>>(response);
    }

    public async Task<List<ApiModels.Computer>?> GetComputersAsync(int room)
    {
        var response = await SendRequestAsync($"rooms/{room}", requestMethod: RequestMethod.Get);
        return JsonConvert.DeserializeObject<List<ApiModels.Computer>>(response);
    }

    public async Task Authorize()
    {
        var store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        var certs = store.Certificates.Find(X509FindType.FindBySubjectName, _certSubject, false);
        store.Close();

        if (certs.Count < 1)
            throw new Exception("No certificate found in Store!");
        
        var handler = new HttpClientHandler
        {
            UseDefaultCredentials = true, // send winAuth token
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        };

        handler.ClientCertificates.AddRange(certs); 
        
        _jwtToken = await SendRequestAsync("user/login/pc", requestMethod: RequestMethod.Get, handler: handler);
    }

    private async Task<string> SendRequestAsync(string endpoint, string body = "", string query = "", RequestMethod requestMethod = RequestMethod.Post, HttpClientHandler? handler = null)
    {
        for (var i = 0; i < 3; i++)
        {
            handler ??= new HttpClientHandler();

            using var client = new HttpClient(handler);
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