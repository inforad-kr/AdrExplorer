using System.Threading.Tasks;

namespace AdrExplorer.Services
{
    public interface IAdrProcessor
    {
        Task<bool> ProcessFile(byte[] data);
    }
}
