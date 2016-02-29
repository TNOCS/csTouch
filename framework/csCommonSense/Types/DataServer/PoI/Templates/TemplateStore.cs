using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using csCommon.Utils.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;

namespace csCommon.Types.DataServer.PoI.Templates
{
    public enum SupportedTemplateExtensions { txml, tjson };

    public class TemplateStore<TTemplate> where TTemplate : class, ITemplateObject, new()
    {
        private FileLocation _fileRoot;
        private Dictionary<string, FileLocation> _templateFiles; // Name -> file
        private Dictionary<string, Dictionary<string, TTemplate>> _templates; // Name -> id -> template
        private Dictionary<string, List<string>> _templateCollectionNamesForId; // Template id -> names.
        private TTemplate _targetTemplate;

        public TemplateStore(FileLocation templateFileRoot = null)
        {
            _fileRoot = templateFileRoot ?? new FileLocation(".");
            Refresh();
        }

        public void Refresh()
        {
            _templateFiles = new Dictionary<string, FileLocation>();
            _templates = new Dictionary<string, Dictionary<string, TTemplate>>();
            _templateCollectionNamesForId = new Dictionary<string, List<string>>();

            // AssemblyClassEnumerator<TTemplate> supportedObjects = new AssemblyClassEnumerator<TTemplate>();
            _targetTemplate = new TTemplate(); // First(supportedObjects);

            // Gather all template files.
            ReadTemplates(_fileRoot);
        }

        /// <summary>
        /// Convert all templates to a certain extension.
        /// </summary>
        /// <param name="templateExtension">The extension to convert to.</param>
        public void ConvertTemplates(SupportedTemplateExtensions templateExtension)
        {
            Dictionary<TTemplate, string> templates = new Dictionary<TTemplate, string>();
            foreach (KeyValuePair<string, Dictionary<string, TTemplate>> collectionNameToDict in _templates)
            {
                string templateCollectionName = collectionNameToDict.Key;
                foreach (KeyValuePair<string, TTemplate> idToTemplate in collectionNameToDict.Value)
                {
                    TTemplate template = idToTemplate.Value;
                    templates[template] = templateCollectionName;
                }
            }
            UpdateTemplates(templates, templateExtension);
            DeleteFiles(_fileRoot, templateExtension);
        }

        private static void DeleteFiles(FileLocation path, SupportedTemplateExtensions exceptionExtension)
        {
            IEnumerable<string> files = Directory.EnumerateFiles(path.LocationString);
            foreach (var file in files)
            {
                string extension = Path.GetExtension(file) ?? "";
                if (extension.EndsWith(exceptionExtension.ToString()))
                {
                    continue;
                }
                File.Delete(file);
            }
            IEnumerable<string> directories = Directory.EnumerateDirectories(path.LocationString);
            foreach (var directory in directories)
            {
                DeleteFiles(new FileLocation(directory), exceptionExtension);
            }
        }

        private void ReadTemplates(FileLocation root)
        {
            string[] files = Directory.GetFiles(root.LocationString, "*.txml");
            foreach (var templateFile in files)
            {
                try
                {
                    string templateName;
                    Dictionary<string, TTemplate> templatesInFile = LoadXmlFile(new FileLocation(templateFile), out templateName);
                    ProcessTemplateFile(templateName, templateFile, templatesInFile);
                }
                // ReSharper disable once EmptyGeneralCatchClause
                // ReSharper disable once UnusedVariable
                catch (Exception e) { } // Ignore.
            }

            files = Directory.GetFiles(root.LocationString, "*.tjson");
            foreach (var templateFile in files)
            {
                try
                {
                    string templateName;
                    Dictionary<string, TTemplate> templatesInFile = LoadJsonFile(new FileLocation(templateFile), out templateName);
                    ProcessTemplateFile(templateName, templateFile, templatesInFile);
                }
                // ReSharper disable once EmptyGeneralCatchClause
                // ReSharper disable once UnusedVariable
                catch (Exception e) { } // Ignore.
            }

            string[] directories = Directory.GetDirectories(root.LocationString);
            foreach (var directory in directories)
            {
                try
                {
                    ReadTemplates(new FileLocation(Path.Combine(root.LocationString, directory)));
                }
                // ReSharper disable once EmptyGeneralCatchClause
                // ReSharper disable once UnusedVariable
                catch (Exception e) { } // Ignore.
            }
        }

