using Caliburn.Micro;
using csShared;
using csShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Speech.Recognition;

namespace csCommon.Plugins.SpeechPlugin
{
    [Export(typeof(IPlugin))]
    public class SpeechPlugin : PropertyChangedBase, IPlugin
    {
        private readonly MenuItem speechMenuItem = new MenuItem();
        private Grammar g;
        private bool hideFromSettings;
        private bool isRunning;
        //private object l = new object();
        private SpeechRecognitionEngine recognizer;
        private bool reset;
        private IPluginScreen screen;
        private ISettingsScreen settings;

        //private DispatcherTimer timer;
        private bool waiting;
        public FloatingElement Element { get; set; }

        public bool Enabled
        {
            get { return AppState.Config.GetBool("Speech.Enabled", false); }
            set { AppState.Config.SetLocalConfig("Speech.Enabled", value.ToString()); }
        }

        public bool CanStop
        {
            get { return true; }
        }

        public ISettingsScreen Settings
        {
            get { return settings; }
            set
            {
                settings = value;
                NotifyOfPropertyChange(() => Settings);
            }
        }

        public IPluginScreen Screen
        {
            get { return screen; }
            set
            {
                screen = value;
                NotifyOfPropertyChange(() => Screen);
            }
        }

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set
            {
                hideFromSettings = value;
                NotifyOfPropertyChange(() => HideFromSettings);
            }
        }

        public int Priority
        {
            get { return 1; }
        }

        public string Icon
        {
            get { return @"/csCommon;component/Resources/Icons/microphone.png"; }
        }


        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                isRunning = value;
                NotifyOfPropertyChange(() => IsRunning);
            }
        }

        public AppStateSettings AppState { get; set; }

        public string Name
        {
            get { return "SpeechPlugin"; }
        }

        public SpeechConfigViewModel Config { get; set; }

        public void Init()
        {
            Config = new SpeechConfigViewModel { DisplayName = "Speech" };
        }

        /// <summary>
        /// Plugin started
        /// </summary>
        public void Start()
        {
            IsRunning = true;
            if (Enabled)
            {
                StartSpeech();
            }
            else
            {
                StopSpeech();
            }
            AppState.ConfigTabs.Add(Config);
            speechMenuItem.Clicked += MainMenuClicked;
            AppState.MainMenuItems.Add(speechMenuItem);
        }

        public void Pause()
        {
            IsRunning = false;
        }

        /// <summary>
        ///     stop plugin
        /// </summary>
        public void Stop()
        {
            StopSpeech();
            speechMenuItem.Clicked -= MainMenuClicked;
            AppState.Commands.CollectionChanged -= CommandsChanged;
            g = null;
            IsRunning = false;
            AppState.ConfigTabs.Remove(Config);
            AppState.MainMenuItems.Remove(speechMenuItem);

        }

        private void MainMenuClicked(object sender, EventArgs e)
        {
            Enabled = !Enabled;
            if (Enabled)
            {
                StartSpeech();
            }
            else
            {
                AppState.TriggerNotification("Speech engine stopped");
                StopSpeech();
            }
        }

        private void StopSpeech()
        {
            speechMenuItem.Name = "Start Speech";
            if (recognizer != null)
            {
                recognizer.RecognizeCompleted -= RecognizeCompleted;
                recognizer.SpeechRecognized -= SpeechRecognized;

                recognizer.UnloadAllGrammars();
                recognizer.RecognizeAsyncStop();
                recognizer = null;
            }
        }

        private void StartSpeech()
        {
            //ThreadPool.QueueUserWorkItem(
            //    delegate
            {

                speechMenuItem.Name = "Stop Speech";
                recognizer = new SpeechRecognitionEngine(new CultureInfo("en-US"));
                AppState.Commands.AddCommand("Exit", new[] { "Exit" }, null);

                try
                {
                    //button1.Text = "Speak Now";
                    recognizer.SetInputToDefaultAudioDevice();
                    UpdateGrammar();
                    recognizer.RecognizeCompleted += RecognizeCompleted;
                    recognizer.SpeechRecognized += SpeechRecognized;
                    recognizer.RecognizeAsync(RecognizeMode.Multiple);


                    //RecognitionResult result = recognizer.Recognize();
                    //button1.Text = result.Text;
                }
                catch (InvalidOperationException exception)
                {
                    AppState.TriggerNotification(
                        String.Format(
                            "Could not recognize input from default aduio device. Is a microphone or sound card available?\r\n{0} - {1}.",
                            exception.Source, exception.Message));
                }

                AppState.Commands.CollectionChanged += CommandsChanged;
                UpdateGrammar();
                // AppState.TriggerNotification("Speech engine started");
            }
            //);
        }


        private void CommandsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            reset = true;
            waiting = true;
            UpdateGrammar();
        }


        /// <summary>
        ///     Update grammar list
        /// </summary>
        public void UpdateGrammar()
        {
            if (!Enabled) return;
            if (waiting)
            {
                recognizer.RecognizeAsyncStop();
                waiting = false;
                return;
            }
            recognizer.UnloadAllGrammars();
            var sentences = new Choices();
            IEnumerable<string> ss = from c in AppState.Commands
                                     from s in c.Sentences
                                     select s;
            foreach (string s in ss.GroupBy(k => k).Select(k => k.Key))
            {
                sentences.Add(s);
            }
            var gBuilder = new GrammarBuilder(sentences);

            g = new Grammar(gBuilder);

            recognizer.LoadGrammar(g);
            //recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        /// <summary>
        ///     something was recognized, search and trigger handlers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence < 0.7) return;
            AppState.TriggerNotification(e.Result.Text + " (" + e.Result.Confidence + ")");
            foreach (Command c in AppState.Commands)
            {
                if (c.Sentences.Contains(e.Result.Text))
                {
                    if (c.Handler != null) c.Handler(c.Id);
                }
            }
        }

        /// <summary>
        ///     Recognition was stoped, check if it was reset and needs to be started again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Cancelled && reset)
            {
                waiting = false;
                reset = false;
                UpdateGrammar();
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
            }
        }
    }
}