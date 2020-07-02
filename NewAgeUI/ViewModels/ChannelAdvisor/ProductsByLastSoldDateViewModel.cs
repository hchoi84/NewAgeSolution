using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewAgeUI.ViewModels
{
  public class ProductsByLastSoldDateViewModel
  {
    public string Sku { get; set; }
    public string UPC { get; set; }
    public string ParentSKU { get; set; }
    public DateTime CreateDateUtc { get; set; }
    public int TotalAvailableQuantity { get; set; }
    public DateTime? LastSaleDateUtc { get; set; }

    //Under DCQuantities
    public int FBA { get; set; }

    //Under Attributes
    public string ItemName { get; set; }
    public string AllName { get; set; }

    //Under Labels
    public string ProductLabel { get; set; }
  }
}
