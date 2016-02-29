using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;

namespace nl.tno.cs.presenter
{
    public class MetroFolderList : Control
    {
        static MetroFolderList()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MetroFolderList), new FrameworkPropertyMetadata(typeof(MetroFolderList)));

        }



        public Visibility TitleVisibility
        {
            get { return (Visibility)GetValue(TitleVisibilityProperty); }
            set { SetValue(TitleVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TitleVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleVisibilityProperty =
            DependencyProperty.Register("TitleVisibility", typeof(Visibility), typeof(MetroFolderList), new UIPropertyMetadata(Visibility.Visible));

        


        public BindableCollection<ItemClass> Items
        {
            get { return (BindableCollection<ItemClass>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(BindableCollection<ItemClass>), typeof(MetroFolderList), new UIPropertyMetadata(null));

        public List<FolderTemplate> Templates { get; set; }

      

        public void GetFiles()
        {

           

            var l = new List<ItemClass>();
            Items = new BindableCollection<ItemClass>();

            if (Folder!=null && Folder.Directory!=null && Folder.Directory.Exists)
            {


                l = Folder.Explorer.GetItems(Folder.Directory.FullName).Where(k => k.Type != ItemType.folder && k.Visible && k.ImagePath!=null).ToList();

                foreach (var i in l)
                {
                    i.Explorer = Folder.Explorer;
                }
                    
                ItemsControl _slb = GetTemplateChild("AllItems") as ItemsControl;
                _slb.ItemsSource = l;
                

            }
        }


        public Folder Folder
        {
            get { return (Folder)GetValue(FolderProperty); }
            set { SetValue(FolderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Folder.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FolderProperty =
            DependencyProperty.Register("Folder", typeof(Folder), typeof(MetroFolderList), new UIPropertyMetadata(null));


        

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            //if (!Folder.Templated) this.Style = Application.Current.FindResource("MetroFolderList") as Style;
            GetFiles();
            if (this.Folder != null && Folder.Explorer != null)
            {
                TitleVisibility = Folder.TitleVibility;
                this.Folder.Explorer.Refreshed += new EventHandler(Explorer_Refreshed);

            }
        }

        void Explorer_Refreshed(object sender, EventArgs e)
        {
            GetFiles();
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);            
        }
    
    }
}