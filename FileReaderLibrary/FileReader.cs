using FileReaderLibrary.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileReaderLibrary
{
  public class FileReader : IFileReader
  {
    public async Task<Dictionary<string, int>> RetrieveSkuAndQty(IFormFile file)
    {
      string sku = "SKU";
      string bufferQty = "Buffer quantity";

      Dictionary<string, int> headerIndex = new Dictionary<string, int>
      { { sku, 0 }, {bufferQty, 0 } };

      List<string> lines = new List<string>();
      Stream stream = file.OpenReadStream();
      Dictionary<string, int> result = new Dictionary<string, int>();

      using (var reader = new StreamReader(stream))
      {
        while (reader.Peek() >= 0) lines.Add(await reader.ReadLineAsync());
      }

      TextFieldParser parser = new TextFieldParser(new StringReader(lines[0]));
      parser.SetDelimiters(",");
      List<string> headers = parser.ReadFields().ToList();

      headerIndex[sku] = headers.IndexOf(sku);
      headerIndex[bufferQty] = headers.IndexOf(bufferQty);

      foreach (string line in lines.Skip(1))
      {
        parser = new TextFieldParser(new StringReader(line));
        parser.SetDelimiters(",");
        string[] rawFields = parser.ReadFields();

        string lineSku = rawFields[headerIndex[sku]];
        int lineQty = Int32.Parse(rawFields[headerIndex[bufferQty]]);

        result.Add(lineSku, lineQty);
      }

      return result;
    }

    public StringBuilder ConvertToStoreBufferSB(
      bool includeHeader,
      Dictionary<string, int> productsToUpdate,
      string channelName)
    {
     string header = "SKU,Code,Channel Name,Do not send quantity for this SKU,Include incoming quantity mode,Buffer Quantity Mode,Buffer quantity,Maximum quantity to push mode,Maximum quantity to push,Push constant quantity mode,Push constant quantity";

      List<string> lines = new List<string>();
      foreach (var product in productsToUpdate)
      {
        string bufferQuantityMode = product.Value == 0 ? "Off" : "Subtraction";

        lines.Add($"{ product.Key },,{ channelName },Off,Off,{ bufferQuantityMode },{ product.Value },Off,20000,Off,0");
      }

      return GenerateSB(includeHeader, header, lines);
    }

    public StringBuilder GenerateSB(bool includeHeader, string header, List<string> lines)
    {
      StringBuilder sb = new StringBuilder();

      if (includeHeader) sb.AppendLine(header);

      lines.ForEach(l => sb.AppendLine(l));

      return sb;
    }

    #region ZendeskCallSummary
    public async Task<List<ZDTModel>> ReadZendeskTalkExportFile(IFormFile file)
    {
      List<string> _headerNames = new List<string>()
      {
        "Call created at", "Call duration", "Wait time"
      };
      List<int> _headerIndexes = new List<int>();
      List<string> lines = new List<string>();

      using (var reader = new StreamReader(file.OpenReadStream()))
      {
        while (reader.Peek() >= 0) lines.Add(await reader.ReadLineAsync());
      }

      TextFieldParser parser = new TextFieldParser(new StringReader(lines[0]));
      parser.SetDelimiters(",");
      List<string> headers = parser.ReadFields().ToList();
      foreach (string header in _headerNames)
      {
        int index = headers.IndexOf(header);
        _headerIndexes.Add(index);
      }

      List<ZDTModel> callHistory = new List<ZDTModel>();

      foreach (string line in lines.Skip(1))
      {
        parser = new TextFieldParser(new StringReader(line));
        parser.SetDelimiters(",");
        string[] rawFields = parser.ReadFields();

        ZDTModel call = new ZDTModel
        {
          CallDate = rawFields[_headerIndexes[0]].Substring(0, 10),
          TalkSec = int.Parse(rawFields[_headerIndexes[1]]),
          WaitSec = int.Parse(rawFields[_headerIndexes[2]])
        };

        callHistory.Add(call);
      }

      return callHistory;
    }

    public List<ZDTSummaryModel> SummarizeCallHistory(List<ZDTModel> model)
    {
      List<IGrouping<string, ZDTModel>> groupedByDate = model.GroupBy(c => c.CallDate).ToList();

      List<ZDTSummaryModel> callSummaries = new List<ZDTSummaryModel>();

      foreach (var item in groupedByDate)
      {
        callSummaries.Add(new ZDTSummaryModel
        {
          CallDate = item.Key,
          Count = item.Count(),
          AvgWaitSec = (int)item.Average(i => i.WaitSec),
          AvgTalkSec = (int)item.Average(i => i.TalkSec),
        });
      }

      return callSummaries;
    }
    #endregion
  }
}
