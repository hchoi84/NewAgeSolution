using FileReaderLibrary.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FileReaderLibrary
{
  public class FileReader : IFileReader
  {
    public async Task<Dictionary<string, int>> RetrieveSkuAndQty(IFormFile file)
    {
      string sku = "SKU";
      string bufferQty = "Buffer quantity";

      Stream stream = file.OpenReadStream();
      List<string> lines = new List<string>();
      using (var reader = new StreamReader(stream))
      {
        while (reader.Peek() >= 0) lines.Add(await reader.ReadLineAsync());
      }

      Dictionary<string, int> headerIndex = new Dictionary<string, int>
      {
        { sku, 0 },
        { bufferQty, 0 }
      };
      TextFieldParser parser = new TextFieldParser(new StringReader(lines[0]));
      parser.SetDelimiters(",");
      List<string> headers = parser.ReadFields().ToList();
      headerIndex[sku] = headers.IndexOf(sku);
      headerIndex[bufferQty] = headers.IndexOf(bufferQty);

      Dictionary<string, int> result = new Dictionary<string, int>();
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

    #region BufferSetter
    public StringBuilder GenerateBufferImportSB(Dictionary<string, int> fromFile, List<JObject> fromCA)
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine("SKU,Code,Channel Name,Do not send quantity for this SKU,Include incoming quantity mode,Buffer Quantity Mode,Buffer quantity,Maximum quantity to push mode,Maximum quantity to push,Push constant quantity mode,Push constant quantity");

      foreach (JObject pCA in fromCA)
      {
        KeyValuePair<string, int> pFile = fromFile.FirstOrDefault(x => x.Key == pCA["SKU"].ToString());
        if (pFile.Key == null)
        {
          // add pCA to lines
          string bufferMode = (int)pCA["Qty"] <= 0 ? "Off" : "Subtraction";
          int bufferQty = (int)pCA["Qty"] <= 0 ? 0 : (int)pCA["Qty"];
          sb.AppendLine($"{ pCA["SKU"] },,CA Golfio,Off,Off,{ bufferMode },{ bufferQty },Off,20000,Off,0");
          sb.AppendLine($"{ pCA["SKU"] },,CA GB,Off,Off,{ bufferMode },{ bufferQty },Off,20000,Off,0");
          if (!(bool)pCA["isForWeb"])
          {
            sb.AppendLine($"{ pCA["SKU"] },,BC Golfio,Off,Off,{ bufferMode },{ bufferQty },Off,20000,Off,0");
          }
        }
        else if (pFile.Value != (int)pCA["Qty"])
        {
          // delete pFile in fromFile
          fromFile.Remove(pFile.Key);
          // add pCA to lines
          string bufferMode = (int)pCA["Qty"] <= 0 ? "Off" : "Subtraction";
          int bufferQty = (int)pCA["Qty"] <= 0 ? 0 : (int)pCA["Qty"];
          sb.AppendLine($"{ pCA["SKU"] },,CA Golfio,Off,Off,{ bufferMode },{ bufferQty },Off,20000,Off,0");
          sb.AppendLine($"{ pCA["SKU"] },,CA GB,Off,Off,{ bufferMode },{ bufferQty },Off,20000,Off,0");
          if (!(bool)pCA["isForWeb"])
          {
            sb.AppendLine($"{ pCA["SKU"] },,BC Golfio,Off,Off,{ bufferMode },{ bufferQty },Off,20000,Off,0");
          }
        }
        else
        {
          // delete pFile in fromFile
          fromFile.Remove(pFile.Key);
        }
      }

      foreach (var pFile in fromFile)
      {
        sb.AppendLine($"{ pFile.Key },,CA Golfio,Off,Off,Off,0,Off,20000,Off,0");
        sb.AppendLine($"{ pFile.Key },,CA GB,Off,Off,Off,0,Off,20000,Off,0");
        sb.AppendLine($"{ pFile.Key },,BC Golfio,Off,Off,Off,0,Off,20000,Off,0");
      }

      //foreach (var product in productsToUpdate)
      //{
      //  string bufferQuantityMode = product.Value == 0 ? "Off" : "Subtraction";

      //  lines.Add($"{ product.Key },,{ channelName },Off,Off,{ bufferQuantityMode },{ product.Value },Off,20000,Off,0");
      //}

      return sb;
    }
    #endregion

    #region ZDTSummarizer
    public async Task<List<ZDTSummaryModel>> SummarizeAsync(IFormFile file)
    {
      List<ZDTModel> callHistory = await ReadZDTExportFile(file);

      List<IGrouping<string, ZDTModel>> groupedByDate = callHistory.GroupBy(c => c.CallDate).ToList();

      List<ZDTSummaryModel> summary = new List<ZDTSummaryModel>();

      foreach (var item in groupedByDate)
      {
        summary.Add(new ZDTSummaryModel
        {
          CallDate = item.Key,
          Count = item.Count(),
          AvgWaitSec = (int)item.Average(i => i.WaitSec),
          AvgTalkSec = (int)item.Average(i => i.TalkSec),
        });
      }

      return summary;
    }

    private async Task<List<ZDTModel>> ReadZDTExportFile(IFormFile file)
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
    #endregion

    public StringBuilder GenerateSB(bool includeHeader, string header, List<string> lines)
    {
      StringBuilder sb = new StringBuilder();

      if (includeHeader) sb.AppendLine(header);

      lines.ForEach(l => sb.AppendLine(l));

      return sb;
    }
  }
}
