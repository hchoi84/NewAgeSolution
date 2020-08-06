using ChannelAdvisorLibrary.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChannelAdvisorLibrary
{
  public interface IChannelAdvisor
  {
    Task<List<JObject>> GetProductsAsync(string filter, string expand, string select);
    Task<List<NoSalesReportModel>> GetNoSalesReport(DateTime lastSoldDate);
    Task<List<UpdateDropShipReportModel>> GetProductsToUpdate();

    List<string> GetAcctNames();
    string GetMainAcctName();
    string GetOtherAcctName();
    int GetMainProfileId();
  }
}
