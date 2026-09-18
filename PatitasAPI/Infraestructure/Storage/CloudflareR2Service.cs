using Amazon.S3;
using Amazon.S3.Transfer;
using PatitasAPI.Core.Interfaces;
using SkiaSharp;

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
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var originalBitmap = SKBitmap.Decode(memoryStream);
        SKBitmap bitmapToEncode = originalBitmap;

        const int maxSize = 1024;
        if (originalBitmap.Width > maxSize || originalBitmap.Height > maxSize)
        {
            var ratio = Math.Min((double)maxSize / originalBitmap.Width, (double)maxSize / originalBitmap.Height);
            var newWidth = (int)(originalBitmap.Width * ratio);
            var newHeight = (int)(originalBitmap.Height * ratio);

            var newImageInfo = new SKImageInfo(newWidth, newHeight);
            bitmapToEncode = originalBitmap.Resize(newImageInfo, new SKSamplingOptions(SKFilterMode.Linear));
        }
        
        using var image = SKImage.FromBitmap(bitmapToEncode);
        using var data = image.Encode(SKEncodedImageFormat.Webp, 75);

        var webpStream = new MemoryStream();
        data.SaveTo(webpStream);
        webpStream.Position = 0;

        if (bitmapToEncode != originalBitmap)
        {
            bitmapToEncode.Dispose();
        }

        return webpStream;
    }
}