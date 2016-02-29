namespace csPresenterPlugin.Events
{
    public enum FolderAction { Add, Remove }

    public class FolderEvent
    {
        public FolderEvent(string folderName, FolderAction action)
        {
            FolderName = folderName;
            Action = action;
        }

        public string FolderName { get; set; }
        public FolderAction Action { get; set; }
    }
}
