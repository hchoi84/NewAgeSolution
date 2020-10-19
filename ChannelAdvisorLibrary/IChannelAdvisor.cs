using ChannelAdvisorLibrary.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChannelAdvisorLibrary
{
  public interface IChannelAdvisor
  {
    Task<IEnumerable<JObject>> GetProductsAsync(string filter, string expand, string select);
    Task<List<NoSalesReportModel>> GetNoSalesReport(DateTime lastSoldDate);

    List<string> GetAcctNames();
    string GetMainAcctName();
    string GetOtherAcctName();
    int GetMainProfileId();
  }
}
