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
    public void EstablishConnection()
    {
      string accessToken = "access_token";
      string expiresIn = "expires_in";

      if (Secrets.TokenExpireDateTime < DateTime.Now || Secrets.TokenExpireDateTime == null)
      {
        string auth = string.Concat(Secrets.ApplicationId, ":", Secrets.SharedSecret);
        byte[] authBytes = Encoding.ASCII.GetBytes(auth);
        string encodedAuth = Convert.ToBase64String(authBytes);
        string authorization = string.Concat("Basic ", encodedAuth);

        HttpRequestMessage request = new HttpRequestMessage
        {
          RequestUri = new Uri(Secrets.TokenUrl),
          Method = HttpMethod.Post,
          Headers = {
            { HttpRequestHeader.Authorization.ToString(), authorization },
            { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
          },
          Content = new StringContent($"grant_type=refresh_token&refresh_token={ Secrets.RefreshToken }", Encoding.UTF8, "application/json"),
        };

        HttpClient client = new HttpClient();
        HttpResponseMessage response = client.SendAsync(request).Result;
        HttpContent content = response.Content;
        string json = content.ReadAsStringAsync().Result;
        JObject result = JObject.Parse(json);
        Secrets.AccessToken = result[accessToken].ToString();
        Secrets.TokenExpireDateTime = DateTime.Now.AddSeconds(Convert.ToDouble(result[expiresIn]) - Secrets.TokenExpireBuffer);
      }
    }

    public async Task<List<JObject>> GetProductsAsync(string filter, string expand, string select)
    {
      EstablishConnection();

      string reqUri = $"https://api.channeladvisor.com/v1/Products?access_token={ Secrets.AccessToken }";

      if (!string.IsNullOrWhiteSpace(filter)) reqUri += $"&$filter={ filter }";
      if (!string.IsNullOrWhiteSpace(expand)) reqUri += $"&$expand={ expand }";
      if (!string.IsNullOrWhiteSpace(select)) reqUri += $"&$select={ select }";

      List<JObject> jObjects = new List<JObject>();

      while (reqUri != null)
      {
        //HttpRequestMessage request = new HttpRequestMessage
        //{
        //  RequestUri = new Uri(reqUri),
        //  Method = HttpMethod.Get,
        //};

        string result;

        using (HttpClient client = new HttpClient())
        {
          HttpResponseMessage response = await client.GetAsync(reqUri);
          HttpContent content = response.Content;
          result = await content.ReadAsStringAsync();
        }
        JObject jObject = JObject.Parse(result);

        reqUri = (string)jObject["@odata.nextLink"];

        foreach (JObject item in jObject["value"]) jObjects.Add(item);
      }

      return jObjects;
    }

    #region Record Keeping
    //private void ConvertToModel(JObject jObject, List<ProductModel> products)
    //{
    //  foreach (var p in (JArray)jObject["value"])
    //  {
    //    ProductModel productModel = p.ToObject<ProductModel>();

    //    if (p["Attributes"] != null)
    //    {
    //      foreach (var attribute in (JArray)p["Attributes"])
    //      {
    //        AttributeModel attributeModel = attribute.ToObject<AttributeModel>();
    //        productModel.Attributes.Add(attributeModel);
    //      }
    //    }

    //    if (p["Labels"] != null)
    //    {
    //      foreach (var label in (JArray)p["Labels"])
    //      {
    //        LabelModel labelModel = label.ToObject<LabelModel>();
    //        productModel.Labels.Add(labelModel);
    //      }
    //    }

    //    if (p["DCQuantities"] != null)
    //    {
    //      foreach (var dcQty in (JArray)p["DCQuantities"])
    //      {
    //        DcQuantityModel dcQuantityModel = dcQty.ToObject<DcQuantityModel>();
    //        productModel.DCQuantities.Add(dcQuantityModel);
    //      } 
    //    }

    //    products.Add(productModel);
    //  }
    //}

    //public List<T> ConvertToListOf<T>(JArray jArray)
    //{
    //  List<T> items = new List<T>();

    //  foreach (var item in jArray)
    //  {
    //    items.Add(item.ToObject<T>());
    //  }

    //  return items;
    //}
    #endregion
  }
}
