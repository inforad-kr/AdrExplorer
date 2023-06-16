using System.Threading.Tasks;

namespace AdrExplorer.Services
{
    public class AdrEmulator : IAdrProcessor
    {
        public void LoadFile(int id, byte[] data)
        {
        }

        public async Task ProcessFiles()
        {
            await Task.Delay(5000);
        }

        public bool? GetResult(int id)
        {
            return id % 2 != 0;
        }
    }
}
