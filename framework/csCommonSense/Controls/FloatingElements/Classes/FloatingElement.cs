using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using csShared.Documents;
using csShared.Geo;
using Microsoft.Surface.Presentation.Controls;

namespace csShared
{
    public class FloatingElement : PropertyChangedBase
    {
        public enum AutoCloseStyle
        {
            None,
            NoInteraction,
            Always
        }

        private bool allowStream = true;
        private List<string> allowedDropsTags = new List<string>();
        private Size? aspectSize;
        private KmlPoint associatedPoint;
        private AutoCloseStyle autoClose = AutoCloseStyle.None;
        private int autoCloseTimeout;

        private Brush background;
        private bool canExit = true;
        private bool canFlip;
        private bool canFullScreen;
        private bool canMove;
        private bool canRotate;
        private bool canScale;
        private bool canStream = true;
        private Dictionary<string, object> contracts = new Dictionary<string, object>();
        private double cornerRadius;
        private object customControl;
        private double dockWidth = 65;
        private DockingStyles dockingStyle = DockingStyles.None;
        private Document document;
        private Brush foreground;
        private bool fullScreenLock = true;
        private double height;
        private ImageSource icon;
        private double iconRotation = 270;
        private string iconUri;
        private double iconWidth = 30;

        private string id = Guid.NewGuid().ToString();
        private bool isBacksideQrCode;
        private bool large;
        private Size? maxSize;
        private Size? minSize;
        private object modelInstance;
        private object modelInstanceBack;
        private double opacityDragging;
        private double opacityNormal;
        private double originalOrientation;
        private bool promotetomouse;
        private bool removeOnEdge;
        private bool resetOnEdge;
        private bool rotateWithFinger = true;
        private string shareUrl;
        private bool showHeader = true;
        private bool showShadow;


        private bool showsActivationEffects;
        private double? startOrientation;
        private Point? startPosition;
        private Size? startSize;
        private Style style;
        private string title;
        private Type viewModel;
        private double width;


        private bool window = true;

        public FloatingElement(Size? targetSize)
        {
            TargetSize = targetSize;
            TargetSize = targetSize;
            IconRotation = AppStateSettings.Instance.Config.GetInt("Layout.Floating.IconRotation", 90);
            CornerRadius = AppStateSettings.Instance.Config.GetInt("Layout.Floating.CornerRadius", 10);
        }

        public FloatingElement()
        {
            
        }

        public int AutoCloseTimeout
        {
            get { return autoCloseTimeout; }
            set
            {
                autoCloseTimeout = value;
                NotifyOfPropertyChange(() => AutoCloseTimeout);
            }
        }

        public AutoCloseStyle AutoClose
        {
            get { return autoClose; }
            set
            {
                autoClose = value;
                NotifyOfPropertyChange(() => AutoClose);
            }
        }


        public string IconUri
        {
            get { return iconUri; }
            set
            {
                iconUri = value;
                NotifyOfPropertyChange(() => IconUri);
            }
        }


        public bool AllowStream
        {
            get { return allowStream; }
            set
            {
                allowStream = value;
                NotifyOfPropertyChange(() => AllowStream);
            }
        }

        public bool CanStream
        {
            get { return canStream; }
            set
            {
                canStream = value;
                NotifyOfPropertyChange(() => CanStream);
            }
        }


        public Size? AspectSize
        {
            get { return aspectSize; }
            set
            {
                aspectSize = value;
                NotifyOfPropertyChange(() => AspectSize);
            }
        }


        public string Id
        {
            get { return id; }
            set
            {
                id = value;
                NotifyOfPropertyChange(() => Id);
            }
        }


        public double OriginalOrientation
        {
            get { return originalOrientation; }
            set
            {
                originalOrientation = value;
                NotifyOfPropertyChange(() => OriginalOrientation);
            }
        }


        public bool RotateWithFinger
        {
            get { return rotateWithFinger; }
            set
            {
                rotateWithFinger = value;
                NotifyOfPropertyChange(() => RotateWithFinger);
            }
        }


        public ContainerPosition LastContainerPosition { get; set; }

        public double IconWidth
        {
            get { return iconWidth; }
            set
            {
                iconWidth = value;
                NotifyOfPropertyChange(() => IconWidth);
            }
        }

        public double CornerRadius
        {
            get { return cornerRadius; }
            set
            {
                cornerRadius = value;
                NotifyOfPropertyChange(() => CornerRadius);
            }
        }

        public bool CanFullScreen
        {
            get { return canFullScreen; }
            set
            {
                canFullScreen = value;
                NotifyOfPropertyChange(() => CanFullScreen);
            }
        }

        public bool IsFullScreen
        {
            get { return (AppStateSettings.Instance.FullScreenFloatingElement == this); }
        }

