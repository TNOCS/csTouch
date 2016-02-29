using System.Windows;
using System.Windows.Controls;

namespace csShared.Controls
{
    
        public class InfoBox : ContentControl
        {
            #region Properties

          
            #region Text

            public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(object), typeof(InfoBox), new UIPropertyMetadata(null));
            public object Text
            {
                get
                {
                    return (object)GetValue(TextProperty);
                }
                set
                {
                    SetValue(TextProperty, value);
                }
            }

            #endregion 

            #region InfoBoxTemplate

            public static readonly DependencyProperty InfoBoxTemplateProperty = DependencyProperty.Register("InfoBoxTemplate", typeof(DataTemplate), typeof(InfoBox), new UIPropertyMetadata(null));
            public DataTemplate InfoBoxTemplate
            {
                get
                {
                    return (DataTemplate)GetValue(InfoBoxTemplateProperty);
                }
                set
                {
                    SetValue(InfoBoxTemplateProperty, value);
                }
            }

            #endregion //WatermarkTemplate

            #endregion //Properties

            #region Constructors

            static InfoBox()
            {
                DefaultStyleKeyProperty.OverrideMetadata(typeof(InfoBox), new FrameworkPropertyMetadata(typeof(InfoBox)));
            }

            #endregion //Constructors

            #region Base Class Overrides

           

            

            #endregion //Base Class Overrides
        }
    
}
