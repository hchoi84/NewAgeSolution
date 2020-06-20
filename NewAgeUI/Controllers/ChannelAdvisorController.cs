using Microsoft.AspNetCore.Mvc;
using ChannelAdvisorLibrary;
using ChannelAdvisorLibrary.Models;
using NewAgeUI.Securities;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using NewAgeUI.Models;

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
      //example of creating filter
      //string profileId = ChannelAdvisorSecret.ProfileId[ChannelAdvisorProfileEnum.Main];

      //string filter = $"profileid eq { profileId } and LastSaleDateUtc lt 2016-03-01";

      string filter = $"LastSaleDateUtc lt 2016-03-01";
      List<ProductModel> product = _ca.GetProducts(filter);

      return Json(product);
    }
  }
}
