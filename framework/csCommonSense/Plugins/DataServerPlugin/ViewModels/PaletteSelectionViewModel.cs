#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Caliburn.Micro;
using csShared;
using csShared.Interfaces;
using PoiServer.PoI;

#endregion

namespace csDataServerPlugin
{
    public class PaletteInputPopupEventArgs : EventArgs
    {
        public Palette Result;
    }

    [Export(typeof (IPopupScreen))]
    public class PaletteSelectionViewModel : Screen, IPopupScreen
    {
        private bool autoClose = true;
        private Brush background = Brushes.White;
        private Brush border = Brushes.Black;
        private Point point;
        private FrameworkElement relativeElement;
        private Point relativePosition;
        private VerticalAlignment verticalAlignment;
        private ObservableCollection<Palette> palettes;
        private Palette selectedPalette;
        private string title;
        private DispatcherTimer toTimer;
        private PaletteSelectionView view;
        private double width;

        public AppStateSettings AppState {
            get { return AppStateSettings.Instance; }
        }

        public ObservableCollection<Palette> Palettes {
            get { return palettes; }
            set {
                palettes = value;
                NotifyOfPropertyChange(() => Palettes);
            }
        }


        public bool AutoClose {
            get { return autoClose; }
            set { autoClose = value; }
        }

        public VerticalAlignment VerticalAlignment {
            get { return verticalAlignment; }
            set {
                verticalAlignment = value;
                UpdatePosition();
                NotifyOfPropertyChange(() => VerticalAlignment);
            }
        }

        public Point Point {
            get { return point; }
            set {
                point = value;
                NotifyOfPropertyChange(() => Point);
            }
        }

        public string Title {
            get { return title; }
            set {
                title = value;
                NotifyOfPropertyChange(() => Title);
            }
        }

        public Brush Background {
            get { return background; }
            set {
                background = value;
                NotifyOfPropertyChange(() => Background);
            }
        }

        public FrameworkElement RelativeElement {
            get { return relativeElement; }
            set {
                relativeElement = value;
                UpdatePosition();

                RelativeElement.LayoutUpdated += RelativeElement_LayoutUpdated;
                NotifyOfPropertyChange(() => RelativeElement);
            }
        }

        public TimeSpan? TimeOut { get; set; }

        public Point RelativePosition {
            get { return relativePosition; }
            set {
                relativePosition = value;
                UpdatePosition();
                NotifyOfPropertyChange(() => RelativePosition);
            }
        }

        public Brush Border {
            get { return border; }
            set {
                border = value;
                NotifyOfPropertyChange(() => Border);
            }
        }

        public Palette SelectedPalette {
            get { return selectedPalette; }
            set {
                selectedPalette = value;
                Save();
            }
        }

        public double Width {
            get { return width; }
            set {
                width = value;
                NotifyOfPropertyChange(() => Width);
            }
        }

        public event EventHandler<PaletteInputPopupEventArgs> Saved;

        public void Close() {
            AppState.Popups.Remove(this);
        }

        private void RelativeElement_LayoutUpdated(object sender, EventArgs e) {
            UpdatePosition();
        }


