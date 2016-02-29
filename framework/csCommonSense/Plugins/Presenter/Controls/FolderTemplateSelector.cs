using System.Windows;
using System.Windows.Controls;

namespace nl.tno.cs.presenter
{
    public class FolderTemplateSelector : DataTemplateSelector
    {
        public DataTemplate FolderTemplate { get; set; }
        public DataTemplate ItemsTemplate { get; set; }
        

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is Folder)
            {
                if (((Folder)item).Templated) return FolderTemplate;
                return ItemsTemplate;
            }            
            return null;
        }
    }
}