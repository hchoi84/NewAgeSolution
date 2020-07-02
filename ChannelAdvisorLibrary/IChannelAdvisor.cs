using ChannelAdvisorLibrary.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChannelAdvisorLibrary
{
  public interface IChannelAdvisor
  {
    void EstablishConnection();
    Task<List<JObject>> GetProductsAsync(string filter, string expand, string select);
    Task<List<string>> GetDistinctParentIdsAsync(string filter, string expand, string select);
    Task<List<JObject>> GetChildrenPerParentIdAsync(List<string> distinctParentIds);
    List<NoSalesReportModel> ConvertToNoSalesReportModel(List<JObject> jObjects);
    List<NoSalesReportModel> AddParentInfo(List<NoSalesReportModel> model); 
  }
}
