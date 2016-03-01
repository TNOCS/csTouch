using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace csCommon.Utils.IO
{
    public class AssemblyClassEnumerator<T> : IEnumerable<T> where T : class // TODO REVIEW: new()  constraint would be required here, but then this propagates through all classes.
    {
        IEnumerator IEnumerable.GetEnumerator()
        {
            return AssemblyClasses.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return AssemblyClasses.GetEnumerator();
        }

        private static ObservableCollection<T> _assemblyClasses; 
        public ObservableCollection<T> AssemblyClasses
        {
            get {
                if (_assemblyClasses == null)
                {
                    _assemblyClasses = new ObservableCollection<T>();
                    Refresh();    
                }
                return _assemblyClasses;
            }
            private set { _assemblyClasses = value;  }
        }

        public void Refresh()
        {
            AssemblyClasses.Clear();

            Type ti = typeof(T);
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type t in asm.GetTypes())
                    {
                        if (!t.IsAbstract) // Cannot instance an abstract class
                        {
                            if (ti.IsAssignableFrom(t) ||
                                (ti.IsGenericType && IsAssignableToGenericType(t, ti)))
                            {
                                try
                                {
                                    var obj = (T) Activator.CreateInstance(t);
                                    AssemblyClasses.Add(obj);
                                }
                                catch (Exception e)
                                {
                                    // Ignore.
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Ignore.
                }
            }
        }

        private static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            // TODO The code below does not fully work.
            // Example: ConvertGeoJson, which implements IImporter<string, PoiService>, is not recognized as IImporter<dynamic, PoiService>
            // http://stackoverflow.com/questions/74616/how-to-detect-if-type-is-another-generic-type/1075059#1075059

            var interfaceTypes = givenType.GetInterfaces();
            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            Type baseType = givenType.BaseType;
            if (baseType == null) return false;

            return IsAssignableToGenericType(baseType, genericType);
        }
    }
}