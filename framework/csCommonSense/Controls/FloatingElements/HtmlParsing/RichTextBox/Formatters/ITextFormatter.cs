using System.Windows.Documents;

namespace csShared.Html
{
    public interface ITextFormatter
    {
        string GetText(FlowDocument document);
        void SetText(FlowDocument document, string text);
    }
}
