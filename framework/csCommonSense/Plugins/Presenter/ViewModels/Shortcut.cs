using nl.tno.cs.presenter;

namespace nl.tno.tnopresenter.Helpers
{
    public class Shortcut
    {
        public Shortcut(string path, ItemType itemType) {
            Path = path;
            ItemType = itemType;
        }

        public string Path { get; set; }
        public ItemType ItemType { get; set; }
    }
}