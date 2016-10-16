using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace FeedbackLoopExplorer
{
  public class TimePoint
  {
    public TimePoint(Point point)
      : this(point, false, false)
    {
    }
    public TimePoint(Point point, bool isDown, bool isHit)
    {
      IsHit = isHit;
      IsDown = isDown;
      Time = DateTime.Now;
      Point = point;
    }
    public Point Point { get; private set; }
    public DateTime Time { get; private set; }
    public bool IsDown { get; private set; }
    public bool IsHit { get; private set; }
  }
}
