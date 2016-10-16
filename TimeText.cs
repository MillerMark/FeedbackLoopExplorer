using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FeedbackLoopExplorer
{
  public class TimeText
  {
    public TimeText(string text)
    {
      Text = text;
      Time = DateTime.Now;
    }
    public string Text { get; private set; }
    public DateTime Time { get; private set; }
  }
}
