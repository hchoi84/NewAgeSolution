﻿using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SkuVaultLibrary
{
  public interface ISkuVault
  {
    Task<JObject> GetDataAsync(string reqUri, StringContent content);
  }
}
