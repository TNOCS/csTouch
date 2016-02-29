using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Caliburn.Micro;



namespace nl.tno.cs.presenter
{
    public class FolderTemplate
    {
        public List<ItemTemplate> Items = new List<ItemTemplate>();
    }

    public class ItemTemplate
    {
        public int Col, Row, Colspan, Rowspan = 0;
    }

    public interface IFolderView
    {
    }

    public class Item : PropertyChangedBase
    {
        
    }

    public static class PresenterExtensions
    {
        public static string CleanName(this string name)
        {
            string s = name;
            while (s.Length > 0 && (Regex.IsMatch(s[0].ToString(), @"\d") || s[0] == ' ' || s[0] == '.')) s = s.Remove(0, 1);
            s = s.Replace("~", "").Replace("!","").Replace("+","").Replace("^","").Trim(' ');
            return s;
        }

        public static string UriId(this Uri u)
        {
            return "temp";
        }

        
    }

    
}
