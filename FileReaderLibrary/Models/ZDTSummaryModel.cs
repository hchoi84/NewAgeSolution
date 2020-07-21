using System;

namespace FileReaderLibrary.Models
{
  public class ZDTSummaryModel
  {
    public string CallDate { get; set; }
    public int Count { get; set; }
    public int AvgWaitSec { get; set; }
    public int AvgTalkSec { get; set; }
  }
}
