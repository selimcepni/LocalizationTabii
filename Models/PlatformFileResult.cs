using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace LocalizationTabii.Models;

public class PlatformFileResult : FileResult
{
    private readonly string _filePath;

    public PlatformFileResult(string filePath, string fileName) : base(filePath)
    {
        _filePath = filePath;
        FileName = fileName;
    }

    public new Task<Stream> OpenReadAsync()
    {
        return Task.FromResult<Stream>(File.OpenRead(_filePath));
    }
}