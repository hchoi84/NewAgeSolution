using System;
using System.Collections.Generic;
using System.Text;

namespace ChannelAdvisorLibrary.Models
{
  public class CaConnectionModel
  {
    public string AccessToken { get; set; }
    public DateTime TokenExpireDateTime { get; set; }
    public string TokenUrl { get; set; }
    public string ApplicationId { get; set; }
    public string SharedSecret { get; set; }
    public string RefreshToken { get; set; }
    public int TokenExpireBuffer { get; set; }
  }
}
