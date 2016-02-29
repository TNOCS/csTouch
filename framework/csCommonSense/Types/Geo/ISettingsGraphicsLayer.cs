#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ESRI.ArcGIS.Client;

#endregion

namespace csShared.Geo
{
    public interface ILayerWithParentLayer
    {
        GroupLayer Parent { get; set; }
    }

    public interface IIgnoreLayer
    {}

    public interface ILayerWithMoreChildren
    {
        ObservableCollection<Layer> Children { get; set; }
    }

    public interface ITabLayer
    {
        bool IsTabActive { get; set; }
        void OpenTab(int index = 0);
        void CloseTab();
    }

    public interface IOnlineLayer
    {
        bool IsOnline { get; }
        bool IsShared { get; }
    }

    public interface IMenuLayer
    {
        List<System.Windows.Controls.MenuItem> GetMenuItems();
    }

    public interface IStartStopLayer
    {
        bool   IsStarted    { get; set; }
        string State        { get; set; }
        bool   CanStart     { get; set; }
        bool   CanStop      { get; set; }
        bool   IsLoading    { get; set; }
        void   Start(bool   share = false);
        void   Stop();
        event  EventHandler Started;
    }

    public interface ISettingsLayer
    {
        bool CanRemove { get; set; }
        void StartSettings();
    }

    public abstract class SettingsGraphicsLayer : GraphicsLayer, ISettingsLayer
    {
        public SettingsGraphicsLayer() {
            CanRemove = false;
        }

        public bool CanRemove { get; set; }

        public abstract void StartSettings();
    }

    public abstract class SettingsElementLayer : ElementLayer, ISettingsLayer
    {
        public SettingsElementLayer() {
            CanRemove = false;
        }

        public bool CanRemove { get; set; }

        public abstract void StartSettings();
    }
}