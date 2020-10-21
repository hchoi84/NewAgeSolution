using System;

namespace ChannelAdvisorLibrary.Models
{
  public class NoSalesReportModel
  {
    public string Sku { get; set; }
    public string UPC { get; set; }
    public string ParentSKU { get; set; }
    public DateTime CreateDateUtc { get; set; }
    public int WHQuantity { get; set; }
    public int StoreQty { get; set; }
    public DateTime? LastSaleDateUtc { get; set; }

    //Under DCQuantities
    public int FBAQuantity { get; set; }

    //Under Attributes
    public string ItemName { get; set; }
    public string AllName { get; set; }

    //Under Labels
    public string ProductLabel { get; set; }
  }
}
