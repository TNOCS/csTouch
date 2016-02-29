using csShared;

namespace Zandmotor.Controls.Plot
{
    public partial class PlotView
    {
        private AppStateSettings state
        {
            get { return AppStateSettings.Instance; }
        }



        public PlotView()
        {
            InitializeComponent();
            //_ModeSelector.ItemsSource = new string[]
            //                                {
            //                                    "Option 1",
            //                                    "Option 2",
            //                                    "Option 3",
            //                                    "Option 4",
            //                                    "Option 5",
            //                                    "Option 6"
            //                                };      
        }

            

       
        
    }
}
