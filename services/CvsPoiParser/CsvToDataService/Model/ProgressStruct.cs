using System.ComponentModel;

namespace CsvToDataService.Model
{
    public class ProgressStruct : INotifyPropertyChanged
    {
        private bool done;
        private int numDone;
        private int numTotal;

        public ProgressStruct()
        {
            NumTotal = 100;
            NumDone = 0;
        }

        public int NumTotal
        {
            get { return numTotal; }
            set
            {
                if (numTotal != value)
                {
                    numTotal = value;
                    OnPropertyChanged("NumTotal");
                }
            }
        }

        public int NumDone
        {
            get { return numDone; }
            set
            {
                if (numDone != value)
                {
                    numDone = value;
                    OnPropertyChanged("NumDone");
                }
            }
        }

        public bool Done
        {
            get { return done; }
            set
            {
                if (done != value)
                {
                    done = value;
                    OnPropertyChanged("Done");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}