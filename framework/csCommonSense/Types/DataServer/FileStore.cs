using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csShared.Utils;
using csCommon.Logging;

namespace DataServer
{
    public class QueueEventArgs : EventArgs
    {
        public Dictionary<string, byte[]> Results { get; set; }
    }

    public class FileStore
    {
        public Service Service { get; set; }
        private QueueEventArgs queue;

        public FileStore()
        {
            queue = new QueueEventArgs {Results = new Dictionary<string, byte[]>()};
        }

        public void Init(Service s)
        {            
            Service = s;            
        }

        public static async Task<byte[]> LoadBytesFromWeb(string url)
        {
            WebClient wc = new WebClient();
            return await wc.DownloadDataTaskAsync(url);
        }

        public bool HasFile(string id,bool hash = false)
        {
            return File.Exists(id);
        }

        public bool HasFile(string folder,string id, bool hash = false)
        {
            return File.Exists(folder + "\\" + id);
        }

        public async Task<string> GetString(string folder, string id, bool hash = false)
        {
            var result = "";
            await System.Threading.Tasks.Task.Run(() =>
            {
                result = File.ReadAllText(id); // UTF-8 is default.
            }
            );
            return result;

        }

        public string GetString(string id) 
        {
            return File.ReadAllText(id); // UTF-8 is default.
        }

    

        public string[] ReadAllLines(string id)
        {
            return File.ReadAllLines(id); // UTF-8 is default.
        }

        public void QueueBytes(string id)
        {
            if (!queue.Results.ContainsKey(id)) queue.Results[id] = null;
        }

        public void GetQueue()
        {
            
        }

        public static void LoadImage(byte[] imageData, BaseContent c)
        {

            BitmapSource bs = new BitmapImage();
            LoadImage(imageData, ref bs);
            Execute.OnUIThread(() =>
            {
                c.Style.Picture = c.NEffectiveStyle.Picture = bs;
                c.TriggerUpdated();

            });
        }

        public  static void LoadImage(byte[] imageData, ref BitmapSource picture)
        {
            if (imageData == null || imageData.Length == 0) return;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            picture= image;
        }

        public static BitmapImage LoadPhoto(byte[] imageData)
        {
            //BitmapSource picture;
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        // TODO Folder and hash arguments are not used.
        public static bool SaveString(string folder, string id, string value, bool hash = false) 
        {
            try
            {
                File.WriteAllText(id, value);
                return true;
            }
            catch (Exception e)
            {
                Logger.Log("FileStore","Error saving string", e.Message,Logger.Level.Error);
                return false;
            }
        }

        public Byte[] GetBytes(string folder, string id, bool hash = false)
        {
            try
            {
                var f = GetLocalUrl(folder, id, hash);
                if (File.Exists(f))
                    return File.ReadAllBytes(f);
                LogCs.LogMessage(String.Format("Could not load file '{0}' from filestore.", f));
                return null;
            }
            catch (Exception ex)
            {
                LogCs.LogException("Filestore exception! ", ex);
                return null;
            }
            
        }

        public bool SaveBytes(string folder, string id, Byte[] image, bool hash = false) // REVIEW TODO fix: async removed
        {
            var p = folder + "\\" + id;
            return SaveBytes(p, image, hash);            
        }

        public bool SaveBytes(string file, Byte[] image, bool hash = false) // REVIEW TODO fix: async removed (the method is not asynchronous; also removed all the cascading asyncs in dependent code)
        {
            var p = file;
            if (image == null || image.Length == 0) return false;
            try
            {
                
                File.WriteAllBytes(p, image);
                return true;
            }
            catch (Exception e)
            {
                Logger.Log("FileStore", "Error writing file:" + p, e.Message, Logger.Level.Error);
                return false;
            }
        }

        public bool Empty()   // REVIEW TODO fix: async removed
        {
            // TODO Implement FileStore.Empty() ?
            return true;
        }

        public void AppendString(string p, string fileName, string s)
        {
            File.AppendAllText(fileName, s);  // UTF-8 is default.
        }

        public string GetLocalUrl(string folder,string Id, bool hash=false)
        {
            if (string.IsNullOrEmpty(folder)) return Id;
            return  folder + "\\" + Id;
        }

        public static bool FolderExists(string folder)
        {
            return (Directory.Exists(folder));
        }

        public static void CreateFolder(string folder)
        {
            Directory.CreateDirectory(folder);
        }

        internal static List<string> GetFiles(string folder, string p)
        {
            var plp = new DirectoryInfo(folder);
            var pp = plp.GetFiles(p);
            return pp.Select(k => k.FullName).ToList();
        }

        internal static List<string> GetFolders(string folder)
        {
            return Directory.GetDirectories(folder).ToList();
        }

        internal static bool FileExists(string file)
        {
            return (File.Exists(file));
        }

        internal static string GetLocalFolder()
        {
            return Directory.GetCurrentDirectory();
        }





        internal void Delete(string bck)
        {
            if (File.Exists(bck)) File.Delete(bck);
        }

        internal void Move(string fileName, string newName)
        {
            if (File.Exists(fileName)) File.Move(fileName, newName);
        }

        internal void Copy(string fileName, string newName)
        {
            if (File.Exists(fileName)) File.Copy(fileName, newName,true);
        }
    }
}
