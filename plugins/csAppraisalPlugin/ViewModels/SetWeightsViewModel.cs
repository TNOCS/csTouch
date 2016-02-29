using System.ComponentModel;
using System.ComponentModel.Composition;
using Caliburn.Micro;
using csAppraisalPlugin.Classes;
using csAppraisalPlugin.Interfaces;

namespace csAppraisalPlugin.ViewModels
{
    [Export(typeof (ISetWeights)), PartCreationPolicy(CreationPolicy.Shared)]
    public class SetWeightsViewModel : Screen, ISetWeights
    {
        [ImportingConstructor]
        public SetWeightsViewModel()
        {
        }

        protected override void OnInitialize()
        {
            DisplayName = "criteria";
            if (Plugin != null && Plugin.SelectedAppraisals != null) {
                Plugin.PropertyChanged += PluginOnPropertyChanged;
            }
            base.OnInitialize();
        }

        private void PluginOnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName.Equals("SelectedAppraisal")) NotifyOfPropertyChange(() => Criterias);
        }

        protected override void OnDeactivate(bool close)
        {
            if (Plugin.SelectedAppraisals.Count <= 1) return;

            var criterias = Criterias;
            for (var i = 1; i < Plugin.SelectedAppraisals.Count; i++)
            {
                var appraisal = Plugin.SelectedAppraisals[i];
                for (var j = 0; j < appraisal.Criteria.Count; j++)
                {
                    var criterion = appraisal.Criteria[j];
                    criterion.Weight = criterias[j].Weight;
                }
            }
            base.OnDeactivate(close);
        }

        public AppraisalPlugin Plugin { get; set; }

        public CriteriaList Criterias
        {
            get
            {
                return Plugin == null || Plugin.SelectedAppraisals == null || Plugin.SelectedAppraisals.Count == 0 
                    ? null
                    : Plugin.SelectedAppraisals[0].Criteria;
            }
        }

        private Criterion selectedCriteria;

        public Criterion SelectedCriteria
        {
            get { return selectedCriteria; }
            set
            {
                if (selectedCriteria == value) return;
                selectedCriteria = value;
                NotifyOfPropertyChange(() => SelectedCriteria);
            }
        }

    }
}