        private void ProcessTemplateFile(string templateCollectionName, string templateFile, Dictionary<string, TTemplate> templatesInFile)
        {
            if (_templateFiles.ContainsKey(templateCollectionName))
            {
                return;
            }
            _templateFiles[templateCollectionName] = new FileLocation(templateFile);
            _templates[templateCollectionName] = templatesInFile;
            foreach (KeyValuePair<string, TTemplate> keyValuePair in templatesInFile)
            {
                TTemplate templateObject = keyValuePair.Value;
                string id = templateObject.Id;
                if (!_templateCollectionNamesForId.ContainsKey(id))
                {
                    _templateCollectionNamesForId[id] = new List<string>();
                }
                _templateCollectionNamesForId[id].Add(templateCollectionName);
            }
        }

        /// <summary>
        /// Return the known template ids. For example, we may have defined a template for the item "Oppervlakte" denoting that this is a number in square meters. Such an id may be defined in multiple template collections, identified by their names.
        /// </summary>
        /// <returns>Just that.</returns>
        public IEnumerable<string> GetTemplateIds()
        {
            return new List<string>(_templateCollectionNamesForId.Keys);
        }
        
        /// <summary>
        /// Returns the names of the template collections (e.g. "WMO", "Gemeentes") storing a template for a certain id (e.g. "Oppervlakte"). 
        /// </summary>
        /// <param name="templateId">The template id we are interested in.</param>
        /// <returns>Template collection names having a template for this id.</returns>
        public IEnumerable<string> GetTemplateCollectionNames(string templateId)
        {
            List<string> templateCollectionNames;
            if (_templateCollectionNamesForId.TryGetValue(templateId, out templateCollectionNames))
            {
                return templateCollectionNames;
            }
            return new List<String>();
        }
        
        /// <summary>
        /// Returns the names of the template collections (e.g. "WMO", "Gemeentes"). 
        /// </summary>
        /// <returns>Template collection names.</returns>
        public IEnumerable<string> GetTemplateCollectionNames()
        {
            HashSet<string> templateNames = new HashSet<string>();
            foreach (string templateId in _templateCollectionNamesForId.Keys)
            {
                List<string> names;
                if (_templateCollectionNamesForId.TryGetValue(templateId, out names))
                {
                    templateNames.UnionWith(names);
                }                
            }
            return templateNames;
        }

        /// <summary>
        /// Returns the template within the given collection, for the given id.
        /// </summary>
        /// <param name="templateCollectionName">The collection name.</param>
        /// <param name="templateId">The template id.</param>
        /// <returns>The template.</returns>
        public TTemplate GetTemplate(string templateCollectionName, string templateId)
        {
            Dictionary<string, TTemplate> templateColl;
            if (_templates.TryGetValue(templateCollectionName, out templateColl))
            {
                TTemplate template;
                if (templateColl.TryGetValue(templateId, out template))
                {
                    return template;
                }
            }
            return null;
        }

        public void UpdateTemplates(Dictionary<TTemplate, string> updatedTemplates, SupportedTemplateExtensions format = SupportedTemplateExtensions.tjson)
        {
            Dictionary<string, List<TTemplate>> templatesPerCollectionName = new Dictionary<string, List<TTemplate>>();
            foreach (KeyValuePair<TTemplate, string> templateToCollectionName in updatedTemplates)
            {
                TTemplate template = templateToCollectionName.Key;
                string templateCollectionName = templateToCollectionName.Value;
                if (templateCollectionName == null) continue; // Do not update this template.                

                if (!templatesPerCollectionName.ContainsKey(templateCollectionName))
                {
                    templatesPerCollectionName[templateCollectionName] = new List<TTemplate>();
                }
                templatesPerCollectionName[templateCollectionName].Add(template);
            }

            foreach (KeyValuePair<string, List<TTemplate>> collectionNameToTemplateList in templatesPerCollectionName)
            {
                string templateCollectionName = collectionNameToTemplateList.Key;
                List<TTemplate> updatedTemplatesList = collectionNameToTemplateList.Value;

                FileLocation file;
                bool templateFileKnown = _templateFiles.TryGetValue(templateCollectionName, out file);
                if (file != null && Path.GetExtension(file.LocationString) != "." + format.ToString())
                {
                    templateFileKnown = false; // We need to refer to the converted file.
                }
                if (!templateFileKnown)
                {
                    file = new FileLocation(Path.Combine(Path.GetFullPath(_fileRoot.LocationString), templateCollectionName + "." + format.ToString()));
                    _templateFiles[templateCollectionName] = file;
                }

                string fileTemplateName;
                Dictionary<string, TTemplate> fileTemplates = LoadFile(file, out fileTemplateName);

                foreach (TTemplate updatedTemplate in updatedTemplatesList)
                {
                    fileTemplates[updatedTemplate.Id] = updatedTemplate;
                }

                SaveFile(file, fileTemplates);   
            }
        }

