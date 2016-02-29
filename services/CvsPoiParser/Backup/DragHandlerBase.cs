/***********************************************************************
Copyright DevZest, 2009 (http://www.devzest.com)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

This program is part of WPF Docking, a commercial undo/redo-able
docking library, which you can download from http://www.devzest.com.
You can obtain a Free Feature License of WPF Docking through
installed License Console, FREE OF CHARGE, with the benefit of writing
proprietary software, along with commercial product quality
documentation, upgrade and free technical support.
**********************************************************************/

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DevZest.Windows
{
    internal abstract class DragHandlerBase
    {
        private enum DragHandlerStatus
        {
            Idle,
            DragDetect,
            Dragging
        }

        private DragHandlerStatus _status = DragHandlerStatus.Idle;
        private UIElement _uiElement;
        private Point _startMousePosition;
        private Point _lastMousePosition;
        private DispatcherTimer _dragDetectTimer;

        protected DragHandlerBase()
        {
        }

        private DragHandlerStatus Status
        {
            get { return _status; }
            set
            {
                DragHandlerStatus oldValue = _status;
                Debug.Assert((oldValue == DragHandlerStatus.Idle && value == DragHandlerStatus.DragDetect) ||
                    (oldValue == DragHandlerStatus.DragDetect && (value == DragHandlerStatus.Dragging || value == DragHandlerStatus.Idle)) ||
                    (oldValue == DragHandlerStatus.Dragging && value == DragHandlerStatus.Idle));

                if (oldValue == DragHandlerStatus.DragDetect)
                    _dragDetectTimer.Stop();

                _status = value;

                if (value == DragHandlerStatus.DragDetect)
                {
                    _uiElement.CaptureMouse();
                    _uiElement.LostMouseCapture += new MouseEventHandler(OnLostMouseCapture);
                    _uiElement.MouseMove += new MouseEventHandler(OnMouseMove);
                    _uiElement.MouseLeftButtonUp += new MouseButtonEventHandler(OnMouseLeftButtonUp);
                    KeyboardManager.KeyDown += new Action<KeyEventArgs>(OnKeyDown);
                    KeyboardManager.KeyUp += new Action<KeyEventArgs>(OnKeyUp);

                    if (_dragDetectTimer == null)
                    {
                        _dragDetectTimer = new DispatcherTimer();
                        _dragDetectTimer.Interval = SystemParameters.MouseHoverTime;
                        _dragDetectTimer.Tick += new EventHandler(OnDragDetectTimerTick);
                    }
                    _dragDetectTimer.Start();
                }
                else if (value == DragHandlerStatus.Idle)
                {
                    _uiElement.MouseMove -= new MouseEventHandler(OnMouseMove);
                    _uiElement.MouseLeftButtonUp -= new MouseButtonEventHandler(OnMouseLeftButtonUp);
                    KeyboardManager.KeyDown -= new Action<KeyEventArgs>(OnKeyDown);
                    KeyboardManager.KeyUp -= new Action<KeyEventArgs>(OnKeyUp);
                    _uiElement.LostMouseCapture -= new MouseEventHandler(OnLostMouseCapture);
                    _uiElement.ReleaseMouseCapture();
                    _uiElement = null;
                }
            }
        }

        protected void DragDetect(UIElement uiElement, MouseEventArgs e)
        {
            Debug.Assert(Status == DragHandlerStatus.Idle);
            _uiElement = uiElement;
            _startMousePosition = _lastMousePosition = GetMousePosition(e);
            Status = DragHandlerStatus.DragDetect;
        }

        protected void DragDetect(UIElement uiElement, Point pt)
        {
            Debug.Assert(Status == DragHandlerStatus.Idle);
            _uiElement = uiElement;
            _startMousePosition = _lastMousePosition = GetMousePosition(pt);
            Status = DragHandlerStatus.DragDetect;
        }

        protected UIElement DragElement
        {
            get { return _uiElement; }
        }

        private static Point GetMousePosition(MouseEventArgs e)
        {
            return e.GetPosition(null);
        }

        private Point GetMousePosition(Point pt)
        {
            return _uiElement.PointFromScreen(pt);
        }

        protected double MouseDeltaX
        {
            get { return _lastMousePosition.X - _startMousePosition.X; }
        }

        protected double MouseDeltaY
        {
            get { return _lastMousePosition.Y - _startMousePosition.Y; }
        }

        protected abstract void OnBeginDrag();

        protected abstract void OnDragDelta();

        protected abstract void OnEndDrag(UIElement dragElement, bool abort);

        private void BeginDrag()
        {
            Debug.Assert(Status == DragHandlerStatus.DragDetect);
            Status = DragHandlerStatus.Dragging;
            OnBeginDrag();
            DragDelta();
        }

        private void DragDelta()
        {
            Debug.Assert(Status == DragHandlerStatus.Dragging);
            if (!DoubleUtil.AreClose(_lastMousePosition, _startMousePosition))
                OnDragDelta();
        }

        private void EndDrag(bool abort)
        {
            if (Status == DragHandlerStatus.DragDetect)
                Status = DragHandlerStatus.Idle;
            else
            {
                Debug.Assert(Status == DragHandlerStatus.Dragging);
                UIElement dragElement = DragElement;
                Status = DragHandlerStatus.Idle;
                OnEndDrag(dragElement, abort);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            _lastMousePosition = GetMousePosition(e);
            if (Status == DragHandlerStatus.DragDetect)
            {
                if (Math.Abs(MouseDeltaX) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(MouseDeltaY) > SystemParameters.MinimumVerticalDragDistance)
                    BeginDrag();
            }
            else
            {
                Debug.Assert(Status == DragHandlerStatus.Dragging);
                DragDelta();
            }
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            _lastMousePosition = GetMousePosition(e);
            EndDrag(true);
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _lastMousePosition = GetMousePosition(e);
            EndDrag(false);
        }

        private void OnDragDetectTimerTick(object sender, EventArgs e)
        {
            Debug.Assert(Status == DragHandlerStatus.DragDetect);
            BeginDrag();
        }

        protected virtual void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                EndDrag(true);
        }

        protected virtual void OnKeyUp(KeyEventArgs e)
        {
        }
    }
}