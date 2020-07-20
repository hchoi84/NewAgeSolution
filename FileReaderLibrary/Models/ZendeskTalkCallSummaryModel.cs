using System;

namespace FileReaderLibrary.Models
{
  public class ZendeskTalkCallSummaryModel
  {
    public DateTime Date { get; set; }
    public string Category { get; set; }
    public int Count { get; set; }
    public string AvgWaitMin { get; set; }
    public string AvgTalkMin { get; set; }
    public bool EndOfDate { get; set; }
  }
}
