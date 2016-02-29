// Filename:    ShapeDisplay.cs
// Description: A helper class for importing ESRI shapefiles and
//              creating/displaying shapes on a WPF canvas.
// Comments:    Uses the classes from ShapeFile.cs.
// 2007-01-29 nschan Initial revision.

using ESRI.ArcGIS.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.OleDb;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TestShapeFile
{
    /// <summary>
    /// The GeometryType enumeration defines geometry types that
    /// can be used when creating WPF shapes. Choosing a stream
    /// geometry type can improve rendering performance.
    /// </summary>
    public enum GeometryType
    {
        /// <summary>
        /// Use path geometry containing path figures.
        /// </summary>
        UsePathGeometry,

        /// <summary>
        /// Use StreamGeometry with StreamGeometryContext class
        /// to specify drawing instructions.
        /// </summary>
        UseStreamGeometry,

        /// <summary>
        /// Same as UseStreamGeometry except that the figures
        /// will be unstroked for greater performance (borders
        /// won't be displayed for the shapes).
        /// </summary>
        UseStreamGeometryNotStroked
    }

    /// <summary>
    /// ShapeDisplay is a helper class for importing ESRI shapefiles
    /// and creating/displaying shapes on a WPF canvas. During the
    /// import step, a progress window is displayed. This is implemented
    /// using the WPF single-threaded programming model, which allows
    /// tasks to be executed by a Dispatcher instance while keeping the
    /// UI responsive.
    /// </summary>
    public class ShapeDisplay
    {
        #region Delegates
        /// <summary>
        /// Defines the prototype for a method that reads a block
        /// of shapefile records. Such a method is intended to be
        /// executed by the Dispatcher.
        /// </summary>
        /// <param name="info">Shapefile read information.</param>
        public delegate void ReadNextPrototype(ShapeFileReadInfo info);

        /// <summary>
        /// Defines the prototype for a method that creates and displays
        /// a set of WPF shapes. Such a method is intended to be
        /// executed by the Dispatcher.
        /// </summary>
        /// <param name="info">Shapefile read information.</param>
        public delegate void DisplayNextPrototype(ShapeFileReadInfo info);
        #endregion Delegates

        #region Constants
        private const int readShapesBlockingFactor = 50;
        private const int displayShapesBlockingFactor = 10;
        private const string baseLonLatText = "Lon/Lat: ";
        #endregion Constants

        #region Private fields
        // UI components.
        private Window owner;
        private Canvas canvas;
        private GraphicsLayer grLayer;
        private Dispatcher dispatcher;
        //private ProgressWindow progressWindow;

        // Used during reading of a shapefile.
        private bool isReadingShapeFile;
        private bool cancelReadShapeFile;
        private int wpfShapeCount;

        // Used during creation of WPF shapes.
        private List<Shape> shapeList = new List<Shape>();
        private GeometryType geometryType = GeometryType.UseStreamGeometryNotStroked;

        // Transformation from lon/lat to canvas coordinates.
        private TransformGroup shapeTransform;

        // Combined view transformation (zoom and pan).
        private TransformGroup viewTransform = new TransformGroup();
        private ScaleTransform zoomTransform = new ScaleTransform();
        private TranslateTransform panTransform = new TranslateTransform();

        // For coloring of WPF shapes.
        private Brush[] shapeBrushes;
        private Random rand = new Random(379013);
        private Brush strokeBrush = new SolidColorBrush(Color.FromArgb(150, 150, 150, 150));

        // For panning operations.
        private bool isPanningEnabled = true;
        Point prevMouseLocation;
        bool isMouseDragging;
        private double panTolerance = 1;

        // Displaying lon/lat coordinates on the canvas.
        bool isDisplayLonLatEnabled = true;
        Label lonLatLabel = new Label();
        #endregion Private fields

        #region Constructor
        /// <summary>
        /// Constructor for the ShapeDisplay class.
        /// </summary>
        /// <param name="owner">Window that acts as an owner to child windows.</param>
        /// <param name="canvas">The canvas on which to create WPF shapes.</param>
        public ShapeDisplay(Window owner, Canvas canvas)
        {
            // Keep reference to a Window to act as the owner for a progress window.
            if (owner == null)
                throw new ArgumentNullException("owner");
            this.owner = owner;

            // Keep reference to the canvas and add mouse event handlers
            // for implementing panning.
            if (canvas == null)
                throw new ArgumentNullException("canvas");
            this.canvas = canvas;
            this.canvas.MouseEnter += canvas_MouseEnter;
            this.canvas.MouseDown += canvas_MouseDown;
            this.canvas.MouseMove += canvas_MouseMove;
            this.canvas.MouseUp += canvas_MouseUp;
            this.canvas.MouseLeave += canvas_MouseLeave;

            // Keep reference to the dispatcher for task execution.
            this.dispatcher = this.canvas.Dispatcher;

            // Add the zoom and pan transforms to the view transform.
            this.viewTransform.Children.Add(this.zoomTransform);
            this.viewTransform.Children.Add(this.panTransform);

            // Configure the lon/lat label.
            this.lonLatLabel.Opacity = 0.70;
        }

        /// <summary>
        /// Constructor for the ShapeDisplay class.
        /// </summary>
        /// <param name="owner">Window that acts as an owner to child windows.</param>
        /// <param name="canvas">The canvas on which to create WPF shapes.</param>
        public ShapeDisplay(Window owner, GraphicsLayer layer)
        {
            // Keep reference to a Window to act as the owner for a progress window.
            if (owner == null)
                throw new ArgumentNullException("owner");
            this.owner = owner;

            // Keep reference to the canvas and add mouse event handlers
            // for implementing panning.
            if (layer == null)
                throw new ArgumentNullException("layer");
            this.grLayer = layer;
            /*this.grLayer.MouseEnter += new MouseEventHandler(canvas_MouseEnter);
            this.grLayer.MouseDown += new System.Windows.Input.MouseButtonEventHandler(canvas_MouseDown);
            this.grLayer.MouseMove += new System.Windows.Input.MouseEventHandler(canvas_MouseMove);
            this.grLayer.MouseUp += new System.Windows.Input.MouseButtonEventHandler(canvas_MouseUp);
            this.grLayer.MouseLeave += new MouseEventHandler(canvas_MouseLeave);*/

            // Keep reference to the dispatcher for task execution.
            this.dispatcher = this.canvas.Dispatcher;

            // Add the zoom and pan transforms to the view transform.
            this.viewTransform.Children.Add(this.zoomTransform);
            this.viewTransform.Children.Add(this.panTransform);

            // Configure the lon/lat label.
            this.lonLatLabel.Opacity = 0.70;
        }
        #endregion Constructor

        #region Properties
        /// <summary>
        /// Indicates if a shapefile read operation is in progress.
        /// </summary>
        public bool IsReadingShapeFile
        {
            get { return this.isReadingShapeFile; }
        }

        /// <summary>
        /// Indicates if we can perform a zoom operation. This is true
        /// the shape transform has been set (meaning at least one shapefile
        /// has been loaded).
        /// </summary>
        public bool CanZoom
        {
            get
            {
                return (this.shapeTransform != null);
            }
        }

        /// <summary>
        /// Indicates if panning is enabled or not. This
        /// applies to both mouse and keyboard panning.
        /// </summary>
        public bool IsPanningEnabled
        {
            get { return this.isPanningEnabled; }
            set { this.isPanningEnabled = value; }
        }

        /// <summary>
        /// Indicates if display of lon/lat coordinates
        /// is enabled or not.
        /// </summary>
        public bool IsDisplayLonLatEnabled
        {
            get { return this.isDisplayLonLatEnabled; }
            set
            {
                if (this.isDisplayLonLatEnabled != value)
                {
                    this.isDisplayLonLatEnabled = value;
                    if (this.isDisplayLonLatEnabled)
                        this.DisplayLonLatDefault();
                    else
                        this.canvas.Children.Remove(this.lonLatLabel);
                }
            }
        }

        /// <summary>
        /// Specifies the geometry type to use when creating WPF shapes.
        /// </summary>
        public GeometryType GeometryType
        {
            get { return this.geometryType; }
            set { this.geometryType = value; }
        }
        #endregion Properties

        #region Public methods
        /// <summary>
        /// Read shapes and attributes from the given shapefile.
        /// </summary>
        /// <param name="fileName">Full pathname of a shapefile.</param>
        public void ReadShapeFile(string fileName)
        {
            this.isReadingShapeFile = true;
            this.cancelReadShapeFile = false;

            // Create an object to store shapefile info during the read.
            ShapeFileReadInfo info = new ShapeFileReadInfo();
            info.FileName = fileName;
            info.ShapeFile = new ShapeFile();
            info.Stream = null;
            info.NumberOfBytesRead = 0;
            info.RecordIndex = 0;

            try
            {
                // Read the File Header first.
                info.Stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                info.ShapeFile.ReadShapeFileHeader(info.Stream);
                info.NumberOfBytesRead = ShapeFileHeader.Length;

                // Schedule the first read of shape file records using the dispatcher.
                this.dispatcher.BeginInvoke(DispatcherPriority.Normal, new ReadNextPrototype(this.ReadNextShapeRecord), info);
            }
            catch (IOException ex)
            {
                this.EndReadShapeFile(info);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Request the current shapefile read operation to be cancelled.
        /// </summary>
        public void CancelReadShapeFile()
        {
            this.cancelReadShapeFile = true;
            this.HideProgress();
        }

        /// <summary>
        /// Reset the canvas.
        /// </summary>
        public void ResetCanvas()
        {
            // End reading of the shapefile.
            this.EndReadShapeFile();

            // Clear the canvas.
            this.canvas.Children.Clear();
            this.wpfShapeCount = 0;

            // Reset transformations.
            this.panTransform.X = 0;
            this.panTransform.Y = 0;
            this.zoomTransform.ScaleX = 1;
            this.zoomTransform.ScaleY = 1;
            this.shapeTransform = null;
        }

        /// <summary>
        /// Perform a zoom operation about the current center
        /// of the canvas.
        /// </summary>
        /// <param name="zoomFactor">Zoom multiplication factor (1, 2, 4, etc).</param>
        public void Zoom(double zoomFactor)
        {
            // Compute the coordinates of the center of the canvas
            // in terms of pre-view transformation values. We do this
            // by applying the inverse of the view transform.
            Point canvasCenter = new Point(this.canvas.ActualWidth / 2, this.canvas.ActualHeight / 2);
            canvasCenter = this.viewTransform.Inverse.Transform(canvasCenter);

            // Temporarily reset the panning transformation.
            this.panTransform.X = 0;
            this.panTransform.Y = 0;

            // Set the new zoom transformation scale factors.
            this.zoomTransform.ScaleX = zoomFactor;
            this.zoomTransform.ScaleY = zoomFactor;

            // Apply the updated view transform to the canvas center.
            // This gives us the updated location of the center point
            // on the canvas. By differencing this with the desired
            // center of the canvas, we can determine the ideal panning
            // transformation parameters.
            Point canvasLocation = this.viewTransform.Transform(canvasCenter);
            this.panTransform.X = this.canvas.ActualWidth / 2 - canvasLocation.X;
            this.panTransform.Y = this.canvas.ActualHeight / 2 - canvasLocation.Y;
        }

        /// <summary>
        /// Perform a panning operation given X and Y factor values
        /// which can be thought of as a fraction of the canvas actual
        /// width or height.
        /// </summary>
        /// <param name="factorX">Fraction of canvas actual width to pan horizontally.</param>
        /// <param name="factorY">Fraction of canvas actual height to pan vertically.</param>
        public void Pan(double factorX, double factorY)
        {
            if (!this.isPanningEnabled)
                return;

            this.panTransform.X += (factorX * this.canvas.ActualWidth);
            this.panTransform.Y += (factorY * this.canvas.ActualHeight);
        }

        /// <summary>
        /// Save the owner object (the main window) to XAML.
        /// This method may take a long time to run if there
        /// are many objects on the canvas (shapefile > 1 Mb).
        /// </summary>
        /// <param name="stream">Output stream for writing the XAML.</param>
        public void SaveToXaml(Stream stream)
        {
            System.Windows.Markup.XamlWriter.Save(this.owner, stream);
        }
        #endregion Public methods

        #region Transformations
        /// <summary>
        /// Computes a transformation so that the shapefile geometry
        /// will maximize the available space on the canvas and be
        /// perfectly centered as well.
        /// </summary>
        /// <param name="info">Shapefile information.</param>
        /// <returns>A transformation object.</returns>
        private TransformGroup CreateShapeTransform(ShapeFileReadInfo info)
        {
            // Bounding box for the shapefile.
            double xmin = info.ShapeFile.FileHeader.XMin;
            double xmax = info.ShapeFile.FileHeader.XMax;
            double ymin = info.ShapeFile.FileHeader.YMin;
            double ymax = info.ShapeFile.FileHeader.YMax;

            // Width and height of the bounding box.
            double width = Math.Abs(xmax - xmin);
            double height = Math.Abs(ymax - ymin);

            // Aspect ratio of the bounding box.
            double aspectRatio = width / height;

            // Aspect ratio of the canvas.
            double canvasRatio = this.canvas.ActualWidth / this.canvas.ActualHeight;

            // Compute a scale factor so that the shapefile geometry
            // will maximize the space used on the canvas while still
            // maintaining its aspect ratio.
            double scaleFactor = 1.0;
            if (aspectRatio < canvasRatio)
                scaleFactor = this.canvas.ActualHeight / height;
            else
                scaleFactor = this.canvas.ActualWidth / width;

            // Compute the scale transformation. Note that we flip
            // the Y-values because the lon/lat grid is like a cartesian
            // coordinate system where Y-values increase upwards.
            ScaleTransform xformScale = new ScaleTransform(scaleFactor, -scaleFactor);

            // Compute the translate transformation so that the shapefile
            // geometry will be centered on the canvas.
            TranslateTransform xformTrans = new TranslateTransform();
            xformTrans.X = (this.canvas.ActualWidth - (xmin + xmax) * scaleFactor) / 2;
            xformTrans.Y = (this.canvas.ActualHeight + (ymin + ymax) * scaleFactor) / 2;

            // Add the two transforms to a transform group.
            TransformGroup xformGroup = new TransformGroup();
            xformGroup.Children.Add(xformScale);
            xformGroup.Children.Add(xformTrans);

            return xformGroup;
        }
        #endregion Transformations

        #region Brushes for gradient coloring
        /// <summary>
        /// Create a set of linear gradient brushes which we can use
        /// as a random pool for assignment to WPF shapes. A higher
        /// gradient factor results in a stronger gradient effect.
        /// </summary>
        /// <param name="gradientFactor">Gradient factor from 0 to 1.</param>
        /// <param name="gradientAngle">Direction of gradient in degrees.</param>
        private void CreateShapeBrushes(double gradientFactor, double gradientAngle)
        {
            // Pick a set of base colors for the brushes.
            Color[] colors = new Color[] {
                Colors.Crimson, Colors.ForestGreen, Colors.RoyalBlue,
                Colors.Navy, Colors.DarkSeaGreen, Colors.LightSlateGray, 
                Colors.DarkKhaki, Colors.Olive, Colors.Indigo, Colors.Violet };

            // Create one brush per color.
            this.shapeBrushes = new Brush[colors.Length];
            for (int i = 0; i < this.shapeBrushes.Length; i++)
            {
                this.shapeBrushes[i] = new LinearGradientBrush(ShapeDisplay.GetAdjustedColor(colors[i], gradientFactor), colors[i], gradientAngle);
            }
        }

        /// <summary>
        /// Given an input color, return an adjusted color using a
        /// factor value which ranges from 0 to 1. The larger the factor,
        /// the lighter the adjusted color. A factor of 0 means no adjustment
        /// to the input color.
        /// </summary>
        /// <remarks>
        /// Note that the alpha component of the input color is not adjusted.
        /// </remarks>
        /// <param name="inColor">Input color.</param>
        /// <param name="factor">Color adjustment factor, from 0 to 1.</param>
        /// <returns>An adjusted color value.</returns>
        private static Color GetAdjustedColor(Color inColor, double factor)
        {
            int red = inColor.R + (int)((255 - inColor.R) * factor);
            red = Math.Max(0, red);
            red = Math.Min(255, red);

            int green = inColor.G + (int)((255 - inColor.G) * factor);
            green = Math.Max(0, green);
            green = Math.Min(255, green);

            int blue = inColor.B + (int)((255 - inColor.B) * factor);
            blue = Math.Max(0, blue);
            blue = Math.Min(255, blue);

            return Color.FromArgb(inColor.A, (byte)red, (byte)green, (byte)blue);
        }

        /// <summary>
        /// Get the next brush that can be used to fill a WPF shape.
        /// </summary>
        /// <returns>A randomly selected brush.</returns>
        private Brush GetRandomShapeBrush()
        {
            int index = this.rand.Next() % this.shapeBrushes.Length;
            return this.shapeBrushes[index];
        }
        #endregion Brushes for gradient coloring

        #region Reading ESRI shapes
        /// <summary>
        /// Read a block of shape file records and possibly schedule
        /// the next read with the dispatcher.
        /// </summary>
        /// <param name="info">Shapefile read information.</param>
        private void ReadNextShapeRecord(ShapeFileReadInfo info)
        {
            if (this.cancelReadShapeFile)
                return;

            try
            {
                // Read a block of shape records.
                for (int i = 0; i < ShapeDisplay.readShapesBlockingFactor; i++)
                {
                    ShapeFileRecord record = info.ShapeFile.ReadShapeFileRecord(info.Stream);
                    info.NumberOfBytesRead += (4 + record.ContentLength) * 2;
                }
            }
            catch (FileFormatException ex)
            {
                this.EndReadShapeFile(info);
                MessageBox.Show(ex.Message);

                return;
            }
            catch (IOException)
            {
                // Display the end progress (100 percent).
                this.ShowProgress("Reading shapefile...", 100);

                // Read attributes from the associated dBASE file.
                this.ReadDbaseAttributes(info);

                // Display shapes on the canvas.
                if (info.ShapeFile.Records.Count > 0)
                    this.DisplayShapes(info);
                else
                    this.EndReadShapeFile(info);

                return;
            }

            // Display the current progress.
            double progressValue = info.NumberOfBytesRead * 100.0 / (info.ShapeFile.FileHeader.FileLength * 2);
            progressValue = Math.Min(100, progressValue);
            this.ShowProgress("Reading shapefile...", progressValue);

            // Schedule the next read at Background priority.
            this.dispatcher.BeginInvoke(DispatcherPriority.Background, new ReadNextPrototype(this.ReadNextShapeRecord), info);
        }

        /// <summary>
        /// Perform some cleanup at the end of reading a shapefile.
        /// </summary>
        private void EndReadShapeFile()
        {
            this.HideProgress();
            this.isReadingShapeFile = false;
            this.cancelReadShapeFile = true;
        }

        public delegate void GeometryAddEventHandler(PointCollection points);

        public event GeometryAddEventHandler GeometryAdd;

        /// <summary>
        /// Perform some cleanup at the end of reading a shapefile.
        /// </summary>
        /// <param name="info">Shapefile read information.</param>
        private void EndReadShapeFile(ShapeFileReadInfo info)
        {
            if (info != null && info.Stream != null)
            {
                info.Stream.Close();
                info.Stream.Dispose();
                info.Stream = null;
            }

            this.EndReadShapeFile();
        }

        /// <summary>
        /// Read dBASE file attributes.
        /// </summary>
        /// <param name="info">Shapefile read information.</param>
        private void ReadDbaseAttributes(ShapeFileReadInfo info)
        {
            // Read attributes from the associated dBASE file.
            try
            {
                string dbaseFile = info.FileName.Replace(".shp", ".dbf");
                dbaseFile = dbaseFile.Replace(".SHP", ".DBF");
                info.ShapeFile.ReadAttributes(dbaseFile);
            }
            catch (OleDbException ex)
            {
                // Note: An exception will occur if the filename of the dBASE
                // file does not follow 8.3 naming conventions. In this case,
                // you must use its short (MS-DOS) filename.
                MessageBox.Show(ex.Message);

                // Activate the window.
                this.owner.Activate();
            }
        }
        #endregion Reading ESRI shapes

        #region Creating / displaying WPF shapes
        /// <summary>
        /// Create a WPF shape given a shapefile record.
        /// </summary>
        /// <param name="shapeName">The name of the WPF shape.</param>
        /// <param name="record">Shapefile record.</param>
        /// <returns>The created WPF shape.</returns>
        private Shape CreateWPFShape(string shapeName, ShapeFileRecord record)
        {
            // Create a new geometry.
            Geometry geometry;
            if (this.geometryType == GeometryType.UsePathGeometry)
                geometry = this.CreatePathGeometry(record);
            else
                geometry = this.CreateStreamGeometry(record);

            // Transform the geometry based on current zoom and pan settings.
            //geometry.Transform = this.viewTransform;

            // Create a new WPF Path.
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();

            // Assign the geometry to the path and set its name.
            path.Data = geometry;
            path.Name = shapeName;

            // Set path properties.
            path.StrokeThickness = 0.5;
            if (record.ShapeType == (int)ShapeType.Polygon)
            {
                path.Stroke = this.strokeBrush;
                path.Fill = this.GetRandomShapeBrush();
            }
            else
            {
                path.Stroke = Brushes.DimGray;
            }

            // Return the created WPF shape.
            return path;
        }

        /// <summary>
        /// Create a PathGeometry given a shapefile record.
        /// </summary>
        /// <param name="record">Shapefile record.</param>
        /// <returns>A PathGeometry instance.</returns>
        private Geometry CreatePathGeometry(ShapeFileRecord record)
        {
            // Create a new geometry.
            PathGeometry geometry = new PathGeometry();

            // Add figures to the geometry.
            for (int i = 0; i < record.NumberOfParts; i++)
            {
                // Create a new path figure.
                PathFigure figure = new PathFigure();

                // Determine the starting index and the end index
                // into the points array that defines the figure.
                int start = record.Parts[i];
                int end;
                if (record.NumberOfParts > 1 && i != (record.NumberOfParts - 1))
                    end = record.Parts[i + 1];
                else
                    end = record.NumberOfPoints;

                // Add line segments to the figure.
                for (int j = start; j < end; j++)
                {
                    System.Windows.Point pt = record.Points[j];

                    // Transform from lon/lat to canvas coordinates.
                    pt = this.shapeTransform.Transform(pt);

                    if (j == start)
                        figure.StartPoint = pt;
                    else
                        figure.Segments.Add(new LineSegment(pt, true));
                }

                // Add the new figure to the geometry.
                geometry.Figures.Add(figure);
            }

            // Return the created path geometry.
            return geometry;
        }

        public List<Geometry> Geos = new List<Geometry>();

        /// <summary>
        /// Create a StreamGeometry given a shapefile record.
        /// </summary>
        /// <param name="record">Shapefile record.</param>
        /// <returns>A StreamGeometry instance.</returns>
        private Geometry CreateStreamGeometry(ShapeFileRecord record)
        {
            // Create a new stream geometry.
            StreamGeometry geometry = new StreamGeometry();

            PointCollection points = new PointCollection();
            // Obtain the stream geometry context for drawing each part.
            //using (StreamGeometryContext ctx = geometry.Open())
            {
                // Draw figures.
                for (int i = 0; i < record.NumberOfParts; i++)
                {
                    // Determine the starting index and the end index
                    // into the points array that defines the figure.
                    int start = record.Parts[i];
                    int end;
                    if (record.NumberOfParts > 1 && i != (record.NumberOfParts - 1))
                        end = record.Parts[i + 1];
                    else
                        end = record.NumberOfPoints;

                    // Draw the figure.
                    for (int j = start; j < end; j++)
                    {
                        System.Windows.Point pt = record.Points[j];
                        points.Add(pt);
                        // Transform from lon/lat to canvas coordinates.
                        //pt = this.shapeTransform.Transform(pt);

                        // Decide if the line segments are stroked or not. For the
                        // PolyLine type it must be stroked.
                        bool isStroked = (record.ShapeType == (int)ShapeType.PolyLine) || !(this.geometryType == GeometryType.UseStreamGeometryNotStroked);

                        // Register the drawing instruction.
                        if (j == start)
                        {
                        }
                        //ctx.BeginFigure(pt, true, false);
                        //ctx.LineTo(pt, isStroked, true);
                    }
                    if (GeometryAdd != null)
                    {
                        GeometryAdd(points);
                        points.Clear();
                    }
                }
            }
            //Geos.Add(geometry);

            // Return the created stream geometry.
            return geometry;
        }

        /// <summary>
        /// Create a WPF shape to represent a shapefile point or
        /// multipoint record.
        /// </summary>
        /// <param name="shapeName">The name of the WPF shape.</param>
        /// <param name="record">Shapefile record.</param>
        /// <returns>The created WPF shape.</returns>
        private Shape CreateWPFPoint(string shapeName, ShapeFileRecord record)
        {
            // Create a new geometry.
            GeometryGroup geometry = new GeometryGroup();

            // Add ellipse geometries to the group.
            foreach (Point pt in record.Points)
            {
                // Create a new ellipse geometry.
                EllipseGeometry ellipseGeo = new EllipseGeometry();

                // Transform center point of the ellipse from lon/lat to
                // canvas coordinates.
                ellipseGeo.Center = this.shapeTransform.Transform(pt);

                // Set the size of the ellipse.
                ellipseGeo.RadiusX = 0.1;
                ellipseGeo.RadiusY = 0.1;

                // Add the ellipse to the geometry group.
                geometry.Children.Add(ellipseGeo);
            }

            // Transform the geometry based on current zoom and pan settings.
            geometry.Transform = this.viewTransform;

            // Add the geometry to a new Path and set path properties.
            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
            path.Data = geometry;
            path.Name = shapeName;
            path.Fill = Brushes.Crimson;
            path.StrokeThickness = 1;
            path.Stroke = Brushes.DimGray;

            // Return the created WPF shape.
            return path;
        }

        /// <summary>
        /// Begin creating and displaying WPF shapes on the canvas.
        /// </summary>
        private void DisplayShapes(ShapeFileReadInfo info)
        {
            // Create shape brushes.
            if (this.shapeBrushes == null)
                this.CreateShapeBrushes(0.40, 45);

            // Set up the transformation for WPF shapes.            
            if (this.shapeTransform == null)
                this.shapeTransform = this.CreateShapeTransform(info);

            // Schedule display of the first block of shapefile records
            // using the dispatcher.
            this.dispatcher.BeginInvoke(DispatcherPriority.Normal, new DisplayNextPrototype(this.DisplayNextShapeRecord), info);
        }

        /// <summary>
        /// Display a block of shape records as WPF shapes, and schedule
        /// the next display with the dispatcher if needed.
        /// </summary>
        /// <param name="info">Shapefile read information.</param>
        private void DisplayNextShapeRecord(ShapeFileReadInfo info)
        {
            if (this.cancelReadShapeFile)
                return;

            // Create a block of WPF shapes and add them to the shape list. 
            this.shapeList.Clear();
            int index = info.RecordIndex;
            for (; index < (info.RecordIndex + ShapeDisplay.displayShapesBlockingFactor); index++)
            {
                if (index >= info.ShapeFile.Records.Count)
                    break;

                ShapeFileRecord record = info.ShapeFile.Records[index];

                // Set the name of the WPF shape.
                ++this.wpfShapeCount;
                string shapeName = String.Format(System.Globalization.CultureInfo.InvariantCulture, "Shape{0}", this.wpfShapeCount);

                // Create the WPF shape.
                Shape shape;
                if (record.NumberOfParts == 0)
                    shape = this.CreateWPFPoint(shapeName, record);
                else
                    shape = this.CreateWPFShape(shapeName, record);

                // Set a tooltip for the shape that displays up to 5 attribute values.
                shape.ToolTip = shape.Name;
                if (record.Attributes != null)
                {
                    string attr = String.Empty;
                    for (int i = 0; i < Math.Min(5, record.Attributes.ItemArray.GetLength(0)); i++)
                    {
                        attr += (", " + record.Attributes[i].ToString());
                    }
                    shape.ToolTip += attr;
                }

                // Add the shape to the shape list.                
                //this.shapeList.Add(shape);

                // If the record just processed is very large, then don't process
                // any further records.
                if (record.Points.Count > 5000)
                {
                    ++index;
                    break;
                }
            }

            // Set the record index to read next (as part of the
            // next dispatched task).
            info.RecordIndex = index;

            // Add the newly created WPF shapes to the canvas.
            foreach (Shape shape in this.shapeList)
            {
                this.canvas.Children.Add(shape);
            }
            this.shapeList.Clear();

            // Display the current progress.
            double progressValue = (index * 100.0) / info.ShapeFile.Records.Count;
            progressValue = Math.Min(100, progressValue);
            this.ShowProgress("Creating WPF shapes...", progressValue);

            // See if we need to dispatch another display operation.
            if (index < info.ShapeFile.Records.Count)
            {
                // Schedule the next display at Background priority.
                this.dispatcher.BeginInvoke(DispatcherPriority.Background, new DisplayNextPrototype(this.DisplayNextShapeRecord), info);
            }
            else
            {
                // End the progress.
                this.ShowProgress("Creating WPF shapes...", 100);
                this.EndReadShapeFile(info);
            }
        }
        #endregion Creating / displaying WPF shapes

        #region Displaying progress
        /// <summary>
        /// Show the progress window with the given progress value.
        /// </summary>
        /// <param name="progressText">Progress text to display.</param>
        /// <param name="progressValue">Progress value from 0 to 100 percent.</param>
        private void ShowProgress(string progressText, double progressValue)
        {
            /*if ( this.progressWindow == null )
            {
                // Create a new progress window.
                this.progressWindow = new ProgressWindow();
                this.progressWindow.Owner = this.owner;
                this.progressWindow.Cancel += new CancelEventHandler(this.progressWindow_Cancel);
                this.progressWindow.Closed += new System.EventHandler(this.progressWindow_Closed);
                this.progressWindow.Title = "Import Shapefile";                
            }
            
            // Show the progress window with new progress text and value.
            this.progressWindow.ProgressText = progressText;
            this.progressWindow.ProgressValue = progressValue;
            this.progressWindow.Show();   */
        }

        /// <summary>
        /// Hide the progress window.
        /// </summary>
        private void HideProgress()
        {
            /*if ( this.progressWindow != null )
            {
                this.progressWindow.Close();
            }*/
        }

        /// <summary>
        /// Handle the Cancel button being pressed in the progress window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void progressWindow_Cancel(object sender, CancelEventArgs e)
        {
            this.EndReadShapeFile();
        }

        /// <summary>
        /// Handle closing of the progress window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void progressWindow_Closed(object sender, EventArgs e)
        {
            //this.progressWindow = null;
        }
        #endregion Displaying progress

        #region Lon/lat coordinates
        /// <summary>
        /// Given a canvas position, convert it to a longitude
        /// and latitude coordinate.
        /// </summary>
        /// <param name="canvasPosition">A canvas position or location.</param>
        /// <returns>The corresponding point in lon/lat coordinates.</returns>
        private Point GetLonLatCoordinates(Point canvasPosition)
        {
            // Apply the inverse of the view transformation.
            Point p1 = this.viewTransform.Inverse.Transform(canvasPosition);

            // Apply the inverse of the shape transformation.
            if (this.shapeTransform != null)
            {
                Point p2 = this.shapeTransform.Inverse.Transform(p1);
                return p2;
            }

            return p1;
        }

        /// <summary>
        /// Given a lon/lat coordinate, determine the corresponding
        /// display text.
        /// </summary>
        /// <param name="lonLat">A lon/lat coordinate.</param>
        /// <returns>Formatted lon/lat display text.</returns>
        private static string GetLonLatDisplayText(Point lonLat)
        {
            string text = ShapeDisplay.baseLonLatText;
            if (lonLat.X < -180 || lonLat.X > 180 || lonLat.Y < -90 || lonLat.Y > 90)
                text += "n/a";
            else
                text += String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00}, {1:0.00}", lonLat.X, lonLat.Y);

            return text;
        }

        /// <summary>
        /// Given a canvas position, display the corresponding
        /// lon/lat coordinates on the canvas.
        /// </summary>
        /// <param name="canvasPosition">A canvas position or location.</param>
        private void DisplayLonLatCoord(Point canvasPosition)
        {
            // Remove the lon/lat label from the canvas first.
            this.canvas.Children.Remove(this.lonLatLabel);

            if (this.shapeTransform != null)
            {
                // Convert from canvas position to lon/lat coordinates.
                Point lonLat = this.GetLonLatCoordinates(canvasPosition);

                // Convert lon/lat value to a display string.
                this.lonLatLabel.Content = ShapeDisplay.GetLonLatDisplayText(lonLat);

                // Add the label back to the canvas. This ensures that
                // it will appear on top of all other canvas elements.
                this.canvas.Children.Add(this.lonLatLabel);
            }
        }

        /// <summary>
        /// Display some default text in the lon/lat label.
        /// </summary>
        private void DisplayLonLatDefault()
        {
            // Remove the lon/lat label from the canvas first.
            this.canvas.Children.Remove(this.lonLatLabel);

            // Set the default text to display.
            this.lonLatLabel.Content = ShapeDisplay.baseLonLatText + "n/a";

            // Add the label back to the canvas. This ensures that
            // it will appear on top of all other canvas elements.
            this.canvas.Children.Add(this.lonLatLabel);
        }
        #endregion Lon/lat coordinates

        #region Mouse handlers
        /// <summary>
        /// Handle the MouseEnter event for the canvas.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void canvas_MouseEnter(object sender, MouseEventArgs e)
        {
            this.isMouseDragging = false;
        }

        /// <summary>
        /// Handle the MouseDown event for the canvas.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.shapeTransform == null)
                return;

            // Display lon/lat coordinates if needed.
            Point canvasPosition = e.GetPosition(this.canvas);
            if (this.isDisplayLonLatEnabled)
                this.DisplayLonLatCoord(canvasPosition);

            // Update previous mouse location for start of dragging.
            this.prevMouseLocation = canvasPosition;
            this.isMouseDragging = true;
        }

        /// <summary>
        /// Handle the MouseMove event for the canvas.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Implement panning with the mouse. 
            if (this.isMouseDragging)
            {
                if (!this.isPanningEnabled)
                    return;

                // Obtain the current mouse location and compute the
                // difference with the previous mouse location.
                Point currMouseLocation = e.GetPosition(this.canvas);
                double xOffset = currMouseLocation.X - this.prevMouseLocation.X;
                double yOffset = currMouseLocation.Y - this.prevMouseLocation.Y;

                // To avoid panning on every single mouse move, we check
                // if the movement is larger than the pan tolerance.
                if (Math.Abs(xOffset) > this.panTolerance || Math.Abs(yOffset) > this.panTolerance)
                {
                    this.panTransform.X += xOffset;
                    this.panTransform.Y += yOffset;

                    this.prevMouseLocation = currMouseLocation;
                }
            }
            else
            {
                // Display lon/lat coordinates if needed.
                if (this.isDisplayLonLatEnabled)
                    this.DisplayLonLatCoord(e.GetPosition(this.canvas));
            }
        }

        /// <summary>
        /// Handle the MouseUp event for the canvas.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.isMouseDragging = false;
        }

        /// <summary>
        /// Handle the MouseLeave event for the canvas.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            this.isMouseDragging = false;
        }
        #endregion Mouse handlers
    }
}

// END

