using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;

namespace SkuVaultLibrary
{
  public class SkuVault
  {
    public async Task<JObject> GetDataAsync(string reqUri, StringContent content)
    {
      HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      HttpResponseMessage response = await client.PostAsync(reqUri, content);
      HttpContent httpContent = response.Content;
      string result = await httpContent.ReadAsStringAsync();
      JObject jObject = JObject.Parse(result);

      return jObject;
    }
  }
}
