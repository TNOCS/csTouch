using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;

namespace SimzzDev
{
    public class DrawingBoard
    {
        public enum PenMode { pen, erase };
        Stroke stroke;
        StylusPointCollection spCol;

        public StylusPointCollection ErasePoints;
        public StrokeCollection allErasedStrokes = new StrokeCollection();
        public InkPresenter ink;
        //props
        public PenMode InkMode { get; set; }
        public Color MainColor { get; set; }
        public Color OutlineColor { get; set; }
        public int BrushWidth { get; set; }
        public int BrushHeight { get; set; }
        public delegate void StrokesChanged();
        public event StrokesChanged OnStrokesChanged;


        public DrawingBoard(InkPresenter Ink)
        {
            ink = Ink;
            ink.MouseLeftButtonDown += ink_MouseLeftButtonDown;
            ink.MouseLeftButtonUp += ink_MouseLeftButtonUp;
            ink.PreviewMouseLeftButtonUp += ink_MouseLeftButtonUp;
            ink.MouseMove += ink_MouseMove;
            ink.PreviewMouseMove += ink_MouseMove;
            ink.MouseLeave += ink_MouseLeave;
            //defaults some properties so drawing will work
            InkMode = PenMode.pen;
            MainColor = Colors.Black;
            OutlineColor = Colors.Black;
            BrushWidth = 4;
            BrushHeight = 4;
        }

        void ink_MouseLeave(object sender, MouseEventArgs e)
        {
            stroke = null;
            ink.ReleaseMouseCapture();
        }

        void ink_MouseMove(object sender, MouseEventArgs e)
        {
            if (InkMode == PenMode.pen && spCol != null)
            {
                //spCol.Add(e.StylusDevice.GetStylusPoints(ink));
                spCol.Add(new StylusPoint() { X = e.MouseDevice.GetPosition(ink).X, Y = e.MouseDevice.GetPosition(ink).Y });
            }
            

            if (InkMode == PenMode.erase && ErasePoints != null)
            {
                ErasePoints.Add(e.StylusDevice.GetStylusPoints(ink));
                IEnumerable<Point> path = from p in ErasePoints select new Point() { X = p.X, Y = p.Y };
                StrokeCollection hitStrokes = ink.Strokes.HitTest(path, 50);
                if (hitStrokes.Count > 0)
                {
                    foreach (Stroke hitStroke in hitStrokes)
                    {
                        allErasedStrokes.Add(hitStroke);
                        ink.Strokes.Remove(hitStroke);
                    }
                }

            }
        }

        void ink_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (spCol != null)
            {
                stroke = new Stroke(spCol);
                stroke.DrawingAttributes.Color = MainColor;
                stroke.DrawingAttributes.Height = BrushHeight;
                stroke.DrawingAttributes.Width = BrushWidth;
                ink.Strokes.Add(stroke);
                //stroke.DrawingAttributes.OutlineColor = OutlineColor;
                stroke = null;
                ink.ReleaseMouseCapture();
                spCol = null;

                if (OnStrokesChanged != null)
                    OnStrokesChanged();
            }
        }

        void ink_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ink.CaptureMouse();
            if (InkMode == PenMode.pen)
            {
                //stroke = new Stroke(new StylusPointCollection());
                //stroke.DrawingAttributes.Color = MainColor;
                //stroke.DrawingAttributes.Height = BrushHeight;
                //stroke.DrawingAttributes.Width = BrushWidth;
                //stroke.DrawingAttributes.OutlineColor = OutlineColor;
                //stroke.StylusPoints.Add(e.StylusDevice.GetStylusPoints(ink));
                spCol = new StylusPointCollection();
                spCol.Add(new StylusPoint() { X = e.MouseDevice.GetPosition(ink).X, Y = e.MouseDevice.GetPosition(ink).Y });// e.StylusDevice.GetStylusPoints(ink));
                //ink.Strokes.Add(stroke);
                e.Handled = true;
            }
            else
            {
                ErasePoints = e.StylusDevice.GetStylusPoints(ink);
            }
        }

        #region Crap
        //public void MouseLeftButtonDown(MouseButtonEventArgs e)
        //{
        //    ink.CaptureMouse();
        //    if (InkMode == PenMode.pen)
        //    {
        //        stroke = new Stroke();
        //        stroke.DrawingAttributes.Color = MainColor;
        //        stroke.DrawingAttributes.Height = BrushHeight;
        //        stroke.DrawingAttributes.Width = BrushWidth;
        //        stroke.DrawingAttributes.OutlineColor = OutlineColor;
        //        stroke.StylusPoints.Add(e.StylusDevice.GetStylusPoints(ink));
        //        ink.Strokes.Add(stroke);
        //    }
        //    else
        //    {
        //        ErasePoints = e.StylusDevice.GetStylusPoints(ink);
        //    }
        //}

        //public void MouseLeftButtonUp()
        //{
        //    stroke = null;
        //    ink.ReleaseMouseCapture();
        //}

        //public void MouseLeave()
        //{
        //    stroke = null;
        //    ink.ReleaseMouseCapture();
        //}

        //public void MouseMove(MouseEventArgs e)
        //{
        //    if (InkMode == PenMode.pen && stroke != null)
        //    {
        //        stroke.StylusPoints.Add(e.StylusDevice.GetStylusPoints(ink));
        //    }

        //    if (InkMode == PenMode.erase && ErasePoints != null)
        //    {
        //        ErasePoints.Add(e.StylusDevice.GetStylusPoints(ink));
        //        StrokeCollection hitStrokes = ink.Strokes.HitTest(ErasePoints);
        //        if (hitStrokes.Count > 0)
        //        {
        //            foreach (Stroke hitStroke in hitStrokes)
        //            {
        //                allErasedStrokes.Add(hitStroke);
        //                ink.Strokes.Remove(hitStroke);
        //            }
        //        }

        //    }
        //}

        #endregion

        public void undoLast(PenMode inkMode)
        {
            if (inkMode == PenMode.pen && ink.Strokes.Count > 0)
            {
                ink.Strokes.RemoveAt(ink.Strokes.Count - 1);
            }
            else if (inkMode == PenMode.erase && allErasedStrokes.Count > 0)
            {
                ink.Strokes.Add(allErasedStrokes[allErasedStrokes.Count - 1]);
                allErasedStrokes.RemoveAt(allErasedStrokes.Count - 1);
            }

        }

    }
}
