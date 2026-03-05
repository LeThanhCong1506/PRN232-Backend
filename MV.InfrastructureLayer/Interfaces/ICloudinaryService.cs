using Microsoft.AspNetCore.Http;

namespace MV.InfrastructureLayer.Interfaces;

public interface ICloudinaryService
{
    /// <summary>
    /// Upload ảnh lên Cloudinary
    /// </summary>
    /// <param name="file">File ảnh cần upload</param>
    /// <param name="folder">Thư mục trên Cloudinary (vd: "products")</param>
    /// <returns>Tuple (imageUrl, publicId) - URL đầy đủ và PublicId để xóa sau này</returns>
    Task<(string ImageUrl, string PublicId)> UploadImageAsync(IFormFile file, string folder = "products");

    /// <summary>
    /// Xóa ảnh khỏi Cloudinary bằng publicId
    /// </summary>
    /// <param name="publicId">PublicId của ảnh trên Cloudinary</param>
    /// <returns>True nếu xóa thành công</returns>
    Task<bool> DeleteImageAsync(string publicId);

    /// <summary>
    /// Trích xuất PublicId từ Cloudinary URL
    /// </summary>
    /// <param name="imageUrl">URL đầy đủ của ảnh Cloudinary</param>
    /// <returns>PublicId</returns>
    string ExtractPublicIdFromUrl(string imageUrl);
}
