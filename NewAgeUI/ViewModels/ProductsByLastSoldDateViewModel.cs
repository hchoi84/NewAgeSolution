using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewAgeUI.ViewModels
{
  public class ProductsByLastSoldDateViewModel
  {
    public string SKU { get; set; }
    public string UPC { get; set; }
    public string ParentSKU { get; set; }
    public string Description { get; set; }
    public string Created { get; set; }
    public string GLSD { get; set; }
    public string GBLSD { get; set; }
    public int WHQTY { get; set; }
    public int GFBA { get; set; }
    public int GBFBA { get; set; }
  }
}
