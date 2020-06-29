using Microsoft.AspNetCore.Mvc;
using ChannelAdvisorLibrary;
using ChannelAdvisorLibrary.Models;
using NewAgeUI.Securities;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using NewAgeUI.ViewModels;
using Microsoft.EntityFrameworkCore.Internal;
using System.Threading.Tasks;

namespace NewAgeUI.Controllers
{
  [Route("ChannelAdvisor")]
  [AllowAnonymous]
  public class ChannelAdvisorController : Controller
  {
    private ChannelAdvisor _ca = new ChannelAdvisor();
    
    public ChannelAdvisorController()
    {
      _ca.SetConnection(new CaConnectionModel
      {
        TokenUrl = ChannelAdvisorSecret.tokenUrl,
        ApplicationId = ChannelAdvisorSecret.applicationId,
        SharedSecret = ChannelAdvisorSecret.sharedSecret,
        RefreshToken = ChannelAdvisorSecret.refreshToken,
        TokenExpireBuffer = ChannelAdvisorSecret.tokenExpireBuffer
      });
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpPost("NoSalesReport")]
    public async Task<IActionResult> ProductsByLastSoldDate(DateTime lastSoldDate)
    {
      //TODO: validate lastSoldDate. Ensure month and date is 2 digits and year is 4 digits. Must be in the past. 

      List<Task> tasks = new List<Task>();
      List<ProductModel> products = await GetSiblingsAsync(lastSoldDate);

      List<ProductsByLastSoldDateViewModel> model = new List<ProductsByLastSoldDateViewModel>();

      tasks.Add(Task.Run(() => AddParentInfo(products, model)));
      tasks.Add(Task.Run(() => AddChildInfo(products, model)));

      Task.WaitAll(tasks.ToArray());

      model = model.OrderBy(m => m.SKU).ToList();

      return View(model);
    }

    private async Task<List<ProductModel>> GetSiblingsAsync(DateTime lastSoldDate)
    {
      List<ProductModel> products = new List<ProductModel>();

      string filter = $"LastSaleDateUtc lt { lastSoldDate.ToString("yyyy-MM-dd") }";

      List<string> parentProductIds = new List<string>();

      (await _ca.GetProductsAsync(filter, "", "ParentProductID"))
        .ForEach(p => parentProductIds.Add(p.ParentProductID.ToString()));

      parentProductIds = parentProductIds.Distinct().ToList();
      parentProductIds.RemoveAll(d => d == "");

      while (parentProductIds.Count > 0)
      {
        bool isMoreThan10 = parentProductIds.Count > 10;
        int x = isMoreThan10 ? 10 : parentProductIds.Count;

        List<string> first10 = new List<string>();

        parentProductIds.GetRange(0, x).ForEach(parentId => first10.Add($"ParentProductId eq { parentId }"));
        parentProductIds.RemoveRange(0, x);

        filter = string.Join(" or ", first10);

        products.AddRange(await _ca.GetProductsAsync(filter, "Attributes,Labels,DCQuantities", ""));
      }

      return products;
    }

    private void AddParentInfo(List<ProductModel> products, List<ProductsByLastSoldDateViewModel> model)
    {
      IEnumerable<IGrouping<string, ProductModel>> groupedByParentSku = products.GroupBy(p => p.ParentSku);

      foreach (var g in groupedByParentSku)
      {
        IEnumerable<ProductModel> productsWithMainFBA = g.Where(p => p.ProfileID == ChannelAdvisorSecret.mainProfileId && p.DCQuantities.Exists(dc => dc.DistributionCenterID == -4));

        IEnumerable<ProductModel> productsWithOtherFBA = g.Where(p => p.ProfileID == ChannelAdvisorSecret.otherProfileId && p.DCQuantities.Exists(dc => dc.DistributionCenterID == -4));

        ProductsByLastSoldDateViewModel p = new ProductsByLastSoldDateViewModel()
        {
          SKU = g.Key,
          UPC = "",
          Description = g.First().Attributes.FirstOrDefault(a => a.Name == "Item Name").Value,
          Created = g.First().CreateDateUtc.ToString("yyyy-MM-dd"),
          GLSD = "",
          GBLSD = "",
          WHQTY = g.Sum(p => p.TotalAvailableQuantity),
          GFBA = productsWithMainFBA == null ? 0 : productsWithMainFBA.Sum(p => p.DCQuantities.First(dc => dc.DistributionCenterID == -4).AvailableQuantity),
          GBFBA = productsWithOtherFBA == null ? 0 : productsWithOtherFBA.Sum(p => p.DCQuantities.First(dc => dc.DistributionCenterID == -4).AvailableQuantity),
        };

        model.Add(p);
      }
    }

    private void AddChildInfo(List<ProductModel> products, List<ProductsByLastSoldDateViewModel> model)
    {
      IEnumerable<IGrouping<int, ProductModel>> groupedByProfileId = products.GroupBy(p => p.ProfileID);

      IGrouping<int, ProductModel> group = groupedByProfileId.FirstOrDefault(g => g.Key == ChannelAdvisorSecret.mainProfileId);

      foreach (ProductModel product in group)
      {
        AddToModel(product, model);
      }

      group = groupedByProfileId.FirstOrDefault(g => g.Key == ChannelAdvisorSecret.otherProfileId);

      foreach (ProductModel product in group)
      {
        DcQuantityModel fbaQty = product.DCQuantities.FirstOrDefault(dc => dc.DistributionCenterID == -4);

        ProductsByLastSoldDateViewModel record = model.FirstOrDefault(m => m.SKU == product.Sku);

        if (record != null)
        {
          record.GBLSD = product.LastSaleDateUtc.HasValue ? ((DateTime)product.LastSaleDateUtc).ToString("yyyy-MM-dd") : "NEVER";
          record.GBFBA = fbaQty != null ? fbaQty.AvailableQuantity : 0;
        }
        else
        {
          AddToModel(product, model);
        }
      }
    }

    private void AddToModel(ProductModel product, List<ProductsByLastSoldDateViewModel> model)
    {
      DcQuantityModel fbaQty = product.DCQuantities.FirstOrDefault(dc => dc.DistributionCenterID == -4);
      string itemName = product.Attributes.FirstOrDefault(a => a.Name == "Item Name").Value;

      ProductsByLastSoldDateViewModel p = new ProductsByLastSoldDateViewModel()
      {
        SKU = product.Sku,
        UPC = product.UPC,
        ParentSKU = product.ParentSku,
        Description = product.Attributes.FirstOrDefault(a => a.Name == "All Name").Value.Replace(itemName, "").Trim(),
        Created = product.CreateDateUtc.ToString("yyyy-MM-dd"),
        GLSD = product.LastSaleDateUtc.HasValue ? ((DateTime)product.LastSaleDateUtc).ToString("yyyy-MM-dd") : "NEVER",
        GBLSD = "NEVER",
        WHQTY = product.TotalAvailableQuantity,
        GFBA = fbaQty != null ? fbaQty.AvailableQuantity : 0,
        GBFBA = 0,
      };

      model.Add(p);
    }
  }
}
