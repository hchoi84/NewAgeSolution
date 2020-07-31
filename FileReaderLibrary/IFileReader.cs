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
    StringBuilder ConvertToStoreBufferSB(
      bool includeHeader,
      Dictionary<string, int> productsToUpdate,
      string channelName);
    StringBuilder GenerateSB(bool includeHeader, string header, List<string> lines);
    Task<List<ZDTSummaryModel>> SummarizeAsync(IFormFile file);
  }
}