        public bool FullScreenLock
        {
            get { return fullScreenLock; }
            set
            {
                fullScreenLock = value;
                NotifyOfPropertyChange(() => FullScreenLock);
            }
        }


        public bool CanDrag { get; set; }


        public bool AllowDrop { get; set; }

        public List<string> AllowedDropsTags
        {
            get { return allowedDropsTags; }
            set { allowedDropsTags = value; }
        }

        public string DropTag { get; set; }

        public double DockWidth
        {
            get { return dockWidth; }
            set { dockWidth = value; }
        }


        public double IconRotation
        {
            get { return iconRotation; }
            set
            {
                iconRotation = value;
                NotifyOfPropertyChange(() => IconRotation);
            }
        }


        public bool Window
        {
            get { return window; }
            set
            {
                window = value;
                NotifyOfPropertyChange(() => Window);
            }
        }


        public ImageSource Icon
        {
            get { return icon; }
            set
            {
                icon = value;
                NotifyOfPropertyChange(() => Icon);
            }
        }

        public bool ShowHeader
        {
            get { return showHeader; }
            set
            {
                showHeader = value;
                NotifyOfPropertyChange(() => ShowHeader);
            }
        }

        public bool CanExit
        {
            get { return canExit; }
            set
            {
                canExit = value;
                NotifyOfPropertyChange(() => CanExit);
            }
        }


        public bool ShowShadow
        {
            get { return showShadow; }
            set
            {
                showShadow = value;
                NotifyOfPropertyChange(() => ShowShadow);
            }
        }


        public double Width
        {
            get { return width; }
            set
            {
                width = value;
                NotifyOfPropertyChange(() => Width);
            }
        }

        public bool PromoteToMouse
        {
            get { return promotetomouse; }
            set
            {
                promotetomouse = value;
                NotifyOfPropertyChange(() => PromoteToMouse);
            }
        }