        private Dictionary<string, TTemplate> LoadFile(FileLocation templateFile, out string templateName)
        {
            switch (Path.GetExtension(templateFile.LocationString))
            {
                case ".txml":
                    return LoadXmlFile(templateFile, out templateName);
                    break;
                case ".tjson":
                    return LoadJsonFile(templateFile, out templateName);
                    break;
                default: throw new Exception(string.Format("Cannot load templates from file '{0}'!", templateFile.LocationString));
            }
        }

        private Dictionary<string, TTemplate> LoadXmlFile(FileLocation templateFile, out string templateName)
        {
            templateName = null;
            Dictionary<string, TTemplate> templates = new Dictionary<string, TTemplate>();
            try
            {
                XElement fileContent = XElement.Load(templateFile.LocationString, LoadOptions.None);
                templateName = fileContent.Attribute("name").Value;
                IEnumerable<XElement> descendants = fileContent.Descendants(_targetTemplate.XmlNodeId);
                foreach (var descendant in descendants)
                {
                    try
                    {
                        TTemplate templateObject = new TTemplate(); 
                        templateObject.FromXml(descendant);
                        string id = templateObject.Id;
                        templates[id] = templateObject;
                    }
                    // ReSharper disable once EmptyGeneralCatchClause
                    // ReSharper disable once UnusedVariable
                    catch (Exception e) { } // Ignore.
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            // ReSharper disable once UnusedVariable
            catch (Exception e) {  } // Ignore.
            return templates;            
        }

        private Dictionary<string, TTemplate> LoadJsonFile(FileLocation templateFile, out string templateName)
        {
            templateName = null;
            Dictionary<string, TTemplate> templates = new Dictionary<string, TTemplate>();
            try
            {
                using (StreamReader reader = new StreamReader(templateFile.LocationString))
                {
                    string content = reader.ReadToEnd();
                    JObject root = JObject.Parse(content);
                    templateName = root["name"].ToObject<string>();
                    JToken templateObject = root["templates"];
                    foreach (JToken descendant in templateObject.Children())
                    {
                        try
                        {
                            TTemplate template = new TTemplate();
                            JProperty p = descendant as JProperty;
                            if (p != null)
                            {
                                JObject o = p.Value as JObject;
                                if (o != null)
                                {
                                    template.FromGeoJson(o, false); // Into object.                                    
                                }
                            }
                            string id = template.Id;
                            templates[id] = template;
                        }
                            // ReSharper disable once EmptyGeneralCatchClause
                            // ReSharper disable once UnusedVariable
                        catch (Exception e)
                        {
                        } // Ignore.
                    }
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            // ReSharper disable once UnusedVariable
            catch (Exception e) { } // Ignore.
            return templates;
        }

        private void SaveFile(FileLocation file, Dictionary<string, TTemplate> templates)
        {
            switch (Path.GetExtension(file.LocationString))
            {
                case ".txml": 
                    SaveXmlFile(file, templates);
                    break;
                case ".tjson":
                    SaveJsonFile(file, templates);
                    break;
            }
        }

        private static void SaveXmlFile(FileLocation file, Dictionary<string, TTemplate> templates)
        {
            string templateName = Path.GetFileNameWithoutExtension(file.LocationString);
            XElement root = new XElement(XName.Get("Templates"));
            root.SetAttributeValue(XName.Get("name"), templateName ?? "Default");
            foreach (KeyValuePair<string, TTemplate> keyValuePair in templates)
            {
                TTemplate template = keyValuePair.Value;
                root.Add(template.ToXml());
            }
            root.Save(file.LocationString);
        }

        private static void SaveJsonFile(FileLocation file, Dictionary<string, TTemplate> templates)
        {
            string templateName = Path.GetFileNameWithoutExtension(file.LocationString);
            JObject root = new JObject();
            root["name"] = templateName ?? "Default";
            JObject templateObject = new JObject();
            StringBuilder templateKeys = new StringBuilder();
            foreach (KeyValuePair<string, TTemplate> keyValuePair in templates)
            {
                TTemplate template = keyValuePair.Value;
                JToken jToken = JToken.Parse(template.ToGeoJson());
                templateObject[template.Id] = jToken;
                templateKeys.Append(template.Id).Append(';');
            }
            templateKeys.Remove(templateKeys.Length - 1, 1); // Remove the last ;
            root["templateKeys"] = templateKeys.ToString();
            root["templates"] = templateObject;
            string fileContent = root.ToString(Formatting.Indented); // Much easier to read.
            using (StreamWriter writer = new StreamWriter(file.LocationString))
            {
                writer.Write(fileContent);
            }
        }
    }
}
