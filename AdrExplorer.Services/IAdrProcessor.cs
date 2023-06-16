using System.Threading.Tasks;

namespace AdrExplorer.Services
{
    public interface IAdrProcessor
    {
        void LoadFile(int id, byte[] data);

        Task ProcessFiles();

        bool? GetResult(int id);
    }
}
