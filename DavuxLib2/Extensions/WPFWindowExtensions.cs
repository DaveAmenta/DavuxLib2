using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Animation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;

namespace DavuxLib2.Extensions
{
    public static class WPFWindowExtensions
    {
        public static void Try(this object o, Action a)
        {
            try
            {
                a.Invoke();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("*** General Failure: " + ex);
            }
        }

        public static void Invoke(this Window w, Action a)
        {
            try
            {
                w.Dispatcher.Invoke(DispatcherPriority.Input, (ThreadStart)(() =>
                {
                    try
                    {
                        a.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("*** Error Invoking Action: " + ex);
                    }
                }));
            }
            catch (Exception ex)
            {
                Trace.WriteLine("*** Error Invoking Action (Dispatcher): " + ex);
            }
        }

        public static void InvokeDelay(this Window w, int Delayms, Action a)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Thread.Sleep(Delayms);
                w.Invoke(a);
            });
        }

        public static double GetDPI(this Window w)
        {
            return PresentationSource.FromVisual(Application.Current.MainWindow)
                .CompositionTarget.TransformToDevice.M11;
        }

        public static void FadeToOpaque(this Window w, string name)
        {
            Storyboard storyboard = new Storyboard();
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, 200);
            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 0.0;
            animation.To = 1.0;
            animation.Duration = new Duration(duration);
            Storyboard.SetTargetName(animation, name);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Control.OpacityProperty));
            storyboard.Children.Add(animation);
            storyboard.Begin(w);
        }

        public static void FadeToTransparent(this Window w, string name)
        {
            Storyboard storyboard = new Storyboard();
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, 200);
            DoubleAnimation animation = new DoubleAnimation();
            animation.From = 1.0;
            animation.To = 0.0;
            animation.Duration = new Duration(duration);
            Storyboard.SetTargetName(animation, name);
            Storyboard.SetTargetProperty(animation, new PropertyPath(Control.OpacityProperty));
            storyboard.Children.Add(animation);
            storyboard.Begin(w);
        }
    }
}
