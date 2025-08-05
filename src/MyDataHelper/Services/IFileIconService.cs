using System.Drawing;
using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public interface IFileIconService
    {
        Task<string> GetIconBase64Async(string extension);
        Icon? GetSystemIcon(string filePath, bool largeIcon = false);
        string GetIconClass(string extension);
        string GetDefaultIconBase64();
    }
}