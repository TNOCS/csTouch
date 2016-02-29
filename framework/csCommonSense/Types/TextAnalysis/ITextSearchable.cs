namespace csCommon.Types.TextAnalysis
{
    public interface ITextSearchable
    {
        string AllText();
        WordHistogram Keywords { get; }
        long IndexId { get; }
    }
}