        public double Height
        {
            get { return height; }
            set
            {
                height = value;
                NotifyOfPropertyChange(() => Height);
            }
        }


        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                NotifyOfPropertyChange(() => Title);
            }
        }

        public DockingStyles DockingStyle
        {
            get { return dockingStyle; }
            set
            {
                if (dockingStyle != value)
                {
                    dockingStyle = value;
                    NotifyOfPropertyChange(() => DockingStyle);
                    if (DockingStyleChanged != null) DockingStyleChanged(this, null);
                }
            }
        }

        public ScatterViewItem ScatterViewItem { get; set; }

        public bool Large
        {
            get { return large; }
            set
            {
                if (large != value)
                {
                    large = value;
                    NotifyOfPropertyChange(() => Large);
                    if (DockedChanged != null) DockedChanged(this, null);
                }
            }
        }

        public Brush Foreground
        {
            get { return foreground; }
            set
            {
                foreground = value;
                NotifyOfPropertyChange(() => Foreground);
            }
        }

        public Brush Background
        {
            get { return background; }
            set
            {
                background = value;
                NotifyOfPropertyChange(() => Background);
            }
        }


        public double OpacityNormal
        {
            get { return opacityNormal; }
            set
            {
                opacityNormal = value;
                NotifyOfPropertyChange(() => OpacityNormal);
            }
        }

        public double OpacityDragging
        {
            get { return opacityDragging; }
            set
            {
                opacityDragging = value;
                NotifyOfPropertyChange(() => OpacityDragging);
            }
        }

        public Point? StartPosition
        {
            get { return startPosition; }
            set
            {
                startPosition = value;
                NotifyOfPropertyChange(() => StartPosition);
            }
        }

        public double? StartOrientation
        {
            get { return startOrientation; }
            set
            {
                startOrientation = value;
                NotifyOfPropertyChange(() => StartOrientation);
            }
        }

        public bool CanMove
        {
            get { return canMove; }
            set
            {
                canMove = value;
                NotifyOfPropertyChange(() => CanMove);
            }
        }

        public Size? StartSize
        {
            get { return startSize; }
            set
            {
                startSize = value;
                NotifyOfPropertyChange(() => StartSize);
            }
        }

        public Size? MinSize
        {
            get { return minSize; }
            set
            {
                minSize = value;
                NotifyOfPropertyChange(() => MinSize);
            }
        }

        public Size? MaxSize
        {
            get { return maxSize; }
            set
            {
                maxSize = value;
                NotifyOfPropertyChange(() => MaxSize);
            }
        }

        public bool CanFlip
        {
            get { return canFlip; }
            set
            {
                canFlip = value;
                NotifyOfPropertyChange(() => CanFlip);
            }
        }

        public bool IsBacksideQrCode
        {
            get { return isBacksideQrCode; }
            set
            {
                if (isBacksideQrCode == value) return;
                isBacksideQrCode = value;
                NotifyOfPropertyChange(() => IsBacksideQrCode);
            }
        }

        public bool CanRotate
        {
            get { return canRotate; }
            set
            {
                canRotate = value;
                NotifyOfPropertyChange(() => CanRotate);
            }
        }

        public bool CanScale
        {
            get { return canScale; }
            set
            {
                canScale = value;
                NotifyOfPropertyChange(() => CanScale);
            }
        }

        public Style Style
        {
            get { return style; }
            set
            {
                style = value;
                NotifyOfPropertyChange(() => Style);
            }
        }

        public bool ResetOnEdge
        {
            get { return resetOnEdge; }
            set
            {
                resetOnEdge = value;
                NotifyOfPropertyChange(() => ResetOnEdge);
            }
        }


        public bool RemoveOnEdge
        {
            get { return removeOnEdge; }
            set
            {
                removeOnEdge = value;
                NotifyOfPropertyChange(() => RemoveOnEdge);
            }
        }


        public bool ShowsActivationEffects
        {
            get { return showsActivationEffects; }
            set
            {
                showsActivationEffects = value;
                NotifyOfPropertyChange(() => ShowsActivationEffects);
            }
        }

        public Document Document
        {
            get { return document; }
            set
            {
                document = value;
                NotifyOfPropertyChange(() => Document);
            }
        }

        public Type ViewModel
        {
            get { return viewModel; }
            set
            {
                viewModel = value;
                NotifyOfPropertyChange(() => ViewModel);
            }
        }

        public object ModelInstance
        {
            get { return modelInstance; }
            set
            {
                modelInstance = value;
                NotifyOfPropertyChange(() => ModelInstance);
            }
        }

        public object ModelInstanceBack
        {
            get { return modelInstanceBack; }
            set
            {
                modelInstanceBack = value;
                NotifyOfPropertyChange(() => ModelInstanceBack);
            }
        }

        public object CustomControl
        {
            get { return customControl; }
            set
            {
                customControl = value;
                NotifyOfPropertyChange(() => CustomControl);
            }
        }

        public string ConnectChannel { get; set; }

        public string ConnectMessage { get; set; }

        public TimeSpan AnimationSpeed { get; set; }

        public Point? OriginPosition { get; set; }


        public Size? OriginSize { get; set; }


        public double SwitchWidth { get; set; }

        public bool Contained { get; set; }

        public double DragScaleFactor { get; set; }

        public int Priority { get; set; }


        public Dictionary<string, object> Contracts
        {
            get { return contracts; }
            set { contracts = value; }
        }

        public Size? TargetSize { get; set; }

        public string ShareUrl
        {
            get { return shareUrl; }
            set
            {
                shareUrl = value;
                NotifyOfPropertyChange(() => ShareUrl);
            }
        }

        public KmlPoint AssociatedPoint
        {
            get { return associatedPoint; }
            set
            {
                associatedPoint = value;
                NotifyOfPropertyChange(() => AssociatedPoint);
            }
        }

        public event EventHandler DockedChanged;
        public event EventHandler FlipEvent;
        public event EventHandler UnFlipEvent;
        public event EventHandler DockingStyleChanged;
        public event EventHandler Closed;
        public event EventHandler<FloatingDragDropEventArgs> DragEnter;
        public event EventHandler<FloatingDragDropEventArgs> DragLeave;
        public event EventHandler<FloatingDragDropEventArgs> Drop;

        public void ForceDragEnter(object data, FloatingElement fe)
        {
            if (DragEnter != null) DragEnter(this, new FloatingDragDropEventArgs {Data = data, Element = fe});
        }

        public void ForceDragLeave(object data, FloatingElement fe)
        {
            if (DragLeave != null) DragLeave(this, new FloatingDragDropEventArgs {Data = data, Element = fe});
        }

        public void ForceDrop(object data, FloatingElement fe)
        {
            if (Drop != null) Drop(this, new FloatingDragDropEventArgs {Data = data, Element = fe});
        }


        public event EventHandler Reseted;

        public event EventHandler ResetRequest;


        public event EventHandler CloseRequest;

        public void Reset()
        {
            if (ResetRequest != null) ResetRequest(this, null);
        }

        public void Close()
        {
            if (CloseRequest != null) CloseRequest(this, null);
        }

        public void Flip()
        {
            if (FlipEvent != null) FlipEvent(this, null);
        }

        public void UnFlip()
        {
            if (UnFlipEvent != null) UnFlipEvent(this, null);
        }

        internal void TriggerResetEvent()
        {
            if (Reseted != null) Reseted(this, null);
        }

        internal void TriggerClosedEvent()
        {
            if (Closed != null) Closed(this, null);
        }
    }

    public class FloatingDragDropEventArgs : EventArgs
    {
        /// <summary>
        ///     Text to be displayed
        /// </summary>
        public object Data { get; set; }

        public FloatingElement Element { get; set; }
    }
}