        protected override void OnViewLoaded(object theView) {
            base.OnViewLoaded(theView);
            view = (PaletteSelectionView) theView;

            UpdatePosition();

            Palettes = new ObservableCollection<Palette> {
                new Palette {
                    Type = PaletteType.Gradient,
                    Stops = new List<PaletteStop> {
                        new PaletteStop {Color = new Color {A = 255, R = 237, G = 248, B = 251}, StopValue = 0},
                        new PaletteStop {Color = new Color {A = 255, R = 0, G = 109, B = 44}, StopValue = 1.0}
                    }
                },
                new Palette {
                    Type = PaletteType.Gradient,
                    Stops = new List<PaletteStop> //253, 224, 221  197, 27, 138
                    {
                        new PaletteStop {Color = new Color {A = 255, R = 253, G = 224, B = 221}, StopValue = 0},
                        new PaletteStop {Color = new Color {A = 255, R = 197, G = 27, B = 138}, StopValue = 1.0}
                    }
                },
                new Palette {
                    Type = PaletteType.Gradient,
                    Stops = new List<PaletteStop> //237, 248, 177   44, 127, 184
                    {
                        new PaletteStop {Color = new Color {A = 255, R = 237, G = 248, B = 177}, StopValue = 0},
                        new PaletteStop {Color = new Color {A = 255, R = 44, G = 127, B = 184}, StopValue = 1.0}
                    }
                },
                new Palette {
                    Type = PaletteType.Gradient,
                    Stops = new List<PaletteStop> //255, 237, 160   240, 59, 32
                    {
                        new PaletteStop {Color = new Color {A = 255, R = 237, G = 248, B = 177}, StopValue = 0},
                        new PaletteStop {Color = new Color {A = 255, R = 240, G = 59, B = 32}, StopValue = 1.0}
                    }
                },
                new Palette {
                    Type = PaletteType.Gradient,
                    Stops = new List<PaletteStop> //255, 237, 160   240, 59, 32
                    {
                        new PaletteStop {Color = new Color {A = 255, R = 240, G = 240, B = 240}, StopValue = 0},
                        new PaletteStop {Color = new Color {A = 255, R = 99, G = 99, B = 99}, StopValue = 1.0}
                    }
                },
                new Palette {
                    Type = PaletteType.Gradient,
                    Stops = new List<PaletteStop> //26, 150, 65
                    {
                        new PaletteStop {Color = new Color {A = 255, R = 215, G = 25, B = 28}, StopValue = 1},
                        new PaletteStop {Color = new Color {A = 255, R = 253, G = 174, B = 97}, StopValue = 0.75},
                        new PaletteStop {Color = new Color {A = 255, R = 255, G = 255, B = 191}, StopValue = 0.5},
                        new PaletteStop {Color = new Color {A = 255, R = 166, G = 217, B = 106}, StopValue = 0.25},
                        new PaletteStop {Color = new Color {A = 255, R = 26, G = 150, B = 65}, StopValue = 0}
                    }
                },
                new Palette {
                    Type = PaletteType.Gradient,
                    Stops = new List<PaletteStop> //26, 150, 65
                    {
                        new PaletteStop {Color = new Color {A = 255, R = 215, G = 25, B = 28}, StopValue = 1},
                        new PaletteStop {Color = Colors.Orange, StopValue = 0.5},
                        new PaletteStop {Color = new Color {A = 255, R = 26, G = 150, B = 65}, StopValue = 0}
                    }
                },
                new Palette {
                    Type = PaletteType.Gradient,
                    Stops = new List<PaletteStop>
                    {
                        new PaletteStop {Color = Colors.Red, StopValue = 0},
                        new PaletteStop {Color = Colors.Orange, StopValue = 0.5},
                        new PaletteStop {Color = Colors.Green, StopValue = 1}
                    }
                },
                new Palette {
                    Type = PaletteType.Gradient,
                    Stops = new List<PaletteStop>
                    {
                        new PaletteStop {Color = Colors.Yellow, StopValue = 0},
                        new PaletteStop {Color = Colors.DodgerBlue, StopValue = 1}
                    }
                },
                new Palette {
                    Type = PaletteType.Gradient,
                    Stops = new List<PaletteStop>
                    {
                        new PaletteStop {Color = Colors.Yellow, StopValue = 1},
                        new PaletteStop {Color = Colors.DodgerBlue, StopValue = 0}
                    }
                }
            };


            //Items = new BindableCollection<System.Windows.Controls.MenuItem>();

            if (TimeOut.HasValue) {
                toTimer = new DispatcherTimer();
                toTimer.Interval = TimeOut.Value;
                toTimer.Tick += toTimer_Tick;
                toTimer.Start();
            }
        }

        private void toTimer_Tick(object sender, EventArgs e) {
            toTimer.Stop();
            Close();
        }

        public void Save() {
            if (Saved != null) Saved(this, new PaletteInputPopupEventArgs() {Result = SelectedPalette});
            if (AutoClose) AppState.Popups.Remove(this);
        }

        public void Cancel() {
            AppState.Popups.Remove(this);
        }

        private void UpdatePosition() {
            if (view == null) return;
            if (relativeElement != null) {
                Point = RelativeElement.TranslatePoint(RelativePosition, Application.Current.MainWindow);
            }

            view.VerticalAlignment = VerticalAlignment;

            switch (view.VerticalAlignment) {
                case VerticalAlignment.Top:
                    view.bInput.Margin = new Thickness(Point.X, Point.Y, 0, 0);
                    break;
                case VerticalAlignment.Bottom:
                    view.bInput.Margin = new Thickness(Point.X, 0, 0,
                        Application.Current.MainWindow.ActualHeight - Point.Y);
                    //view.Items.Margin = new Thickness(Point.X, Point.Y, 0, 0);
                    break;
            }
        }
    }
}