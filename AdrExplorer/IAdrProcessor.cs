using System.Threading.Tasks;

namespace AdrExplorer
{
    interface IAdrProcessor
    {
        Task<bool> ProcessFile(byte[] data);
    }
}
