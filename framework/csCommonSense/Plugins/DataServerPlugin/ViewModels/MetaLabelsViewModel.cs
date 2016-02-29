using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using csShared.Controls.Popups.MenuPopup;
using DataServer;
using System.ComponentModel;

namespace csDataServerPlugin.ViewModels
{
    [Export(typeof(IEditableScreen))]
    public class MetaLabelsViewModel : Screen, IEditableScreen
    {
        private BindableCollection<MetaLabel> content;
        private bool canEdit;

        public bool CanEdit
        {
            get { return canEdit; }
            set
            {
                if (canEdit == value) return;
                canEdit = value;
                NotifyOfPropertyChange(() => CanEdit);
            }
        }

        public MapCallOutViewModel CallOut { get; set; }

        public BindableCollection<MetaLabel> Content
        {
            get { return content; }
            set
            {
                if (content == value) return;
                content = value;
                NotifyOfPropertyChange(() => Content);
            }
        }

        public void SelectOption(MetaLabel ml, FrameworkElement b)
        {
            var m = GetMenu(b);
            foreach (var o in ml.Meta.Options)
            {
                var mi = m.AddMenuItem(o);
                mi.Click += (e, s) =>
                {
                    ml.Data = o;
                    ml.PoI.Labels[ml.Meta.Label] = o;
                    m.Close();
                };
            }

            AppStateSettings.GetInstance().Popups.Add(m);
        }

        public void SelectFocus(MetaLabel ml, FrameworkElement b)
        {
            ml.Data = AppStateSettings.Instance.TimelineManager.FocusTime.ToString();
            //ml.PoI.Labels[ml.Meta.Label] = ml.Data;
        }

        public void SelectDatetime(MetaLabel ml, FrameworkElement b)
        {
            //var d = new DatetimePopupViewModel
            //{
            //    RelativeElement = b,
            //    RelativePosition = new Point(35, -5),
            //    TimeOut = new TimeSpan(0, 0, 0, 15),
            //    VerticalAlignment = VerticalAlignment.Top,                
            //    AutoClose = true
            //};


            //AppStateSettings.GetInstance().Popups.Add(d);
        }

        private MenuPopupViewModel GetMenu(FrameworkElement fe)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement = fe,
                RelativePosition = new Point(35, -5),
                TimeOut = new TimeSpan(0, 0, 1, 15),
                VerticalAlignment = VerticalAlignment.Top,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            return menu;
        }
    }

    public class MetaLabelTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NumberTemplate { get; set; }
        public DataTemplate NumberEditorTemplate { get; set; }
        public DataTemplate BooleanTemplate { get; set; }
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate TextEditorTemplate { get; set; }
        public DataTemplate SensorTemplate { get; set; }
        public DataTemplate TextareaTemplate { get; set; }
        public DataTemplate TextareaEditorTemplate { get; set; }
        public DataTemplate RatingTemplate { get; set; }
        public DataTemplate OptionsTemplate { get; set; }
        public DataTemplate OptionsEditorTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate BBCodeBlockTemplate { get; set; }
        public DataTemplate DatetimeTemplate { get; set; }
        public DataTemplate DatetimeEditorTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var path = (MetaLabel)item; 

            if (path == null) return TextTemplate;

            var ds = path.Meta;
            if (ds == null)
            {
                return base.SelectTemplate(item, container);
            }
            PropertyChangedEventHandler lambda = null;
            lambda = (o, args) =>
            {
                if (args.PropertyName == "InEditMode")
                {
                    ds.PropertyChanged -= lambda;
                    var cp = (ContentPresenter)container;
                    cp.ContentTemplateSelector = null;
                    cp.ContentTemplateSelector = this;
                }
            };
            ds.PropertyChanged += lambda;

            var isEditMode = path.Meta.InEditMode;

            switch (path.Meta.Type)
            {
                case MetaTypes.number:
                    return isEditMode ? NumberEditorTemplate : NumberTemplate;
                case MetaTypes.sensor:
                    return SensorTemplate;
                case MetaTypes.textarea:
                    return isEditMode ? TextareaEditorTemplate : TextareaTemplate;
                case MetaTypes.bbcode:
                    return isEditMode ? TextareaEditorTemplate : BBCodeBlockTemplate;
                case MetaTypes.boolean:
                    return BooleanTemplate;
                case MetaTypes.rating:
                    return RatingTemplate;
                case MetaTypes.options:
                    return isEditMode ? OptionsEditorTemplate : OptionsTemplate;
                case MetaTypes.image:
                    return ImageTemplate;
                case MetaTypes.datetime:
                    return isEditMode ? DatetimeEditorTemplate : DatetimeTemplate;
                default:
                    return isEditMode ? TextEditorTemplate : TextTemplate;
            }
        }
    }

}
