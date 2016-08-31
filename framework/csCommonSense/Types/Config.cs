using Caliburn.Micro;
using ClosedXML.Excel;
using csCommon;
using csCommon.Logging;
using csShared.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace csShared
{
    public class Config : PropertyChangedBase
    {

        private string applicationName;

        private string configLocalString;
        private string configOnlineString;
        public Dictionary<string, string> localValues = new Dictionary<string, string>();

        //private bool offlineSupported;
        public Dictionary<string, string> OnlineValues = new Dictionary<string, string>();

        private readonly Dictionary<string, string> values = new Dictionary<string, string>();

        public int UserId { get; set; }

        public string ApplicationName
        {
            get { return applicationName; }
            set
            {
                applicationName = value;
                NotifyOfPropertyChange(() => ApplicationName);
            }
        }

        public string UserName { get; set; }


        public event EventHandler ConfigLoaded;

        public void SetLocalConfig(string name, string value)
        {
            SetLocalConfig(name, value, true);
        }

        public void SetLocalConfig(string name, string value, bool save)
        {
            localValues[name] = value;
            values[name] = value;
            if (save) SaveLocalConfig();
        }

        public void UpdateValues()
        {
            values.Clear();
            foreach (var a in OnlineValues) values[a.Key] = a.Value;
            foreach (var a in localValues) values[a.Key] = a.Value;
        }

        private void OverrideVariablesFromExcelSheet(FileInfo pConfigFile, string pConfigName)
        {
            try
            {
                if (!pConfigFile.Exists)
                {
                    LogCs.LogError(String.Format("Excel configuration file {0} not found, cannot apply configuration", pConfigFile.FullName));
                    Application.Current.Shutdown();
                    return;
                }
                using (var fs = new FileStream(pConfigFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var workbook = new XLWorkbook(fs);
                    var worksheet = workbook.Worksheets.FirstOrDefault();
                    // Get all configuration names
                    List<string> configNames = new List<string>();
                    var configNameFound = true;
                    var index = (byte)'B';
                    while (configNameFound)
                    {
                        string configName = worksheet.Cell((char)index + "3").Value.ToString();
                        index++;
                        if (!String.IsNullOrWhiteSpace(configName))
                        {
                            configNames.Add(configName);
                        }
                        else configNameFound = false;
                    }
                    if (!configNames.Contains(pConfigName))
                    {
                        LogCs.LogError(string.Format("Configuration '{0}' not found in excel sheet '{1}'; cannot apply config", pConfigName, pConfigFile.FullName));
                        Application.Current.Shutdown();
                        return;
                    }
                    char column = (char)((byte)'B' + (byte)configNames.FindIndex(x => String.Equals(x, pConfigName)));
                    // Apply 
                    bool foundEmptyRow = false;
                    int row = 4;
                    while (!foundEmptyRow)
                    {
                        var columnName = worksheet.Cell("A" + row).Value.ToString();
                        var columnValue = worksheet.Cell(column + row.ToString()).Value.ToString();
                        if (!String.IsNullOrWhiteSpace(columnName))
                        {
                            if (!columnName.StartsWith("#"))
                            {
                                LogCs.LogMessage(string.Format("Override config variable '{0}' with '{1}'", columnName, columnValue));
                                OnlineValues[columnName] = columnValue;
                            }
                        }
                        else foundEmptyRow = true;
                        row++;
                    }
                }
            } catch (Exception ex)
            {
                LogCs.LogException("Failed to apply excel configuration sheet: " + pConfigFile.FullName, ex);
            }
        }


        private static Dictionary<string, string> ParseValues(string e)
        {
            var result = new Dictionary<string, string>();
            try
            {
                var d = XDocument.Parse(e);
                var xElement = d.Element("root");
                if (xElement != null)
                    foreach (var a in xElement.Elements())
                    {
                        result[a.Name.LocalName] = a.Value;
                    }
                return result;
            }
            catch (Exception)
            {
                Logger.Log("Config", "Error parsing config string", e, Logger.Level.Error);
            }
            
            return result;
        }


        public static string LoadConfig(bool encrypted, string file)
        {
            var result = "";
            try
            {
                if (File.Exists(file))
                {
                    result = encrypted
                                 ? DecryptString(File.ReadAllText(file), "test")
                                 : File.ReadAllText(file);
                }
            }
            catch (Exception)
            {
                Logger.Log("CONFIG", "Error opening config file", null, Logger.Level.Error);
            }
            return result;
        }

        public static void SaveConfig(string file, string data, bool encrypt)
        {
            try
            {
                File.WriteAllText(file, encrypt
                                            ? EncryptString(data, "test")
                                            : data);
            }
            catch (Exception)
            {
                Logger.Log("CONFIG", "Error saving config file", null, Logger.Level.Error);
            }
        }


        public Brush GetBrush(string key, Brush @default)
        {
            try
            {
                if (values.ContainsKey(key))
                {
                    var convertFromString = ColorConverter.ConvertFromString(values[key]);
                    if (convertFromString != null) return new SolidColorBrush((Color)convertFromString);
                }
            }
            catch (Exception)
            {
                Logger.Log("CONFIG", "Error loading brush", key, Logger.Level.Error);
            }
            return @default;
        }

        public bool GetBool(string key, bool @default)
        {
            try
            {
                if (values.ContainsKey(key)) return Convert.ToBoolean(values[key]);
            }
            catch
            {
                Logger.Log("CONFIG", "Error loading bool", key, Logger.Level.Error);
            }
            return @default;
        }

        public void SetBool(string key, bool newValue)
        {
            values[key] = Convert.ToString(newValue);
        }


        public string Get(string key, string @default, bool storeOffline)
        {
            if (values.ContainsKey(key)) return values[key];
            if (storeOffline) SetLocalConfig(key, @default);
            return @default;
        }

        public void Set(string key, string newValue)
        {
            values[key] = newValue;
        }

        public string Get(string key, string @default)
        {
            return values.ContainsKey(key) ? values[key] : @default;
        }

        //        public E Get<E>(string key, E @default) where E : struct, IComparable, IConvertible, IFormattable // e: Enum is not allowed.
        //        {
        //            E e;
        //            return values.ContainsKey(key) ? Enum.TryParse(values[key], false, out e) ? e : @default : @default;
        //        }
        //
        //        public void Set<E>(string key, E @value) where E : struct, IComparable, IConvertible, IFormattable // e: Enum is not allowed.
        //        {
        //            values[key] = @value.ToString(CultureInfo.InvariantCulture);
        //        }

        public double GetDouble(string key, double @default)
        {
            return values.ContainsKey(key) ? Convert.ToDouble(values[key], CultureInfo.InvariantCulture) : @default;
        }

        public int GetInt(string key, int @default)
        {
            return values.ContainsKey(key) ? Convert.ToInt32(values[key]) : @default;
        }

        public static string EncryptString(string message, string passphrase)
        {
            byte[] results;
            var utf8 = new UTF8Encoding();

            // Step 1. We hash the passphrase using MD5
            // We use the MD5 hash generator as the result is a 128 bit byte array
            // which is a valid length for the TripleDES encoder we use below

            var hashProvider = new MD5CryptoServiceProvider();
            var tdesKey = hashProvider.ComputeHash(utf8.GetBytes(passphrase));

            // Step 2. Create a new TripleDESCryptoServiceProvider object
            var tdesAlgorithm = new TripleDESCryptoServiceProvider
            {
                Key = tdesKey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            // Step 3. Setup the encoder

            // Step 4. Convert the input string to a byte[]
            var dataToEncrypt = utf8.GetBytes(message);

            // Step 5. Attempt to encrypt the string
            try
            {
                var encryptor = tdesAlgorithm.CreateEncryptor();
                results = encryptor.TransformFinalBlock(dataToEncrypt, 0, dataToEncrypt.Length);
            }
            finally
            {
                // Clear the TripleDes and Hashprovider services of any sensitive information
                tdesAlgorithm.Clear();
                hashProvider.Clear();
            }

            // Step 6. Return the encrypted string as a base64 encoded string
            return Convert.ToBase64String(results);
        }

        public static string DecryptString(string message, string passphrase)
        {
            byte[] results;
            var utf8 = new UTF8Encoding();

            // Step 1. We hash the passphrase using MD5
            // We use the MD5 hash generator as the result is a 128 bit byte array
            // which is a valid length for the TripleDES encoder we use below

            var hashProvider = new MD5CryptoServiceProvider();
            var tdesKey = hashProvider.ComputeHash(utf8.GetBytes(passphrase));

            // Step 2. Create a new TripleDESCryptoServiceProvider object
            var tdesAlgorithm = new TripleDESCryptoServiceProvider
            {
                Key = tdesKey,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            // Step 3. Setup the decoder

            // Step 4. Convert the input string to a byte[]
            var dataToDecrypt = Convert.FromBase64String(message);

            // Step 5. Attempt to decrypt the string
            try
            {
                var decryptor = tdesAlgorithm.CreateDecryptor();
                results = decryptor.TransformFinalBlock(dataToDecrypt, 0, dataToDecrypt.Length);
            }
            finally
            {
                // Clear the TripleDes and Hashprovider services of any sensitive information
                tdesAlgorithm.Clear();
                hashProvider.Clear();
            }

            // Step 6. Return the decrypted string in UTF8 format
            return utf8.GetString(results);
        }


        internal static void LogOff()
        {
            Logger.Log("Config", "Log Off", "", Logger.Level.Info);
            if (File.Exists("config.xml"))
            {
                File.Delete("config.xml");
            }
        }

        #region local config

        private void ApplyExcelConfigSheet()
        {
            
            var options = new ConfigCmdLineOptions();
            if (CommandLine.Parser.Default.ParseArguments(Environment.GetCommandLineArgs(), options))
            {
                if (!String.IsNullOrEmpty(options.ConfigurationFile) &&
                    !String.IsNullOrEmpty(options.ConfigurationName))
                {
                    OverrideVariablesFromExcelSheet(new FileInfo(options.ConfigurationFile), options.ConfigurationName);
                }
            }
        }


        public void LoadOfflineConfig()
        {
            configOnlineString = LoadConfig(false, "configoffline.xml"); // TODO REVIEW (1) Config ONLINE string loads Config OFFLINE; (2) the name of the Config Offline file is specified in App.config, but that specification is ignored here.
            if (!string.IsNullOrEmpty(configOnlineString))
                OnlineValues = ParseValues(configOnlineString);
            ApplyExcelConfigSheet();
            if (ConfigLoaded != null) ConfigLoaded(this, null);
            //Logger.Log("Config", "Loaded Offline Config", "", Logger.Level.Info);
        }

        public void LoadLocalConfig()
        {
            //Logger.Log("Config", "Loading Local Config", "", Logger.Level.Info);
            configLocalString = LoadConfig(false, "localconfig.xml");
            if (configLocalString == "")
            {
                SaveLocalConfig();
            }
            else
            {
                localValues = ParseValues(configLocalString);
                if (localValues.Count == 0) SaveLocalConfig();
            }
        }

        private void SaveLocalConfig()
        {
            Logger.Log("Config", "Saving Local Config", "", Logger.Level.Info);

            var config = new XDocument(new XElement("root", from localValue in localValues select new XElement(localValue.Key, localValue.Value)));
            SaveConfig("localconfig.xml", config.ToString(), false);

            //configLocalString = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?><root>";
            //foreach (var s in localValues)
            //{
            //    configLocalString += "<" + s.Key + ">" + s.Value + "</" + s.Key + ">" + Environment.NewLine;
            //}
            //configLocalString += "</root>";
            //SaveConfig("localconfig.xml", configLocalString, false);
        }

        public IEnumerable<KeyValuePair<string, string>> GetTagIds()
        {
            return values.Where(k => k.Key.ToLower().EndsWith(".tagid"));
        }

        #endregion
    }
}