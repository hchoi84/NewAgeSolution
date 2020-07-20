using System;

namespace FileReaderLibrary.Models
{
  public class ZendeskTalkCallModel
  {
    public DateTime DateTime { get; set; }
    public string Category { get; set; }
    public int WaitMin { get; set; }
    public int TalkMin { get; set; }
  }
}
