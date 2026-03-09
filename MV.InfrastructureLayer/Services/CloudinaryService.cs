using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MV.InfrastructureLayer.Interfaces;

namespace MV.InfrastructureLayer.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary? _cloudinary;
    private readonly bool _isConfigured;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            _isConfigured = false;
            Console.WriteLine("[WARNING] Cloudinary configuration is missing. Image upload/delete will be disabled.");
            return;
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
        _isConfigured = true;
    }

    public async Task<(string ImageUrl, string PublicId)> UploadImageAsync(IFormFile file, string folder = "products")
    {
        if (!_isConfigured || _cloudinary == null)
            throw new InvalidOperationException("Cloudinary is not configured. Please set Cloudinary:CloudName, ApiKey, ApiSecret in appsettings.json");

        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null.");

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            Transformation = new Transformation()
                .Quality("auto")
                .FetchFormat("auto")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            throw new Exception($"Cloudinary upload failed: {uploadResult.Error?.Message ?? "Unknown error"}");

        return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (!_isConfigured || _cloudinary == null)
            return false;

        if (string.IsNullOrEmpty(publicId))
            return false;

        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);

        return result.Result == "ok";
    }

    public string ExtractPublicIdFromUrl(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return string.Empty;

        // Cloudinary URL format: https://res.cloudinary.com/{cloud_name}/image/upload/v{version}/{folder}/{public_id}.{ext}
        // Extract public_id including folder
        try
        {
            var uri = new Uri(imageUrl);
            var segments = uri.AbsolutePath.Split('/');

            // Find index of "upload" segment
            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex < 0) return string.Empty;

            // Skip "upload" and version (v12345...) segments
            var startIndex = uploadIndex + 1;
            if (startIndex < segments.Length && segments[startIndex].StartsWith("v") && long.TryParse(segments[startIndex][1..], out _))
                startIndex++;

            // Join remaining segments as public_id (remove file extension from last segment)
            var publicIdParts = segments[startIndex..];
            if (publicIdParts.Length == 0) return string.Empty;

            // Remove file extension from the last part
            var lastPart = publicIdParts[^1];
            var dotIndex = lastPart.LastIndexOf('.');
            if (dotIndex > 0)
                publicIdParts[^1] = lastPart[..dotIndex];

            return string.Join("/", publicIdParts);
        }
        catch
        {
            return string.Empty;
        }
    }
}
