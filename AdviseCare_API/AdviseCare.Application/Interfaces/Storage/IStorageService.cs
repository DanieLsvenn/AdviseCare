namespace AdviseCare.Application.Interfaces.Storage
{
    public interface IStorageService
    {
        // Uploads a file to the avatar bucket at the given path and returns its public URL
        Task<string> UploadAvatarAsync(Stream content, string path, string contentType);
    }
}
