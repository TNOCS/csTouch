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
using System.Windows.Documents;
using System.Windows.Media;

namespace DevZest.Windows
{
    partial class AdornerManager
    {
        private class DecoratorAdorner : Adorner
        {
            private UIElement _child;
            private AdornerLayer _adornerLayer;

            public DecoratorAdorner(FrameworkElement source, DataTemplate adorner)
                : base(source)
            {
                Debug.Assert(source != null);
                Debug.Assert(adorner != null);
                ContentPresenter contentPresenter = new ContentPresenter();
                contentPresenter.Content = source;
                contentPresenter.ContentTemplate = adorner;
                _child = contentPresenter;
                AddLogicalChild(_child);
                AddVisualChild(_child);
            }

            public DecoratorAdorner(FrameworkElement source, UIElement adorner)
                : base(source)
            {
                DataContext = source;
                _child = adorner;
                AddVisualChild(_child);
            }

            private FrameworkElement Source
            {
                get { return AdornedElement as FrameworkElement; }
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                _child.Arrange(new Rect(new Point(), finalSize));
                return finalSize;
            }

            protected override Visual GetVisualChild(int index)
            {
                if (index != 0)
                    throw new ArgumentOutOfRangeException("index");
                return _child;
            }

            protected override int VisualChildrenCount
            {
                get { return 1; }
            }

            public void Show()
            {
                Debug.Assert(_adornerLayer == null);

                if (!Source.IsLoaded)
                    Source.Loaded += new RoutedEventHandler(OnLoaded);
                else
                    AddToAdornerLayer();
            }

            private void OnLoaded(object sender, RoutedEventArgs e)
            {
                Source.Loaded -= new RoutedEventHandler(OnLoaded);
                AddToAdornerLayer();
            }

            private void AddToAdornerLayer()
            {
                _adornerLayer = AdornerLayer.GetAdornerLayer(AdornedElement);
                if (_adornerLayer != null)
                    _adornerLayer.Add(this);
            }

            public void Close()
            {
                Source.Loaded -= new RoutedEventHandler(OnLoaded);
                if (_adornerLayer != null)
                {
                    _adornerLayer.Remove(this);
                    _adornerLayer = null;
                }
            }
        }
    }
}
