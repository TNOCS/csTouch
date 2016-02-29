using System.IO;
using System.Xml.Serialization;
using Caliburn.Micro;

namespace csBookmarkPlugin
{
    public class BookmarkList : BindableCollection<Bookmark>
    {

        public void Save(string file)
        {
            try
            {
                var xml = new FileStream(file, FileMode.Create);
                var serializer2 = new XmlSerializer(typeof(BookmarkList));
                serializer2.Serialize(xml, this);
                xml.Close();
            }
            catch { }
        }

        public static BookmarkList Load(string file)
        {
            if (!File.Exists(file)) return new BookmarkList();

            try {
                using (var reader2 = new StreamReader(file)) {
                    var serializer2 = new XmlSerializer(typeof(BookmarkList));
                    var result = (BookmarkList) serializer2.Deserialize(reader2);
                    reader2.Close();
                    return result;
                }
            }
            catch {
                return new BookmarkList();                
            }
        }
    }
}