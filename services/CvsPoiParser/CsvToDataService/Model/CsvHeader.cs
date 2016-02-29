using csCommon.Types.DataServer.PoI.Templates;
using csCommon.Utils;
using DataServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CsvToDataService.Model
{
    // TODO Keywords saved to CSV are not read back in.

    [DebuggerDisplay("{Id}: {Header} => {ProcessingAction}")]
    public class CsvHeader : MetaInfo // PropertyChangedBase
    {
        // Needed for templates!

        public const string NO_SECTION = "none";
        private static HashSet<string> _sections;
        private bool canSearchSetExplicitly;
        private int id;
        private int index;
        private IEnumerable<string> _templates;
        private string _template;
        private TemplateStore<MetaInfo> _templateStore;
        private ProcessingActions _processingAction;
        private bool _metaTypeGuessed = false;

        public CsvHeader()
        {
            base.Type = MetaTypes.unknown;
        }

        public CsvHeader(int id, string header, ProcessingActions processingAction)
        {
            Id = id;
            Header = header;
            ProcessingAction = processingAction;
            ExampleCellValues = new List<string>();
            if (ProcessingAction != ProcessingActions.WKT) return;
            VisibleInCallOut = false;
            Title = "<not shown>";
            base.Type = MetaTypes.unknown; // deliberate
            IsEditable = false;
            base.IsSearchable = false;
            canSearchSetExplicitly = true;
        }

        /// <summary>
        ///     Field indicator (starting with 1)
        /// </summary>
        public new int Id
        {
            get { return id; }
            set
            {
                id = value;
                Index = id;
            }
        }

        /// <summary>
        ///     Column header.
        /// </summary>
        public string Header
        {
            get { return Label; }
            set
            {
                Label = value.Replace(" ", "_").Replace("(", string.Empty).Replace(")", string.Empty).Replace("+", "_");
                if (string.IsNullOrEmpty(Title))
                    Title = UppercaseFirstLetter(value.Replace("_", " "));
                // TODO REVIEW: Replace("P_", "% "). was unstable.
            }
        }

        /// <summary>
        ///     Indicates what to do with this data column.
        /// </summary>
        public ProcessingActions ProcessingAction
        {
            get { return _processingAction; }
            set
            {
                if (_processingAction == value) return;
                _processingAction = value;
                NotifyOfPropertyChange(() => VisibleInCallOut);
                NotifyOfPropertyChange(() => ProcessingAction);
                NotifyOfPropertyChange(() => CanModifyAttributes);
                NotifyOfPropertyChange(() => HasTemplates); // Some result in the header not being editable at all.
                NotifyOfPropertyChange(() => CanSetNumberFormatting); // Some result in the header not being editable at all.
            }
        }

        /// <summary>
        ///     A list of all possible column values.
        /// </summary>
        public List<string> ExampleCellValues { get; private set; }

        public new bool VisibleInCallOut
        {
            get
            {
                return ProcessingAction != ProcessingActions.WKT && base.VisibleInCallOut;
            }
            set
            {
                if (!CanModifyAttributes)
                {
                    base.VisibleInCallOut = false; // Should stay invisible.
                }
                base.VisibleInCallOut = value;
            }
        }

        public bool CanModifyAttributes
        {
            get
            {
                return !(ProcessingAction == ProcessingActions.WKT ||
                       ProcessingAction == ProcessingActions.Latitude ||
                       ProcessingAction == ProcessingActions.LatitudeDegrees ||
                       ProcessingAction == ProcessingActions.Longitude ||
                       ProcessingAction == ProcessingActions.LongitudeDegrees);
            }
        }

        public new bool IsSearchable
        {
            get
            {
                if (!canSearchSetExplicitly)
                {
                    return (base.Type == MetaTypes.text);
                }
                return base.IsSearchable;
            }
            set
            {
                canSearchSetExplicitly = true; // Make sure we do not override it with our heuristic.
                base.IsSearchable = value;
            }
        }


        public new MetaTypes Type
        {
            get
            {
                if (ProcessingAction == ProcessingActions.WKT)
                {
                    return MetaTypes.unknown;
                }
                if (_metaTypeGuessed || (base.Type != MetaTypes.unknown && base.Type != MetaTypes.text)) // text is the default
                {
                    return base.Type;
                }
                // Guess the meta type.
                var length = Math.Min(10, ExampleCellValues.Count);
                // Guess that the data is numeric.
                for (int i = 0; i < length; i++)
                {
                    decimal number;
                    bool ignoreHyphen = ExampleCellValues[i].Trim() == "-";
                    if (ignoreHyphen ||
                        decimal.TryParse(ExampleCellValues[i], NumberStyles.Number, CultureInfo.InvariantCulture, out number) ||
                        decimal.TryParse(ExampleCellValues[i], NumberStyles.Number, CultureInfo.CurrentCulture, out number))
                    {
                        base.Type = MetaTypes.number;
                        StringFormat = NumberFormats.GetFormatString(NumberFormats.INTEGER); // Default value.
                    }
                    else
                    {
                        base.Type = MetaTypes.unknown; // not sure yet.
                        break;
                    }
                }
                if (base.Type != MetaTypes.unknown)
                {
                    _metaTypeGuessed = true;
                    return base.Type;
                }
                // Guess that the data is boolean.
                for (int i = 0; i < length; i++)
                {
                    bool boolean;
                    bool ignoreHyphen = ExampleCellValues[i].Trim() == "-";
                    var val = Regex.Replace(ExampleCellValues[i], "ja", "true", RegexOptions.IgnoreCase);
                    // TODO REVIEW Ugly Dutch hack.
                    val = Regex.Replace(val, "nee", "false", RegexOptions.IgnoreCase);
                    if (ignoreHyphen || bool.TryParse(val, out boolean))
                    {
                        base.Type = MetaTypes.boolean; // 
                    }
                    else
                    {
                        base.Type = MetaTypes.text; // this was our last guess.
                        break;
                    }
                }

                // I like to convert a boolean to option too, so we can easily filter it in JSON.
                if (base.Type != MetaTypes.boolean)
                {
                    // Final guess, try options
                    const int maxNumberOfOptions = 29; // TODO This needs to be one less than maxNumberOfExamples in ConvertCsv
                    if (ExampleCellValues.Count <= maxNumberOfOptions)
                    {
                        // It seems reasonable to assume that we are dealing with options
                        base.Type = MetaTypes.options;
                        ExampleCellValues.Sort();
                        Options = ExampleCellValues.ToList();
                    }
                }
                base.Type = MetaTypes.options;
                _metaTypeGuessed = true;
                return base.Type;
            }
            set
            {
                if (base.Type == value) return;
                if (ProcessingAction == ProcessingActions.WKT)
                {
                    return; // We will not change it.
                }
                base.Type = value;
                NotifyOfPropertyChange(() => Type);
                // Also modify whether the data is searchable, automatically.
                canSearchSetExplicitly = false;
                NotifyOfPropertyChange(() => IsSearchable);
                // Also modify whether the data supports number formatting.
                NotifyOfPropertyChange(() => CanSetNumberFormatting);
                // Also modify whether the data can have a min value.
                NotifyOfPropertyChange(() => CanSetMinValue);
            }
        }

        public bool CanSetNumberFormatting
        {
            get { return CanModifyAttributes && base.Type == MetaTypes.number; }
            //            set
            //            {
            //                // TODO This is very ugly, but I cannot get the binding to work otherwise. 
            //                throw new Exception("An attempt to modify Read-Only property");
            //            }
        }

        public string NumberFormatDescription
        {
            get { return NumberFormats.GetFormatDescription(StringFormat) ?? NumberFormats.UNFORMATTED; }
            set { StringFormat = NumberFormats.GetFormatString(value); }
        }

        public bool CanSetMinValue
        {
            get { return CanModifyAttributes && base.Type == MetaTypes.number; }
        }

        public string MinValueDescription
        {
            get { return (Math.Abs(MinValue - double.MinValue) < 0.00001) ? "None" : MinValue.ToString(CultureInfo.InvariantCulture); }
            set
            {
                double val;
                MinValue = double.TryParse(value, out val) ? val : Double.MinValue;
            }
        }

        public new double MinValue
        {
            get { return base.MinValue; }
            set
            {
                if (Type != MetaTypes.number)
                {
                    return;
                }
                base.MinValue = value;
                NotifyOfPropertyChange(() => MinValue);
                NotifyOfPropertyChange(() => MinValueDescription);
            }
        }

        public int Index // Allows headers to be re-ordered (these are the Id, by default, but we can change that).
        {
            get { return index; }
            set
            {
                if (index == value) return;
                index = value;
                NotifyOfPropertyChange(() => Index);
            }
        }

        public static HashSet<string> Sections
        {
            get
            {
                if (_sections == null)
                {
                    _sections = new HashSet<string>();
                    _sections.Add(NO_SECTION);
                }
                return _sections;
            }
        }

        public new string Section
        {
            get { return string.IsNullOrEmpty(base.Section) ? NO_SECTION : base.Section; }
            set
            {
                if (!_sections.Contains(value))
                {
                    _sections.Add(value);
                }
                if (value == NO_SECTION)
                {
                    base.Section = null;
                }
                else
                {
                    base.Section = value;
                }
                NotifyOfPropertyChange(() => Section);
            }
        }

        public TemplateStore<MetaInfo> TemplateStore
        {
            get { return _templateStore; }
            set
            {
                //int i = 0;
                if (_templateStore == value) return;
                _templateStore = value;
                NotifyOfPropertyChange(() => TemplateStore);
                if (_templateStore != null)
                {
                    Templates = _templateStore.GetTemplateCollectionNames(base.Id);
                }
                else
                {
                    Templates = null;
                }
            }
        }

        public bool HasTemplates
        {
            get { return Templates != null && Templates.Any() && CanModifyAttributes; } // If we cannot modify the templates, then don't show them :)
        }

        public IEnumerable<string> Templates
        {
            get { return _templates; }
            set
            {
                if (_templates == value) return;
                _templates = value;
                NotifyOfPropertyChange(() => Templates);
                NotifyOfPropertyChange(() => HasTemplates);
                if (_templates == null)
                {
                    _template = null;
                    return;
                }
                foreach (string template in _templates)
                {
                    var metaInfo = _templateStore.GetTemplate(template, base.Id);
                    if (metaInfo != null && !string.IsNullOrEmpty(metaInfo.Section))
                    {
                        Sections.Add(metaInfo.Section);
                    }
                }

                if (!string.IsNullOrEmpty(_template) && _templates.Contains(_template)) return;
                if (_templates.Any())
                {
                    Template = Templates.First();
                }
                else
                {
                    Template = null;
                }
            }
        }

        public string Template
        {
            get { return _template; }
            set
            {
                if (_template == value) return;
                _template = value;
                if (_templateStore != null)
                {
                    MetaInfo metaInfo = _templateStore.GetTemplate(_template, base.Id);
                    if (metaInfo != null) FromXml(metaInfo.ToXml()); // Copy template data in here.                    
                }
                NotifyOfPropertyChange(() => Template);
            }
        }

        public CsvHeader Self
        {
            get { return this; }
        }

        private static string UppercaseFirstLetter(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }
    }
}