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
        string bufferQuantityMode = product.Value == 0 ? "Off" : "Subtract";

        sb.AppendLine($"{ product.Key },,{ channelName },Off,Off,{ bufferQuantityMode },{ product.Value },Off,20000,Off,0");
      }

      return sb;
    }
  }
}
