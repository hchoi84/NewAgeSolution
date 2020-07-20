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

    public StringBuilder ConvertToStoreBufferStringBuilder(Dictionary<string, int> skusAndStoreQty, string channelName, bool includeHeader)
    {
      StringBuilder sb = new StringBuilder();

      if (includeHeader)
      {
        sb.AppendLine("SKU,Code,Channel Name,Do not send quantity for this SKU,Include incoming quantity mode,Buffer Quantity Mode,Buffer quantity,Maximum quantity to push mode,Maximum quantity to push,Push constant quantity mode,Push constant quantity");
      }

      foreach (var product in skusAndStoreQty)
      {
        string bufferQuantityMode = product.Value == 0 ? "Off" : "Subtraction";

        sb.AppendLine($"{ product.Key },,{ channelName },Off,Off,{ bufferQuantityMode },{ product.Value },Off,20000,Off,0");
      }

      return sb;
    }

    public StringBuilder GenerateStringBuilder(bool includeHeader, string header, List<string> lines)
    {
      StringBuilder sb = new StringBuilder();

      if (includeHeader) sb.AppendLine(header);

      lines.ForEach(l => sb.AppendLine(l));

      return sb;
    }

    #region ZendeskCallSummary
    public async Task<List<ZendeskTalkCallModel>> ReadZendeskTalkExportFile(IFormFile file)
    {
      List<string> _headerNames = new List<string>()
      {
        "Date/Time", "Agent", "Call Status", "Wait Time", "Minutes"
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

      List<ZendeskTalkCallModel> callHistory = new List<ZendeskTalkCallModel>();

      foreach (string line in lines.Skip(1))
      {
        parser = new TextFieldParser(new StringReader(line));
        parser.SetDelimiters(",");
        string[] rawFields = parser.ReadFields();

        ZendeskTalkCallModel call = new ZendeskTalkCallModel();

        call.DateTime = DateTime.Parse(rawFields[_headerIndexes[0]]).Date;
        call.Category = string.IsNullOrWhiteSpace(rawFields[_headerIndexes[1]]) ? rawFields[_headerIndexes[2]] : rawFields[_headerIndexes[1]];
        call.WaitMin = Int32.Parse(rawFields[_headerIndexes[3]]) / 60;
        call.TalkMin = Int32.Parse(rawFields[_headerIndexes[4]]);

        callHistory.Add(call);
      }

      return callHistory;
    }

    public List<ZendeskTalkCallSummaryModel> SummarizeCallHistory(List<ZendeskTalkCallModel> model)
    {
      List<IGrouping<DateTime, ZendeskTalkCallModel>> groupedByDate = model.GroupBy(c => c.DateTime).ToList();

      List<ZendeskTalkCallSummaryModel> callSummaries = new List<ZendeskTalkCallSummaryModel>();

      foreach (var item in groupedByDate)
      {
        var groupedByCategory = item.GroupBy(i => i.Category).ToList();

        List<ZendeskTalkCallSummaryModel> callSummaryByCategory = new List<ZendeskTalkCallSummaryModel>();

        foreach (var g in groupedByCategory)
        {
          ZendeskTalkCallSummaryModel summaryByCategory = new ZendeskTalkCallSummaryModel
          {
            Date = item.Key.Date,
            Category = g.Key,
            Count = g.Count(),
            AvgWaitMin = g.Average(i => i.WaitMin).ToString("F2"),
            AvgTalkMin = g.Average(i => i.TalkMin).ToString("F2"),
            EndOfDate = false
          };

          callSummaryByCategory.Add(summaryByCategory);
        }
        callSummaryByCategory.OrderBy(c => c.Category).ToList();

        ZendeskTalkCallSummaryModel summaryByDate = new ZendeskTalkCallSummaryModel
        {
          Date = item.Key.Date,
          Category = "Total",
          Count = item.Count(),
          AvgWaitMin = "",
          AvgTalkMin = "",
          EndOfDate = true
        };
        callSummaryByCategory.Add(summaryByDate);

        callSummaries.AddRange(callSummaryByCategory);
      }

      return callSummaries;
    }
    #endregion
  }
}
