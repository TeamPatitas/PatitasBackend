using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PatitasAPI.Core.Interfaces;

namespace PatitasAPI.Infraestructure.Email;

public class ResendEmailService(HttpClient httpClient) : IEmailService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _apiKey = PatitasEnv.GetEnvVariable("RESEND_API_KEY")!;

    public async Task SendEmailAsync(string email, string subject, string htmlBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var payload = new
        {
            from = "Patitas al Rescate <no-reply@patitasalrescate.galaxym4.dev>", 
            email = new[] { email },
            subject = subject,
            html = htmlBody
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error enviando correo con Resend: {error}");
        }
    }
}