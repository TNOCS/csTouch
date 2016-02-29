using csAppraisalPlugin.ViewModels;

namespace csAppraisalPlugin.Interfaces
{
    public interface ICompareResults
    {
        AppraisalPlugin Plugin { get; set; }
        event GotoDetailedViewEventHandler GotoDetailedViewModel;
    }
}