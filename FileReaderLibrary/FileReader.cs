using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileReaderLibrary
{
  public class FileReader : IFileReader
  {
    public async Task<List<string>> GetSkusFromFileAsync(IFormFile file)
    {
      string sku = "SKU";

      Dictionary<string, int> headerIndex = new Dictionary<string, int>
      { { sku, 0 } };

      List<string> lines = new List<string>();
      Stream stream = file.OpenReadStream();
      List<string> result = new List<string>();

      using (var reader = new StreamReader(stream))
      {
        while (reader.Peek() >= 0) lines.Add(await reader.ReadLineAsync());
      }

      TextFieldParser parser = new TextFieldParser(new StringReader(lines[0]));
      parser.SetDelimiters(",");
      List<string> headers = parser.ReadFields().ToList();

      headerIndex[sku] = headers.IndexOf(sku);

      foreach (string line in lines.Skip(1))
      {
        parser = new TextFieldParser(new StringReader(line));
        parser.SetDelimiters(",");
        string[] rawFields = parser.ReadFields();

        result.Add(rawFields[headerIndex[sku]]);
      }

      return result;
    }

    public StringBuilder ConvertToStoreBufferStringBuilder(Dictionary<string, int> skusAndStoreQty)
    {
      StringBuilder sb = new StringBuilder();

      sb.AppendLine("SKU,Code,Channel Name,Do not send quantity for this SKU,Include incoming quantity mode,Buffer Quantity Mode,Buffer quantity,Maximum quantity to push mode,Maximum quantity to push,Push constant quantity mode,Push constant quantity,Check marketplace quantity,Delay interval,Maximum consecutive delays");

      foreach (var product in skusAndStoreQty)
      {
        string bufferQuantityMode = product.Value == 0 ? "Off" : "Subtract";

        sb.AppendLine($"{ product.Key },,CA Golfio,Off,Off,Off,{ bufferQuantityMode },{ product.Value },Off,20000,Off,0,Off,30,1");
      }

      return sb;
    }
  }
}
