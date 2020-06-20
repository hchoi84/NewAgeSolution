using Microsoft.AspNetCore.Mvc;
using ChannelAdvisorLibrary;
using ChannelAdvisorLibrary.Models;
using NewAgeUI.Securities;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace NewAgeUI.Controllers
{
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

    public IActionResult Index()
    {
      string filter = "LastSaleDateUtc lt 2016-03-01";
      List<ProductModel> product = _ca.GetProducts(filter);

      return Json(product);
    }
  }
}
