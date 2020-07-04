﻿using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SkuVaultLibrary
{
  public interface ISkuVault
  {
    Task<JObject> GetDataAsync(string reqUri, StringContent content);
    Task<List<JToken>> GetInventoryByLocationAsync(int pageNumber, int pageSize);
    Dictionary<string, int> RetrieveSkuAndQty(List<JToken> jTokens);
    void ProcessUniqueSkuAndQty(Dictionary<string, int> skuAndQtyFromSV, Dictionary<string, int> skuAndQtyFromFile, Dictionary<string, int> skuAndQtyForImport);
  }
}