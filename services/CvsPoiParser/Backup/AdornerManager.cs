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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace DevZest.Windows
{
    /// <summary>Provides attached properties to get or set a <see cref="UIElement"/> or <see cref="DataTemplate"/> as the adorner of
    /// <see cref="FrameworkElement"/>.</summary>
    /// <remarks><para><see cref="AdornerManager"/> provides two attached properties: <b>Adorner</b> to get or set <see cref="UIElement"/>
    /// as adorner of <see cref="FrameworkElement"/>; and <b>AdornerTemplate</b> to get or set <see cref="DataTemplate"/>
    /// as adorner of <see cref="FrameworkElement"/>. You can use these attached properties to declare adorner of
    /// <see cref="FrameworkElement"/> in XAML.</para>
    /// <para><b>AdornerTemplate</b> provides similar functionality as <b>Adorner</b>, however it can be used in style setters because
    /// direct UI child is not allowed. Both way the provided adorner inherits DataContext as adorned element.</para>
    /// <para>Setting adorner for specified <see cref="FrameworkElement"/> will clear the adorner previously set, no matter using
    /// <b>Adorner</b> or <b>AdornerTemplate</b> attached properties.</para></remarks>
    /// <example>The following example demostrates the usage of <b>Adorner</b> and <b>AdornerTemplate</b> attached property:
    ///     <code lang="xaml" source="..\Samples\Common\CSharp\AdornerManagerSample\Window1.xaml" />
    /// </example>
    public static partial class AdornerManager
    {
        /// <summary>Identifies the <b>Adorner</b> attached property.</summary>
        public static readonly DependencyProperty AdornerProperty = DependencyProperty.RegisterAttached("Adorner", typeof(UIElement), typeof(AdornerManager),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnAdornerChanged)));
        /// <summary>Identifies the <b>AdornerTemplate</b> attached property.</summary>
        public static readonly DependencyProperty AdornerTemplateProperty = DependencyProperty.RegisterAttached("AdornerTemplate", typeof(DataTemplate), typeof(AdornerManager),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnAdornerChanged)));
        private static readonly DependencyPropertyKey DecoratorAdornerPropertyKey = DependencyProperty.RegisterAttachedReadOnly("DecoratorAdorner", typeof(DecoratorAdorner), typeof(AdornerManager),
            new FrameworkPropertyMetadata(null));

        private static void OnAdornerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = d as FrameworkElement;
            if (element == null)
                return;

            DecoratorAdorner oldDecorator = GetDecoratorAdorner(element);
            DecoratorAdorner newDecorator = GetNewDecorator(element, e.NewValue);
            SetDecoratorAdorner(element, newDecorator);
            if (oldDecorator != null)
                oldDecorator.Close();
            if (newDecorator != null)
                newDecorator.Show();
        }

        private static DecoratorAdorner GetNewDecorator(FrameworkElement element, object newValue)
        {
            DataTemplate newTemplate = newValue as DataTemplate;
            if (newTemplate != null)
                return new DecoratorAdorner(element, newTemplate);

            UIElement newUIElement = newValue as UIElement;
            if (newUIElement != null)
                return new DecoratorAdorner(element, newUIElement);

            return null;
        }

        /// <summary>Gets the adorner <see cref="UIElement"/> for specified <see cref="FrameworkElement"/>.
        /// Getter of <b>Adorner</b> attached property.</summary>
        /// <param name="element">The specified <see cref="FrameworkElement"/>.</param>
        /// <returns>The adorner <see cref="UIElement" />.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static UIElement GetAdorner(FrameworkElement element)
        {
            return (UIElement)element.GetValue(AdornerProperty);
        }

        /// <summary>Sets the adorner <see cref="UIElement"/> for specified <see cref="FrameworkElement"/>.
        /// Setter of <b>Adorner</b> attached property.</summary>
        /// <param name="element">The specified <see cref="FrameworkElement"/>.</param>
        /// <param name="value">The adorner <see cref="UIElement"/> to set.</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void SetAdorner(FrameworkElement element, UIElement value)
        {
            element.SetValue(AdornerProperty, value);
        }

        /// <summary>Gets the adorner <see cref="DataTemplate"/> for specified <see cref="FrameworkElement"/>.
        /// Getter of <b>AdornerTemplate</b> attached property.</summary>
        /// <param name="element">The specified <see cref="FrameworkElement"/>.</param>
        /// <returns>The adorner <see cref="DataTemplate" />.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static DataTemplate GetAdornerTemplate(FrameworkElement element)
        {
            return (DataTemplate)element.GetValue(AdornerTemplateProperty);
        }

        /// <summary>Sets the adorner <see cref="DataTemplate"/> for specified <see cref="FrameworkElement"/>.
        /// Setter of <b>AdornerTemplate</b> attached property.</summary>
        /// <param name="element">The specified <see cref="FrameworkElement"/>.</param>
        /// <param name="value">The adorner <see cref="DataTemplate"/> to set.</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void SetAdornerTemplate(FrameworkElement element, DataTemplate value)
        {
            element.SetValue(AdornerTemplateProperty, value);
        }

        private static DecoratorAdorner GetDecoratorAdorner(DependencyObject element)
        {
            return (DecoratorAdorner)element.GetValue(DecoratorAdornerPropertyKey.DependencyProperty);
        }

        private static void SetDecoratorAdorner(DependencyObject element, DecoratorAdorner value)
        {
            element.SetValue(DecoratorAdornerPropertyKey, value);
        }
    }
}
