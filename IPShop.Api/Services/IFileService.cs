// Services/IFileService.cs
public interface IFileService
{
    Task<string> UploadFileAsync(IFormFile file);
    Task DeleteFileAsync(string fileUrl);
    bool IsValidImage(IFormFile file);
}

// Services/FileService.cs
public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long _maxFileSize = 5 * 1024 * 1024; // 5MB

    public FileService(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        if (!IsValidImage(file))
            throw new ArgumentException("Invalid image file");

        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = $"{request?.Scheme}://{request?.Host}";
        return $"{baseUrl}/uploads/products/{uniqueFileName}";
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl))
            return Task.CompletedTask;

        try
        {
            var uri = new Uri(fileUrl);
            var fileName = Path.GetFileName(uri.LocalPath);
            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "products", fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch
        {
            // Log error if needed
        }

        return Task.CompletedTask;
    }

    public bool IsValidImage(IFormFile file)
    {
        if (file.Length > _maxFileSize)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return _allowedExtensions.Contains(extension);
    }
}