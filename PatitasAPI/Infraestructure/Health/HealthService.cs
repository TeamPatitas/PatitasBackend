using System.Diagnostics;
using Amazon.S3;
using PatitasAPI.Core.DTOs;
using PatitasAPI.Core.Interfaces;
using PatitasAPI.Infraestructure.Data;

namespace PatitasAPI.Infraestructure.Health;

public class HealthService(PatitasDbContext dbContext, IHttpClientFactory httpClientFactory) : IHealthService
{
    private readonly PatitasDbContext _dbContext = dbContext;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<HealthResponse> CheckHealthAsync()
    {
        var services = new Dictionary<string, HealthServiceResult>
        {
            ["database"] = await CheckDatabaseAsync(),
            ["storage"] = await CheckStorageAsync(),
            ["email"] = await CheckEmailAsync(),
            ["api"] = new HealthServiceResult { Status = "UP", LatencyMs = 0 }
        };

        var overall = services.Values.All(s => s.Status == "UP") ? "UP" : "DOWN";
        return new HealthResponse
        {
            Status = overall,
            Services = services
        };
    }

    private async Task<HealthServiceResult> CheckDatabaseAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            sw.Stop();
            return canConnect
                ? new HealthServiceResult { Status = "UP", LatencyMs = sw.Elapsed.TotalMilliseconds }
                : new HealthServiceResult { Status = "DOWN", LatencyMs = sw.Elapsed.TotalMilliseconds, Error = "Cannot connect to Postgres" };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HealthServiceResult { Status = "DOWN", LatencyMs = sw.Elapsed.TotalMilliseconds, Error = ex.Message };
        }
    }

    private async Task<HealthServiceResult> CheckStorageAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var accountId = PatitasEnv.GetEnvVariable("R2_ACCOUNT_ID");
            var accessKey = PatitasEnv.GetEnvVariable("R2_ACCESS_KEY_ID");
            var secretKey = PatitasEnv.GetEnvVariable("R2_SECRET_ACCESS_KEY");
            var bucket = PatitasEnv.GetEnvVariable("R2_BUCKET_NAME");

            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true
            };
            using var client = new AmazonS3Client(accessKey, secretKey, config);
            // Lightweight check: HeadBucket
            var response = await client.ListObjectsV2Async(new Amazon.S3.Model.ListObjectsV2Request
            {
                BucketName = bucket,
                MaxKeys = 1
            });
            sw.Stop();
            return new HealthServiceResult { Status = "UP", LatencyMs = sw.Elapsed.TotalMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HealthServiceResult { Status = "DOWN", LatencyMs = sw.Elapsed.TotalMilliseconds, Error = ex.Message };
        }
    }

    private async Task<HealthServiceResult> CheckEmailAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var apiKey = PatitasEnv.GetEnvVariable("RESEND_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                return new HealthServiceResult { Status = "DOWN", LatencyMs = sw.Elapsed.TotalMilliseconds, Error = "RESEND_API_KEY missing" };

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.resend.com/domains");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            var resp = await client.SendAsync(request);
            sw.Stop();
            // Resend returns 200 if key valid, 401 if invalid but still reachable
            if (resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return new HealthServiceResult { Status = "UP", LatencyMs = sw.Elapsed.TotalMilliseconds };
            return new HealthServiceResult { Status = "DOWN", LatencyMs = sw.Elapsed.TotalMilliseconds, Error = $"Resend status {(int)resp.StatusCode}" };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HealthServiceResult { Status = "DOWN", LatencyMs = sw.Elapsed.TotalMilliseconds, Error = ex.Message };
        }
    }
}
