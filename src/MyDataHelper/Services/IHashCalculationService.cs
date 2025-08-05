using System.Threading;
using System.Threading.Tasks;

namespace MyDataHelper.Services
{
    public interface IHashCalculationService
    {
        Task<string> CalculateSHA256Async(string filePath, CancellationToken cancellationToken = default);
        Task<string> CalculateMD5Async(string filePath, CancellationToken cancellationToken = default);
        Task<string> CalculateQuickHashAsync(string filePath, CancellationToken cancellationToken = default);
        bool IsHashValid(string hash);
    }
}