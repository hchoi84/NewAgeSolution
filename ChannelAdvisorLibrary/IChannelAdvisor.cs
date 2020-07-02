using ChannelAdvisorLibrary.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChannelAdvisorLibrary
{
  public interface IChannelAdvisor
  {
    void SetConnection(CaConnectionModel model);
    Task<List<JObject>> GetProductsAsync(string filter, string expand, string select);
  }
}
