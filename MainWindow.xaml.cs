using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace FeedbackLoopExplorer
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    static Color backgroundColor1 = Color.FromRgb(0xF5, 0xEB, 0xEB);
    static Color backgroundColor2 = Color.FromRgb(0xEC, 0xEC, 0xF5);
    Color lastBackgroundColor2 = backgroundColor2;
    #if UseBigTarget
    const double mouseHeight = 50d;
    const double mouseWidth = 50d;
    const double mouseHotspotY = 25d;
    const double mouseHotspotX = 25d;
    #else 
    const double mouseHeight = 24d;
    const double mouseWidth = 15d;
    const double mouseHotspotY = 1d;
    const double mouseHotspotX = 1d;
    #endif

    const int INT_ProcessingTime = 50;    // in milliseconds - estimated time to process input and update the screen on this machine.

    double feedbackLoopDelay;
    Queue<TimePoint> queuedMousePositions = new Queue<TimePoint>();
    Queue<TimeText> queuedText = new Queue<TimeText>();
    Timer timer = new Timer(3);

    public MainWindow()
    {
      InitializeComponent();
      timer.Elapsed += timer_Elapsed;
      timer.Start();
    }

    void UpdatePosition(FrameworkElement frameworkElement, Point point)
    {
      Canvas.SetLeft(frameworkElement, point.X - mouseHotspotX);
      Canvas.SetTop(frameworkElement, point.Y - mouseHotspotY);
    }

    void timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      lock (queuedMousePositions)
        while (queuedMousePositions.Count > 0 && DateTime.Now - queuedMousePositions.Peek().Time > TimeSpan.FromMilliseconds(feedbackLoopDelay))
        {
          TimePoint timePoint = queuedMousePositions.Dequeue();
          Dispatcher.BeginInvoke((Action)(() => UpdatePosition(mouseDelayed, timePoint.Point)));
          if (timePoint.IsDown)
            Dispatcher.BeginInvoke((Action)(() => MouseDown(timePoint.Point, timePoint.IsHit)));
        }
      lock (queuedText)
        while (queuedText.Count > 0 && DateTime.Now - queuedText.Peek().Time > TimeSpan.FromMilliseconds(feedbackLoopDelay))
        {
          TimeText timeText = queuedText.Dequeue();
          Dispatcher.BeginInvoke((Action)(() => UpdateText(timeText.Text)));
        }
    }

    Polygon lastPointer;
        
    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      lblDelay.Text = String.Format("{0:.}ms", sliderDelay.Value + INT_ProcessingTime);
      feedbackLoopDelay = sliderDelay.Value;
      int newSliderDelay = (int)Math.Round(sliderDelay.Value) + INT_ProcessingTime;
      if (lastPointer != null)
      {
        lastPointer.Fill = new SolidColorBrush(Color.FromRgb(0xF1, 0xF1, 0xF1));
        lastPointer = null;
      }
      if (newSliderDelay == 50 || newSliderDelay == 150 || newSliderDelay == 250 || newSliderDelay == 500 || newSliderDelay == 750 || newSliderDelay == 1000 || newSliderDelay == 1250 || newSliderDelay == 1500)
      {
        string controlName = String.Format("_{0}Pointer", newSliderDelay);
        lastPointer = this.FindName(controlName) as Polygon;
        if (lastPointer != null)
          lastPointer.Fill = new SolidColorBrush(Colors.Red);
      }
    }

    private void cvsMain_MouseMove(object sender, MouseEventArgs e)
    {
      Point point = e.GetPosition(cvsMain);
      if (point.Y < mouseHotspotY)
        point.Offset(0, mouseHotspotY - point.Y);
      else if (point.Y > cvsMain.ActualHeight - (mouseHeight - mouseHotspotY))
        point.Offset(0, cvsMain.ActualHeight - (mouseHeight - mouseHotspotY) - point.Y);
      UpdatePosition(mouseRealtime, point);
      lock (queuedMousePositions)
        queuedMousePositions.Enqueue(new TimePoint(point));
    }

    void UpdateText(string text)
    {
      txtDelayed.Text = text;
    }

    private void txtRealtime_TextChanged(object sender, TextChangedEventArgs e)
    {
      TimeText timeText = new TimeText(txtRealtime.Text);
      lock (queuedText)
        queuedText.Enqueue(timeText);
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
      Button button = sender as Button;
      if (button == null)
        return;

      string content = (string)button.Content;
      if (string.IsNullOrEmpty(content))
        return;
      int indexOfMS = content.IndexOf("ms");
      if (indexOfMS < 0)
        return;
      string newValueStr = content.Substring(0, indexOfMS);
      int newValue;
      if (int.TryParse(newValueStr, out newValue))
        sliderDelay.Value = newValue - INT_ProcessingTime;
    }

    private void chkShowGhosting_Checked(object sender, RoutedEventArgs e)
    {
      txtRealtime.Foreground = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0));
      mouseRealtime.Opacity = 0.2;
    }

    private void chkShowGhosting_Unchecked(object sender, RoutedEventArgs e)
    {
      txtRealtime.Foreground = txtRealtime.Background;
      mouseRealtime.Opacity = 0.001;
    }

    private void cvsMain_MouseEnter(object sender, MouseEventArgs e)
    {
      Mouse.OverrideCursor = Cursors.None;
			cvsMain.Cursor = Cursors.None;
      this.Cursor = Cursors.None;
    }

    private void cvsMain_MouseLeave(object sender, MouseEventArgs e)
    {
      this.Cursor = Cursors.Arrow;
      Mouse.OverrideCursor = null;
    }

    void RepositionTarget()
    {
      Random random = new Random(DateTime.Now.Millisecond);
      double xPos = random.Next(70, (int)Math.Round(cvsMain.ActualWidth - 70));
      double yPos = random.Next(70, (int)Math.Round(cvsMain.ActualHeight - 70));
      Canvas.SetLeft(TargetBack, xPos);
      Canvas.SetLeft(TargetFront, xPos);
      Canvas.SetTop(TargetBack, yPos);
      Canvas.SetTop(TargetFront, yPos);
    }

    void ChangeBackgroundColor()
    {
      if (lastBackgroundColor2 == backgroundColor2)
      {
        lastBackgroundColor2 = backgroundColor1;
        TargetBack.Fill = new SolidColorBrush(Colors.Blue);
      }
      else
      {
        lastBackgroundColor2 = backgroundColor2;
        TargetBack.Fill = new SolidColorBrush(Colors.DarkRed);
      }
      Background = new SolidColorBrush(lastBackgroundColor2);
    }

    private void Target_MouseDown(object sender, MouseButtonEventArgs e)
    {
      Point point = e.GetPosition(cvsMain);
      lock (queuedMousePositions)
        queuedMousePositions.Enqueue(new TimePoint(point, true, true));
      e.Handled = true;
    }

    private void cvsMain_MouseDown(object sender, MouseButtonEventArgs e)
    {
      Point point = e.GetPosition(cvsMain);
      lock (queuedMousePositions)
        queuedMousePositions.Enqueue(new TimePoint(point, true, false));
    }

    void MouseHitTarget()
    {
      ChangeBackgroundColor();
      RepositionTarget();
    }
    void AnimateMissBeacon(Point point)
    {
      const int INT_Duration = 650;
      double expansion = 6;
      DoubleAnimation diameterAnimation = new DoubleAnimation();
      diameterAnimation.From = TargetFront.ActualHeight;
      diameterAnimation.To = diameterAnimation.From + expansion;
      diameterAnimation.Duration = TimeSpan.FromMilliseconds(INT_Duration);

      DoubleAnimation opacityAnimation = new DoubleAnimation();
      opacityAnimation.From = 1.0;
      opacityAnimation.To = 0;
      opacityAnimation.Duration = TimeSpan.FromMilliseconds(INT_Duration);

      DoubleAnimation topAnimation = new DoubleAnimation();
      topAnimation.From = point.Y - TargetFront.ActualHeight / 2;
      topAnimation.To = topAnimation.From - expansion / 2;
      topAnimation.Duration = TimeSpan.FromMilliseconds(INT_Duration);

      DoubleAnimation leftAnimation = new DoubleAnimation();
      leftAnimation.From = point.X - TargetFront.ActualWidth / 2;
      leftAnimation.To = leftAnimation.From - expansion / 2;
      leftAnimation.Duration = TimeSpan.FromMilliseconds(INT_Duration);

      ellipseMiss.BeginAnimation(Ellipse.WidthProperty, diameterAnimation);
      ellipseMiss.BeginAnimation(Ellipse.HeightProperty, diameterAnimation);
      ellipseMiss.BeginAnimation(Canvas.LeftProperty, leftAnimation);
      ellipseMiss.BeginAnimation(Canvas.TopProperty, topAnimation);
      ellipseMiss.BeginAnimation(Ellipse.OpacityProperty, opacityAnimation);
    }

    void AnimateMissedText(Point point)
    {
      const int INT_Duration = 800;

      double missTextDirectionX;
      double missTextDirectionY;

      double missTextDeltaX = 20;
      double missTextDeltaY = 10;

      double startX;
      
      double startY;

      if (point.X < cvsMain.ActualWidth / 2)
      {
        missTextDirectionX = 1;
        startX = point.X + missTextDeltaX * missTextDirectionX;
      }
      else
      {
        missTextDirectionX = -1;
        startX = point.X + 2 * missTextDeltaX * missTextDirectionX;
      }

      if (point.Y < cvsMain.ActualHeight / 2)
      {
        missTextDirectionY = 1;
        startY = point.Y + missTextDeltaY * missTextDirectionY;
      }
      else
      {
        missTextDirectionY = -1;
        startY = point.Y + 1.5 * missTextDeltaY * missTextDirectionY;
      }

      DoubleAnimation topAnimation = new DoubleAnimation();
      topAnimation.From = startY;
      topAnimation.To = startY + missTextDeltaY * missTextDirectionY;
      topAnimation.Duration = TimeSpan.FromMilliseconds(INT_Duration);

      DoubleAnimation leftAnimation = new DoubleAnimation();
      leftAnimation.From = startX;
      leftAnimation.To = startX + missTextDeltaX * missTextDirectionX;
      leftAnimation.Duration = TimeSpan.FromMilliseconds(INT_Duration);

      DoubleAnimation fontAnimation = new DoubleAnimation();
      fontAnimation.From = 12;
      fontAnimation.To = 18;
      fontAnimation.Duration = TimeSpan.FromMilliseconds(INT_Duration);

      DoubleAnimation opacityAnimation = new DoubleAnimation();
      opacityAnimation.From = 1.0;
      opacityAnimation.To = 0;
      opacityAnimation.Duration = TimeSpan.FromMilliseconds(INT_Duration);

      textMiss.BeginAnimation(TextBlock.FontSizeProperty, fontAnimation);
      textMiss.BeginAnimation(Canvas.LeftProperty, leftAnimation);
      textMiss.BeginAnimation(Canvas.TopProperty, topAnimation);
      textMiss.BeginAnimation(Ellipse.OpacityProperty, opacityAnimation);
    }

    void MouseMissedTarget(Point point)
    {
      AnimateMissBeacon(point);
      AnimateMissedText(point);
    }

    void MouseDown(Point point, bool isHit)
    {
      if (isHit)
        MouseHitTarget();
      else
        MouseMissedTarget(point);
    }
  }
}

