using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csCommon.Types.DataServer.PoI
{
    using System.ComponentModel;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using global::DataServer;

    using PoiServer.PoI;

    public interface IReadonlyPoIStyle
    {
        // TODO: Is this correct that the Picture can be set (and use of PropertyChanged event)?
        // Seems like a hack
        // See ucPoiIcon that monitors Picture property change
        BitmapSource Picture { get; set; }
        event PropertyChangedEventHandler PropertyChanged;

        double? IconWidth { get; }
        double? IconHeight { get; }
        Uri IconUri { get; }
        string Icon { get; }
        AnalysisMetaInfo Analysis { get; }
        byte[] PictureByteArray { get; }
        Color? FillColor{ get; }
        Color? StrokeColor { get; }
        DrawingModes? DrawingMode { get; }
        double? StrokeWidth { get; }
        string CallOutFillColorString { get; }
        Color? CallOutFillColor { get; }
        string CallOutForegroundString { get; }
        Color? CallOutForeground { get; }
        TapMode? TapMode { get; }
        TitleModes? TitleMode { get; }
        string NameLabel { get; }
        string InnerTextLabel { get; }
        double? MinResolution { get; }
        double? MaxResolution { get; }
        bool? AutoCallOutLabels { get; }
        double? StrokeOpacity { get; }
        double? FillOpacity { get; }
        bool? Visible { get; }
        string Name { get; }
        string BaseStyle { get; }
        bool? CanEdit { get; }
        bool? CanMove { get; }
        string CanMoveString { get; }
        bool? CanDelete { get; }
        bool? CanRotate { get; }
        string Category { get; }
        CallOutOrientation? CallOutOrientation { get; }
        int? CallOutTimeOut { get; }
        TimelineBehaviours? TimelineBehaviour { get; }
        string ShowOnTimeline { get; }
        AddModes? AddMode { get; }
        double? MaxTitleResolution { get; }
        double? CallOutMaxWidth { get; }
        double? CallOutMinHeight { get; }
        string SubTitles { get; }
        Color? InnerTextColor { get; }
        bool? Cluster { get; }
        bool? ScalePoi { get; }
        double? ScaleStartResolution { get; }
        double? ScaleUnits { get; }
        double? MaxScale { get; }

        PoIStyle CloneStyle();
    }
}
