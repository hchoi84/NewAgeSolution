using ChannelAdvisorLibrary.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ChannelAdvisorLibrary
{
  public class ChannelAdvisor : IChannelAdvisor
  {
    CaConnectionModel ca = new CaConnectionModel();

    public void SetConnection(CaConnectionModel model)
    {
      string accessToken = "access_token";
      string expiresIn = "expires_in";
      ca = model;

      if (ca.TokenExpireDateTime < DateTime.Now || ca.TokenExpireDateTime == null)
      {
        RequestNewAccessToken(accessToken, expiresIn, ca);
      }
    }

    private void RequestNewAccessToken(string accessToken, string expiresIn, CaConnectionModel ca)
    {
      string auth = string.Concat(ca.ApplicationId, ":", ca.SharedSecret);
      byte[] authBytes = Encoding.ASCII.GetBytes(auth);
      string encodedAuth = Convert.ToBase64String(authBytes);
      string authorization = string.Concat("Basic ", encodedAuth);

      HttpRequestMessage request = new HttpRequestMessage
      {
        RequestUri = new Uri(ca.TokenUrl),
        Method = HttpMethod.Post,
        Headers = {
            { HttpRequestHeader.Authorization.ToString(), authorization },
            { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
          },
        Content = new StringContent($"grant_type=refresh_token&refresh_token={ca.RefreshToken}", Encoding.UTF8, "application/json"),
      };

      HttpClient client = new HttpClient();
      HttpResponseMessage response = client.SendAsync(request).Result;
      HttpContent content = response.Content;
      string json = content.ReadAsStringAsync().Result;
      JObject result = JObject.Parse(json);
      ca.AccessToken = result[accessToken].ToString();
      ca.TokenExpireDateTime = DateTime.Now.AddSeconds(Convert.ToDouble(result[expiresIn]) - ca.TokenExpireBuffer);
    }

    public async Task<List<ProductModel>> GetProductsAsync(string filter, string expand, string select)
    {
      string reqUri = $"https://api.channeladvisor.com/v1/Products?access_token={ ca.AccessToken }";

      if (!string.IsNullOrWhiteSpace(filter)) reqUri += $"&$filter={ filter }";
      // Attributes,Labels,DCQuantities
      if (!string.IsNullOrWhiteSpace(expand)) reqUri += $"&$expand={ expand }";
      if (!string.IsNullOrWhiteSpace(select)) reqUri += $"&$select={ select }";
      
      List<ProductModel> products = new List<ProductModel>();

      while (reqUri != null)
      {
        HttpRequestMessage request = new HttpRequestMessage
        {
          RequestUri = new Uri(reqUri),
          Method = HttpMethod.Get,
        };

        HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.SendAsync(request);
        HttpContent content = response.Content;
        string result = await content.ReadAsStringAsync();
        JObject jObject = JObject.Parse(result);
        
        reqUri = (string)jObject["@odata.nextLink"];

        await Task.Run(() => ConvertToModel(jObject, products));
      }

      return products;
    }

    private void ConvertToModel(JObject jObject, List<ProductModel> products)
    {
      foreach (var p in (JArray)jObject["value"])
      {
        ProductModel productModel = p.ToObject<ProductModel>();

        if (p["Attributes"] != null)
        {
          foreach (var attribute in (JArray)p["Attributes"])
          {
            AttributeModel attributeModel = attribute.ToObject<AttributeModel>();
            productModel.Attributes.Add(attributeModel);
          }
        }

        if (p["Labels"] != null)
        {
          foreach (var label in (JArray)p["Labels"])
          {
            LabelModel labelModel = label.ToObject<LabelModel>();
            productModel.Labels.Add(labelModel);
          }
        }

        if (p["DCQuantities"] != null)
        {
          foreach (var dcQty in (JArray)p["DCQuantities"])
          {
            DcQuantityModel dcQuantityModel = dcQty.ToObject<DcQuantityModel>();
            productModel.DCQuantities.Add(dcQuantityModel);
          } 
        }

        products.Add(productModel);
      }
    }
  }
}
