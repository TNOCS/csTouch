using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using csGeoLayers.Plugins.DemoScript;
using csShared.Utils;
using nl.tno.cs.presenter;

namespace csPresenterPlugin.Utils
{
    public class PlaylistManager
    {
        public List<ItemClass> Items { get; set; }
        private int _itemPointer = 0;
        private Thread _scriptThread = null;
        private DemoScript _demoScript;
        public bool ScriptRunning = false;

        public delegate void ItemFinished(ItemClass ic);
        public event ItemFinished ItemFinishedEvent;

        public delegate void PlaylistFinished();
        public event PlaylistFinished PlaylistFinishedEvent;

        public delegate void PlaylistStarted();
        public event PlaylistStarted PlaylistStartedEvent;

        private bool _loop = false;

        public bool Loop
        {
            get { return _loop; }
            set { _loop = value; }
        }
        

        public ItemClass CurrentItem
        {get
        {
            if (Items != null && _itemPointer < Items.Count)
                return Items[_itemPointer];
            return null;
        }
        }

        public PlaylistManager(DemoScript ds)
        {
            _demoScript = ds;
            Items = new List<ItemClass>();
            if (_demoScript != null)
            {
                _demoScript.ScriptFinished += _demoScript_ScriptFinished;
            }
            Application.Current.Exit += Current_Exit;   
        
        }

        public void Play()
        {
            if(Items != null && Items.Count > 0)
            {
                StopThread();
                if (_itemPointer > (Items.Count - 1))
                    _itemPointer = 0;                
                StartThread(Items[_itemPointer].Path);
                
                if(PlaylistStartedEvent != null)
                {
                    PlaylistStartedEvent();
                }
            }
        }

        public void Play(ItemClass itm)
        {
            int idx = 0;
            foreach(var itmL in Items)
            {
                if(itmL.Path == itm.Path)
                {
                    _itemPointer = idx;
                    Play();
                    return;
                }
                idx++;
            }
        }

        public void Next()
        {
            StopThread();
            //_itemPointer++;
            //while (_itemPointer > (Items.Count - 1))
            //    _itemPointer--;
            //StartThread(Items[_itemPointer].Path);
        }

        public void Previous()
        {
            StopThread();
            _itemPointer--;
            if (_itemPointer < 0)
                _itemPointer = 0;
            StartThread(Items[_itemPointer].Path);
        }

        public void Pause()
        {
            
        }

        public void Stop()
        {
            StopThread();
        }

        public void AddItem(ItemClass item)
        {
            Items.Add(item);
        }

        public void ClearList()
        {
            Items.Clear();
            _itemPointer = 0;
        }

        /*private void PauseThread()
        {
            if(_scriptThread != null && _scriptThread.IsAlive)
            {
                _scriptThread
            }
        }*/

        private void StopThread()
        {
            if (_scriptThread != null && _scriptThread.IsAlive)
            {
                try
                {
                    _demoScript.Stop();
                    //_scriptThread.Abort();
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public void StartThread(string script)
        {
            try
            {
                StopThread();
                _demoScript.ScriptPath = script;
                _scriptThread = new Thread(new ThreadStart(_demoScript.StartScript));

                _demoScript.ScriptStarted += (e, s) => ScriptRunning = true;

                _scriptThread.Start();
            }
            catch (Exception e)
            {
                
                Logger.Log("Script Starter","Error starting script",e.Message,Logger.Level.Error);
            }
            
        }

        void Current_Exit(object sender, ExitEventArgs e)
        {
            Stop();
        }
        void _demoScript_ScriptFinished(object sender, EventArgs e)
        {
            ScriptRunning = true;
            _itemPointer++;
            if(Items != null && (Items.Count - 1 < _itemPointer))
            {
                // Finished
                _itemPointer = 0;
                ScriptRunning = false;
            }
            if (ItemFinishedEvent != null)
            {
                if (ScriptRunning)
                {
                    ItemFinishedEvent(Items[_itemPointer]);
                    // Play next item
                    Play();
                }
                else
                {
                    // Always enable touch after playlist finished
                    _demoScript.RemoveMapConstraint();
                    //_demoScript.EnableTouch();
                    ItemFinishedEvent(null);
                    if (!_demoScript.KeepScriptActive)
                    {
                        if (!Loop)
                        {
                            Items.Clear();
                            PlaylistFinishedEvent();
                        }
                        else
                        {
                            _itemPointer = 0;
                            Play();
                        }


                    }
                    else
                    {
                        _demoScript.KeepScriptActive = false;
                    }
                    


                }
            }

        }
       
    }
}
