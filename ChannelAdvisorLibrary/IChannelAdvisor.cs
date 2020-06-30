using ChannelAdvisorLibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChannelAdvisorLibrary
{
  public interface IChannelAdvisor
  {
    void SetConnection(CaConnectionModel model);
    Task<List<ProductModel>> GetProductsAsync(string filter, string expand, string select);
  }
}
