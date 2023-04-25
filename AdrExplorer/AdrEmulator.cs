using System;
using System.Threading.Tasks;

namespace AdrExplorer
{
    class AdrEmulator : IAdrProcessor
    {
        public async Task<bool> ProcessFile(byte[] _)
        {
            await Task.Delay(5000);
            return Random.Shared.Next(2) != 0;
        }
    }
}
