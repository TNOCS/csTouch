using System;
using System.Globalization;
using Caliburn.Micro;
using DataServer;

namespace csCommon.Plugins.EffectAppraisalPlugin.ViewModels
{
    public class AppraisalEffect : PropertyChangedBase
    {
        private double effectValue;

        private readonly PoI selectedPoI;

        public string Title { get; set; }

        public string Label { get; set; }

        public double Min { get; set; }

        public double Max { get; set; }

        public int TickFrequency { get; private set; }

        public double EffectValue
        {
            get { return effectValue; }
            set
            {
                if (value.Equals(effectValue)) return;
                effectValue = value;
                selectedPoI.Labels[Label] = effectValue.ToString(CultureInfo.InvariantCulture);
                NotifyOfPropertyChange(() => EffectValue);
            }
        }

        public AppraisalEffect(PoI poi, string title, string label, double effectValue, double min = 0, double max = 10) {
            selectedPoI      = poi;
            Title            = title;
            Label            = label;
            Min              = min;
            Max              = max;
            this.effectValue = effectValue;
            if (max < min) Max = min + 10;
            TickFrequency = (int) Math.Round((Max - Min)/10);
        }
    }
}
