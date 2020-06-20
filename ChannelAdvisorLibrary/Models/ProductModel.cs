using System;
using System.Collections.Generic;
using System.Text;

namespace ChannelAdvisorLibrary.Models
{
  public class ProductModel
  {
    public int ID { get; set; }
    public int ProfileID { get; set; }
    public DateTime CreateDateUtc { get; set; }
    public DateTime UpdateDateUtc { get; set; }
    public DateTime QuantityUpdateDateUtc { get; set; }
    public bool IsAvailableInStore { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsExternalQuantityBlocked { get; set; }
    public DateTime? LastSaleDateUtc { get; set; }
    public string ASIN { get; set; }
    public string Brand { get; set; }
    public string Condition { get; set; }
    public string Manufacturer { get; set; }
    public string MPN { get; set; }
    public string Sku { get; set; }
    public string Title { get; set; }
    public string UPC { get; set; }
    public string WarehouseLocation { get; set; }
    public float Height { get; set; }
    public float Length { get; set; }
    public float Width { get; set; }
    public float Weight { get; set; }
    public float Cost { get; set; }
    public float RetailPrice { get; set; }
    public float BuyItNowPrice { get; set; }
    public string Classification { get; set; }
    public bool IsDisplayInStore { get; set; }
    public string StoreTitle { get; set; }
    public string BundleType { get; set; }
    public string ProductType { get; set; }
    public int TotalAvailableQuantity { get; set; }
    public int OpenAllocatedQuantity { get; set; }
    public int OpenAllocatedQuantityPooled { get; set; }
    public int PendingCheckoutQuantity { get; set; }
    public int PendingCheckoutQuantityPooled { get; set; }
    public int PendingPaymentQuantity { get; set; }
    public int PendingPaymentQuantityPooled { get; set; }
    public int PendingShipmentQuantity { get; set; }
    public int PendingShipmentQuantityPooled { get; set; }
    public int TotalQuantity { get; set; }
    public int TotalQuantityPooled { get; set; }
    public int? QuantitySoldLast7Days { get; set; }
    public int? QuantitySoldLast14Days { get; set; }
    public int? QuantitySoldLast30Days { get; set; }
    public int? QuantitySoldLast60Days { get; set; }
    public int? QuantitySoldLast90Days { get; set; }
    public bool IsParent { get; set; }
    public bool IsInRelationship { get; set; }
    public int? ParentProductID { get; set; }
    public string ParentSku { get; set; }
    public string RelationshipName { get; set; }
    public List<AttributeModel> Attributes { get; set; } = new List<AttributeModel>();
    public List<LabelModel> Labels { get; set; } = new List<LabelModel>();
    public List<DcQuantityModel> DCQuantities { get; set; } = new List<DcQuantityModel>();
  }
}
