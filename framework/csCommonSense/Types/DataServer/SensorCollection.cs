using System.Linq;
using Caliburn.Micro;
using csEvents.Sensors;

namespace DataServer
{
    public class SensorCollection : BindableCollection<DataSet>
    {
        private string title;

        public string Title
        {
            get { return title; }
            set
            {
                if (string.Equals(title, value)) return;
                title = value;
                NotifyOfPropertyChange();
            }
        }

        public DataSet FindDataSet(string dataSetId)
        {
            return this.FirstOrDefault(k => k.DataSetId == dataSetId);
        }
    }
}