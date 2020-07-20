using FileReaderLibrary.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FileReaderLibrary
{
  public interface IFileReader
  {
    Task<Dictionary<string, int>> RetrieveSkuAndQty(IFormFile file);
    StringBuilder ConvertToStoreBufferStringBuilder(Dictionary<string, int> skusAndStoreQty, string channelName, bool includeHeader);
    StringBuilder GenerateStringBuilder(bool includeHeader, string header, List<string> lines);
    Task<List<ZDTModel>> ReadZendeskTalkExportFile(IFormFile file);
    List<ZDTSummaryModel> SummarizeCallHistory(List<ZDTModel> model);
  }
}
