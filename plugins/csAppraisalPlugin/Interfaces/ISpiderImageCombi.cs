using Caliburn.Micro;
using csAppraisalPlugin.Classes;
using csAppraisalPlugin.ViewModels;

namespace csAppraisalPlugin.Interfaces {
    public interface ISpiderImageCombi : IScreen {
        event CloseViewHandler CloseView;
        Appraisal SelectedAppraisal { get; set; }
    }
}
