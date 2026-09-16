using Amazon.S3;
using Amazon.S3.Transfer;
using PatitasAPI.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PatitasAPI.Infraestructure.Storage;

public class CloudflareR2Service : IStorageService
{
    private readonly AmazonS3Client _s3Client;
    private readonly string _bucketName;
    private readonly string _publicUrl;

    public CloudflareR2Service()
    {
        var accessKey = PatitasEnv.GetEnvVariable("R2_ACCESS_KEY_ID");
        var secretKey = PatitasEnv.GetEnvVariable("R2_SECRET_ACCESS_KEY");
        var accountId = PatitasEnv.GetEnvVariable("R2_ACCOUNT_ID");
        
        _bucketName = PatitasEnv.GetEnvVariable("R2_BUCKET_NAME");
        _publicUrl = PatitasEnv.GetEnvVariable("R2_PUBLIC_URL").TrimEnd('/');

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string key)
    {
        if (file.Length == 0) throw new ArgumentException("El archivo está vacío.");
        if (file.Length > 10 * 1024 * 1024) throw new ArgumentException("Archivo excede 10MB.");

        using var convertedStream = await ConvertToWebpAsync(file);

        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = convertedStream,
            Key = key,
            BucketName = _bucketName,
            ContentType = "image/webp",
            DisablePayloadSigning = true 
        };

        var transferUtility = new TransferUtility(_s3Client);
        await transferUtility.UploadAsync(uploadRequest);

        return $"{_publicUrl}/{key}";
    }

    public Task<string> UploadPetPhotoAsync(IFormFile file, Guid petId, int photoIndex)
    {
        if (photoIndex < 1 || photoIndex > 3) throw new ArgumentException("PhotoIndex debe ser 1, 2 o 3.");
        var key = $"pets/{petId}/{petId}-{photoIndex}.webp";
        return UploadFileAsync(file, key);
    }

    private static async Task<MemoryStream> ConvertToWebpAsync(IFormFile file)
    {
        using var inputStream = file.OpenReadStream();
        using var image = await SixLabors.ImageSharp.Image.LoadAsync(inputStream);

        // Resize max 1024x1024 manteniendo aspecto
        const int maxSize = 1024;
        if (image.Width > maxSize || image.Height > maxSize)
        {
            var ratio = Math.Min((double)maxSize / image.Width, (double)maxSize / image.Height);
            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(newWidth, newHeight));
        }

        var webpStream = new MemoryStream();
        var encoder = new SixLabors.ImageSharp.Formats.Webp.WebpEncoder
        {
            Quality = 75,
            Method = SixLabors.ImageSharp.Formats.Webp.WebpEncodingMethod.Fastest,
            FileFormat = SixLabors.ImageSharp.Formats.Webp.WebpFileFormatType.Lossy
        };
        await image.SaveAsWebpAsync(webpStream, encoder);
        webpStream.Position = 0;
        return webpStream;
    }
}