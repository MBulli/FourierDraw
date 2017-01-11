using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace FourierDraw
{
    public class MirroredInkCanvas : InkCanvas
    {
        private Stroke mirroredStroke;

        private Point Center => new Point(this.ActualWidth / 2, this.ActualHeight / 2);

        private StylusPoint MirrorPoint(Point p1, StylusPoint p2)
        {
            return new StylusPoint(p1.X + (p1.X - p2.X),
                                   p1.Y + (p1.Y - p2.Y),
                                   p2.PressureFactor);
        }

        private StylusPoint MirrorPoint(Point p1, Point p2)
        {
            return new StylusPoint(p1.X + (p1.X - p2.X),
                                   p1.Y + (p1.Y - p2.Y));
        }

        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            base.OnStylusDown(e);

            if (!e.Handled)
            {
                var points = e.GetStylusPoints(this).Select(p => MirrorPoint(Center, p));
                mirroredStroke = new Stroke(new StylusPointCollection(points));
                this.Strokes.Add(mirroredStroke);
            }
        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            base.OnStylusMove(e);

            if (!e.Handled && !e.InAir)
            {
                mirroredStroke.StylusPoints.Add(new StylusPointCollection(e.GetStylusPoints(this).Select(p => MirrorPoint(Center, p))));
            }
        }

        protected override void OnStylusUp(StylusEventArgs e)
        {
            base.OnStylusUp(e);

            if (!e.Handled)
            {
                mirroredStroke.StylusPoints.Add(new StylusPointCollection(e.GetStylusPoints(this).Select(p => MirrorPoint(Center, p))));
                mirroredStroke = null;
            }
        }
        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!e.Handled && e.LeftButton == MouseButtonState.Pressed)
            {
                if (mirroredStroke == null)
                {
                    mirroredStroke = new Stroke(new StylusPointCollection(new Point[] { e.GetPosition(this) }));
                    this.Strokes.Add(mirroredStroke);
                }
                else
                {
                    mirroredStroke.StylusPoints.Add(MirrorPoint(Center, e.GetPosition(this)));
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (!e.Handled && e.LeftButton == MouseButtonState.Pressed)
            {
                mirroredStroke.StylusPoints.Add(MirrorPoint(Center, e.GetPosition(this)));
                mirroredStroke = null;
            }
        }

        
    }
}
