namespace csAppraisalPlugin
{
    public interface IAppraisalPlugin
    {
        
    }

    public interface IAppraisalTab
    {
        AppraisalPlugin Plugin { get; set; }
    }

    public interface IFunctionsTab
    {
        AppraisalPlugin Plugin { get; set; }
    }

    public interface IAppraisal
    {
        AppraisalPlugin Plugin { get; set; }
    }

    public interface IAppraisalMain
    {
        AppraisalPlugin Plugin { get; set; }
    }

    public interface IFullScreenAppraisal
    {
        
    }

    public interface IFloatingAppraisal
    {
        
    }

    
}
