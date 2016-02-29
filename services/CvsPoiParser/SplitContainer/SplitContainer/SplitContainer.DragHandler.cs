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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DevZest.Windows
{
    partial class SplitContainer
    {
        private class DragHandler : DragHandlerBase
        {
            private static DragHandler s_default;
            
            private SplitContainer _splitContainer;
            private SplitterDistance _originalSplitterDistance;
            private double _offsetX, _offsetY;

            public static DragHandler Default
            {
                get
                {
                    if (s_default == null)
                        s_default = new DragHandler();
                    return s_default;
                }
            }

            private DragHandler()
            {
            }

            private bool ShowsPreview
            {
                get { return _splitContainer.ShowsPreview; }
            }

            private double DragIncrement
            {
                get { return _splitContainer.DragIncrement; }
            }

            private bool IsPreviewVisible
            {
                get { return _splitContainer.IsPreviewVisible; }
            }

            private void ShowPreview()
            {
                Debug.Assert(!IsPreviewVisible);
                _splitContainer.IsPreviewVisible = true;
                PreviewOffsetX = PreviewOffsetY = 0;
            }

            private void ClosePreview()
            {
                Debug.Assert(IsPreviewVisible);
                PreviewOffsetX = PreviewOffsetY = 0;
                _splitContainer.IsPreviewVisible = false;
            }

            private double PreviewOffsetX
            {
                set
                {
                    Debug.Assert(IsPreviewVisible);
                    _splitContainer.PreviewOffsetX = value;
                }
            }

            private double PreviewOffsetY
            {
                set
                {
                    Debug.Assert(IsPreviewVisible);
                    _splitContainer.PreviewOffsetY = value;
                }
            }

            private void MoveSplitter(double offsetX, double offsetY)
            {
                _offsetX += offsetX;
                _offsetY += offsetY;
                _splitContainer.MoveSplitter(offsetX, offsetY);
            }

            public void BeginDrag(SplitContainer splitContainer, MouseEventArgs e)
            {
                _splitContainer = splitContainer;
                _originalSplitterDistance = _splitContainer.SplitterDistance;
                _offsetX = _offsetY = 0;
                Debug.Assert(splitContainer.Splitter != null);
                DragDetect((UIElement)VisualTreeHelper.GetChild(splitContainer.Splitter, 0), e);
            }

            protected override void OnBeginDrag()
            {
                if (ShowsPreview)
                    ShowPreview();
            }

            protected override void OnDragDelta()
            {
                Point offset = GetOffset(MouseDeltaX, MouseDeltaY);
                if (IsPreviewVisible)
                {
                    PreviewOffsetX = offset.X;
                    PreviewOffsetY = offset.Y;
                }
                else
                    MoveSplitter(offset.X, offset.Y);
            }

            private Point GetOffset(double horizontalChange, double verticalChange)
            {
                return _splitContainer.GetOffset(horizontalChange - _offsetX, verticalChange - _offsetY, DragIncrement);
            }

            protected override void OnEndDrag(UIElement dragElement, bool abort)
            {
                if (IsPreviewVisible)
                    ClosePreview();

                if (abort)
                    _splitContainer.SplitterDistance = _originalSplitterDistance;
                else
                {
                    Point offset = GetOffset(MouseDeltaX, MouseDeltaY);
                    MoveSplitter(offset.X, offset.Y);
                }
            }
        }
    }
}
