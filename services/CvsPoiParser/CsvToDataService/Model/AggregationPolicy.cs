using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using csCommon.Types.TextAnalysis;

namespace CsvToDataService.Model
{
    /// <summary>
    /// Aggregation policies for grouping data.
    /// </summary>
    public class AggregationPolicy : PropertyChangedBase
    {
        public enum NumericAggregation
        {
            KeepFirst, Average, Sum, Omit
        }

        public enum NonNumericAggregation
        {
            KeepFirst, Keywords, Concatenate, Omit
        }

        private string _label;
        private readonly HashSet<string> _values = new HashSet<string>(); // The values the column 'label' can take. 
        private bool _dataIsNumeric;

        private NumericAggregation _numericAggregationPolicy = NumericAggregation.Sum;
        private NonNumericAggregation _nonNumericAggregationPolicy = NonNumericAggregation.Omit;

        public string Label
        {
            get { return _label; }
            set
            {
                _label = value; 
                NotifyOfPropertyChange(() => Label);
            }
        }

        public void AddValueIfUnique(string value)
        {
            _values.Add(value);
        }

        public string ExampleValues
        {
            get
            {
                if (! _values.Any()) return "No values";
                StringBuilder stringBuilder = new StringBuilder("Example values: ");
                int index = 0;
                foreach (string value in _values)
                {
                    string valueT = (value.Length > 12) ? value.Substring(0, 9) + "..." : value;
                    if (index > 0)
                    {
                        stringBuilder.Append(", ");                        
                    }
                    stringBuilder.Append(valueT);
                    index++;
                    if (index == 5)
                    {
                        break;
                    }
                }
                if (index == 5 && _values.Count > 5)
                {
                    stringBuilder.Append(", et cetera.");
                }
                return stringBuilder.ToString();
            }
        }

        public int Count
        {
            get { return _values.Count; }
        }

        public bool DataIsNumeric
        {
            get { return _dataIsNumeric; }
            set
            {
                if (value == _dataIsNumeric) return;
                _dataIsNumeric = value;
                NotifyOfPropertyChange(() => DataIsNumeric);
            }
        }

        public bool IsOmit
        { 
            get {
                return (DataIsNumeric && NumericAggregationPolicy == NumericAggregation.Omit) ||
                   (!DataIsNumeric && NonNumericAggregationPolicy == NonNumericAggregation.Omit);
            }
        }

        public bool IsKeywordAggregation
        {
            get { return !DataIsNumeric && NonNumericAggregationPolicy == NonNumericAggregation.Keywords; }
        }

        public IEnumerable<string> Policies { get
        {
            if (DataIsNumeric) return Enum.GetNames(typeof (NumericAggregation));
            else return Enum.GetNames(typeof(NonNumericAggregation));
        }}

        public string AggregationPolicyString
        {
            get
            {
                if (DataIsNumeric) return _numericAggregationPolicy.ToString();
                else return _nonNumericAggregationPolicy.ToString();
            }
            set
            {
                if (DataIsNumeric) Enum.TryParse(value, out _numericAggregationPolicy);
                else Enum.TryParse(value, out _nonNumericAggregationPolicy);

                NotifyOfPropertyChange(() => AggregationPolicyString);

                NotifyOfPropertyChange(() => NumericAggregationPolicy); // The out args above cannot refer to a property, so fire the property change event here.
                NotifyOfPropertyChange(() => NonNumericAggregationPolicy);
            }
        }

        public NumericAggregation NumericAggregationPolicy
        {
            get { return _numericAggregationPolicy; }
            set
            {
                _numericAggregationPolicy = value;
                NotifyOfPropertyChange(() => NumericAggregationPolicy);
                NotifyOfPropertyChange(() => AggregationPolicyString);
            }
        }

        public NonNumericAggregation NonNumericAggregationPolicy
        {
            get { return _nonNumericAggregationPolicy; }
            set
            {
                _nonNumericAggregationPolicy = value;
                NotifyOfPropertyChange(() => NonNumericAggregationPolicy);
                NotifyOfPropertyChange(() => AggregationPolicyString);
            }
        }

        // Can return null.
        public string Aggregrate(IEnumerable<string> data)
        {
            if (DataIsNumeric)
            {
                return AggregateNumeric(data);
            }
            else
            {
                return AggregateNonNumeric(data);                
            }
        }

        private string AggregateNumeric(IEnumerable<string> data)
        {
            IEnumerable<string> enumerableData = data as string[] ?? data.ToArray();
            switch (NumericAggregationPolicy)
            {
                case NumericAggregation.KeepFirst:
                    return enumerableData.FirstOrDefault();

                case NumericAggregation.Average:
                    return "" + (CalculateSum(enumerableData)/enumerableData.Count());

                case NumericAggregation.Sum:
                    return "" + (CalculateSum(enumerableData));

                case NumericAggregation.Omit:
                default:
                    return null;
            }
        }

        private static double CalculateSum(IEnumerable<string> enumerableData)
        {
            double sum = 0;
            foreach (string s in enumerableData)
            {
                double d;
                if (Double.TryParse(s, out d))
                {
                    sum += d;
                }
            }
            return sum;
        }

        private string AggregateNonNumeric(IEnumerable<string> data)
        {
            switch (NonNumericAggregationPolicy)
            {
                case NonNumericAggregation.Concatenate:
                    StringBuilder sb = new StringBuilder();
                    foreach (string s in data)
                    {
                        sb.Append(s);
                    }
                    return sb.ToString();

                case NonNumericAggregation.KeepFirst:
                    return data.FirstOrDefault();

                case NonNumericAggregation.Keywords:
                    StringBuilder allText = new StringBuilder();
                    foreach (string text in data)
                    {
                        allText.Append(text).Append(" ");
                    }
                    WordHistogram histogram = new WordHistogram();
                    return histogram.ToGeoJson(); // Pity, we only support aggregation to strings.

                case NonNumericAggregation.Omit:
                default:
                    return null;
            }
        }        
    }
}
