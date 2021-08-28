using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkuVaultLibrary
{
  public interface ISkuVault
  {
    //Task<Dictionary<string, int>> GetProductsToUpdate(Dictionary<string, int> activeBufferProducts);

    Task UpdateDropShip(Dictionary<string, int> skuAndNewQty);
  }
}
