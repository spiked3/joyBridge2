// RoboNUC ©2014 Mike Partain
// This file is NOT open source
// 
// RnMaster :: RnMaster :: JoystickCanvas.cs 
// 
// /* ----------------------------------------------------------------------------------- */

#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Threading;
using System.Globalization;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;
using System.Diagnostics;

using rChordata;

#endregion

namespace spiked3
{
    public class JoystickControl : Canvas
    {
        // it is important for the background to have a brush - otherwise mouse events will not get fired
        // found this out the hard way :|

        public static readonly DependencyProperty DrawForegroundProperty =
            DependencyProperty.Register("Foreground", typeof (Brush), typeof (JoystickControl),
                new PropertyMetadata(Brushes.Red));

        Point centerPoint;
        const double degToRadians = Math.PI / 180.0;
        bool draggingStarted;
        Pen drawPen = new Pen(Brushes.Black, 1.0);
        Point mouseDraggedLocation;
        DispatcherTimer t = new DispatcherTimer();

        public JoystickControl()
        {
            ClipToBounds = true;

            // +++ get GamePad

            t.Interval = new TimeSpan(0, 0, 0, 0, 1000 / 20);
            t.Tick += TimerTick;
            t.Start();
        }

        public Brush Foreground
        {
            get { return (Brush) GetValue(DrawForegroundProperty); }
            set { SetValue(DrawForegroundProperty, value); }
        }

        public DiamondPoint DiamondPoint
        {
            get { return (DiamondPoint)GetValue(DiamondPointProperty); }
            set { SetValue(DiamondPointProperty, value); }
        }
        public static readonly DependencyProperty DiamondPointProperty =
            DependencyProperty.Register("DiamondPoint", typeof(DiamondPoint), typeof(JoystickControl));

        void TimerTick(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        public delegate void JoystickMovedEventHandler(DiamondPoint p);
        public event JoystickMovedEventHandler JoystickMovedListeners;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (draggingStarted)
            {
                mouseDraggedLocation = e.GetPosition(this);
                var p = new DoublePoint(
                    map((int) mouseDraggedLocation.X, 0, (int) ActualWidth, 100, -100),
                    map((int) mouseDraggedLocation.Y, 0, (int) ActualHeight, 100, -100)
                    );
                DiamondPoint = DiamondToolbox.CartesianToDiamond(p, 100);

                if (JoystickMovedListeners != null)
                    JoystickMovedListeners(DiamondPoint);

                e.Handled = true;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            draggingStarted = true;
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            draggingStarted = false;
            mouseDraggedLocation = centerPoint;
            DiamondPoint = DiamondToolbox.CartesianToDiamond(new DoublePoint(0, 0), 100);
            if (JoystickMovedListeners != null)
                JoystickMovedListeners(DiamondPoint);
            e.Handled = true;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            centerPoint = new Point(ActualWidth / 2, ActualHeight / 2);
            mouseDraggedLocation = centerPoint;
        }

        protected override void OnRender(DrawingContext dc)
        {
            //base.OnRender(dc);
            dc.DrawRectangle(Background, null, new Rect(0, 0, ActualWidth, ActualHeight)); // erase bg

            // red dot at mouse drag location
            dc.DrawEllipse(Foreground, null, mouseDraggedLocation, 10, 10);

            double rd = ActualHeight * .9 / 2;

            // circle
            dc.DrawEllipse(null, drawPen, centerPoint, rd, rd);
            // diamond
            for (int a = 0; a < 360; a += 90)
            {
                Point sp = new Point(Math.Cos(a * degToRadians) * rd + centerPoint.X, Math.Sin(a * degToRadians) * rd + centerPoint.Y);
                Point ep = new Point(Math.Cos((a + 90) * degToRadians) * -rd + centerPoint.X,
                    Math.Sin((a + 90) * degToRadians) * -rd + centerPoint.Y);
                dc.DrawLine(drawPen, sp, ep);
            }
        }

        int map(int x, int in_min, int in_max, int out_min, int out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
        double map(double x, double in_min, double in_max, double out_min, double out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
    }
}
