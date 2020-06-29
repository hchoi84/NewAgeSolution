using System;
using System.Collections.Generic;
using System.Text;

namespace FileReaderLibrary.Models
{
  public class ProductQuantityBufferModel
  {
    public string Code { get; set; }
    public string SKU { get; set; }
    public string ChannelName { get; set; }
    public string DoNotSendQuantityForThisSKU { get; set; }
    public string IncludeIncomingQuantityMode { get; set; }
    public string BufferQuantityMode { get; set; }
    public string BufferQuantity { get; set; }
    public string MaximumQuantityToPushMode { get; set; }
    public string MaximumQuantityToPush { get; set; }
    public string PushConstantQuantityMode { get; set; }
    public string PushConstantQuantity { get; set; }
    public string CheckMarketplaceQuantityBeforeUpdate { get; set; }
    public string DelayInterval { get; set; }
    public string MaximumConsecutiveDelays { get; set; }
  }
}
