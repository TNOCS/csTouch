using System.ComponentModel.Composition;
using Caliburn.Micro;
using csShared.Interfaces;

namespace csShared.FloatingElements
{
  [Export(typeof(IQrCode)), PartCreationPolicy(CreationPolicy.NonShared)]
  public class QrCodeViewModel : PropertyChangedBase, IQrCode
  {
    #region Implementation of IQrCode

    private string text;
    public string Text
    {
      get { return text; }
      set
      {
        if (text == value) return;
        text = value;
        NotifyOfPropertyChange(() => Text);
      }
    }
    
    #endregion
  }
}