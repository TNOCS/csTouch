// Copied from http://www.arcgis.com/home/item.html?id=1e432da7e74f4402bd43a5863167022d
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ESRI.ArcGIS.Client.Symbols;

namespace csCommon.Types.Geometries.AdvancedGeometry.Symbols
{
    /// <summary>
    /// Line symbol allowing the geometry to be transformed by a transformer.
    /// </summary>
    public class ExtendedLineSymbol : CartographicLineSymbol
    {
        #region Ctors
        private static readonly ControlTemplate _controlTemplate;
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedLineSymbol"/> class.
        /// </summary>
        public ExtendedLineSymbol()
        {
            SelectionColor = new SolidColorBrush(Colors.Cyan);
            Fill = new SolidColorBrush(Colors.Black); // can be used by extended geometries
            ControlTemplate = _controlTemplate;
            PropertyChanged += ExtendedLineSymbol_PropertyChanged;
        }

        void ExtendedLineSymbol_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
           
        }

        static ExtendedLineSymbol()
        {
            var uri = new Uri("/csCommon;component/Types/Geometries/AdvancedGeometry/Symbols/SymbolTemplates.xaml", UriKind.Relative);
            var resourceDictionary = new ResourceDictionary { Source = uri };
            _controlTemplate = resourceDictionary["ExtendedLineSymbol"] as ControlTemplate;
        }
        #endregion

        #region SelectionColor
        /// <summary>
        /// Identifies the <see cref="SelectionColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionColorProperty =
            DependencyProperty.Register("SelectionColor", typeof(Brush), typeof(ExtendedLineSymbol), null);

        /// <summary>
        /// Gets or sets the selection color.
        /// </summary>
        public Brush SelectionColor
        {
            get
            {
                return (Brush)GetValue(SelectionColorProperty);
            }
            set
            {
                SetValue(SelectionColorProperty, value);
            }
        }
        #endregion

        #region Fill
        /// <summary>
        /// Identifies the Fill Brush dependency property
        /// </summary>
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill", typeof(Brush), typeof(ExtendedLineSymbol), new PropertyMetadata(OnFillPropertyChanged));

        /// <summary>
        /// Gets or sets the fill <see cref="Brush"/>.
        /// </summary>
        /// <value>The fill.</value>
        public Brush Fill
        {
            get
            {
                return GetValue(FillProperty) as Brush;
            }

            set
            {
                SetValue(FillProperty, value);
            }
        }

        private static void OnFillPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dp = d as ExtendedLineSymbol;
            if (dp != null)
                dp.OnPropertyChanged("Fill");
        }

        #endregion

        #region GeometryTransformer
        /// <summary>
        /// Gets or sets the geometry transformer.
        /// </summary>
        /// <value>The geometry transformer.</value>
        public IGeometryTransformer GeometryTransformer
        {
            get { return (IGeometryTransformer)GetValue(GeometryTransformerProperty); }
            set { SetValue(GeometryTransformerProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="GeometryTransformerProperty"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeometryTransformerProperty =
            DependencyProperty.Register("GeometryTransformer", typeof(IGeometryTransformer), typeof(ExtendedLineSymbol), new PropertyMetadata(null, OnTFPropertyChanged));

        private static void OnTFPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
           // var dp = d as ExtendedLineSymbol;
            //if (dp != null)
             //   dp.OnPropertyChanged("GeometryTransformer");
        }
        #endregion


    }
}
