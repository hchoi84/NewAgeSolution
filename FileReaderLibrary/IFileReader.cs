using FileReaderLibrary.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FileReaderLibrary
{
  public interface IFileReader
  {
    Task<Dictionary<string, int>> RetrieveSkuAndQty(IFormFile file);
    Task<List<ZDTSummaryModel>> SummarizeAsync(IFormFile file);
    StringBuilder GenerateSB(bool includeHeader, string header, List<string> lines);
    StringBuilder GenerateBufferImportSB(Dictionary<string, int> fromFile, List<JObject> fromCA);
  }
}
