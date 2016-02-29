using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Caliburn.Micro;

namespace csAppraisalPlugin.Classes
{
    [Serializable]
    public class CriteriaList : BindableCollection<Criterion>
    {
        private int minimum;
        private int maximum = 10;

        public CriteriaList()
        {
        }

        public CriteriaList(FunctionList functionList, int minimum, int maximum)
        {
            this.minimum = minimum;
            this.maximum = maximum;
            var total = functionList.Count(f => f.IsDefault);
            if (total == 0) return;
            var initValue = (maximum - minimum) / 2;
            foreach (var function in functionList.Where(f => f.IsDefault))
                Add(new Criterion(function.Name, 1, initValue) { Id = function.Id });
        }

        public int Minimum
        {
            get { return minimum; }
            set
            {
                minimum = value;
                NotifyOfPropertyChange();
            }
        }

        public int Maximum
        {
            get { return maximum; }
            set
            {
                maximum = value;
                NotifyOfPropertyChange();
            }
        }

        /// <summary>
        /// Weighted average of assigned values
        /// </summary>
        [XmlIgnore]
        public double Score
        {
            get
            {
                return Count > 0
                           ? this.Select(k => k.AssignedValue*k.Weight).Sum()/this.Select(k => k.Weight).Sum()
                           : -1;
            }
        }

        private readonly List<double> assignedValues = new List<double>();
        public List<double> AssignedValues
        {
            get { return assignedValues; }
        }
 
        /// <summary>
        /// Set the assigned values
        /// </summary>
        /// <param name="newValues"></param>
        public void SetAssignedValues(List<double> newValues)
        {
            var i = 0;
            foreach (var criterion in Items)
            {
                assignedValues[i] = criterion.AssignedValue = newValues[i++];
            }
            NotifyOfPropertyChange("AssignedValues");
            NotifyOfPropertyChange("Score");
        }

        /// <summary>
        /// Update the criteria based on the current (selected) functions.
        /// In case a criterion is no longer represented by the current list of functions, add it.
        /// </summary>
        /// <param name="functionList"></param>
        public void Update(FunctionList functionList)
        {
            AddMissingFunctions(functionList);
            AddMissingCriteria(functionList);
            foreach (var criterion in Items)
            {
                var f = functionList.RetreiveById(criterion.Id);
                if (f != null) criterion.Title = f.Name;
            }
            UpdateCriteria();
            NotifyOfPropertyChange("Score");
        }

        private void UpdateCriteria()
        {
            assignedValues.Clear();
            foreach (var criterion in Items)
            {
                //criterion.PropertyChanged -= CriterionOnPropertyChanged;
                //criterion.PropertyChanged += CriterionOnPropertyChanged;
                assignedValues.Add(criterion.AssignedValue);
            }
        }

        private void CriterionOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!e.PropertyName.Equals("AssignedValue")) return;
            var i = 0;
            foreach (var criterion in Items) assignedValues[i++] = criterion.AssignedValue; 
            NotifyOfPropertyChange("Score");
        }

        /// <summary>
        /// Remove a criterion
        /// </summary>
        /// <param name="function"></param>
        public void RemoveCriterion(Function function)
        {
            var crit = Items.FirstOrDefault(c => c.Id == function.Id);
            if (crit != null) Items.Remove(crit);
            NotifyOfPropertyChange("Score");
        }

        /// <summary>
        /// Order the criteria based on the order of the functions.
        /// </summary>
        /// <param name="functionList"></param>
        public void UpdateOrdering(FunctionList functionList)
        {
            var orderedByIDList = from f in functionList
                                  join c in Items
                                  on f.Id equals c.Id
                                  select c;
            Clear();
            AddRange(orderedByIDList);
        }

        /// <summary>
        /// Add criteria that do not have a representation in the list of functions to this list.
        /// </summary>
        /// <param name="functionList"></param>
        private void AddMissingFunctions(ICollection<Function> functionList)
        {
            foreach (var criterion in Items.Where(criterion => functionList.All(f => f.Id != criterion.Id)).ToList())
                functionList.Add(new Function(criterion.Title, true) { Id = criterion.Id });
        }

        /// <summary>
        /// Add criteria that should be included, as the function IsSelected
        /// </summary>
        /// <param name="functionList"></param>
        private void AddMissingCriteria(IEnumerable<Function> functionList)
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var criterion = Items[i];
                var function = functionList.FirstOrDefault(f => f.Id == criterion.Id);
                if (function == null || !function.IsSelected)
                {
                    // Function has been removed or is no longer selected
                    RemoveAt(i);
                }
            }
            var average = (maximum - minimum) / 2D;
            foreach (var function in functionList.Where(f => f.IsSelected && Items.All(c => c.Id != f.Id)))
                Add(new Criterion(function.Name, 1, average) { Id = function.Id });
            
            // Reset weights to average
            foreach (var criterion in Items)
                criterion.Weight = 1;
        }
    }
}