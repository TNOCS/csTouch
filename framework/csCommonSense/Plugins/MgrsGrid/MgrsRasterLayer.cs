using csShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace csCommon.Plugins.MgrsGrid
{
     
    public class MgrsRasterLayer : FrameworkElement
    {
        private VisualCollection mVisuals;

        public MgrsRasterLayer(MgrsConfig pConfig)
        {
            Cfg = pConfig;
            mVisuals = new VisualCollection(this);
            ClipToBounds = true;
            IsHitTestVisible = false;

        }

        public MgrsConfig Cfg { get; private set; }

        private bool mIsInitialized = false;

        #region OVERRIDES

        protected override void OnRender(DrawingContext pDrawingContext)
        {
            base.OnRender(pDrawingContext);
            if ((!mIsInitialized) && (AppStateSettings.Instance.ViewDef.MapControl != null))
            {
                AppStateSettings.Instance.ViewDef.MapControl.ExtentChanged += (sender, e) => this.InvalidateVisual(); 
                mIsInitialized = true;
            }
            if (!mIsInitialized) return;
            //if (!(Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))) return;
            

            var vp = new MgrsViewport(AppStateSettings.Instance.ViewDef.MapControl); /* Calculate visisble UTM boxes */
            LastMetersPerPixel = vp.MetersPerPixel;
            if (!Cfg.IsEnabled) return;
            var draw = new DrawMgrsRaster(Cfg, vp);
            draw.Render(pDrawingContext);
        }

        public double LastMetersPerPixel { get; private set; }

        protected override int VisualChildrenCount
        {
            get { return mVisuals.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index > mVisuals.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            return mVisuals[index];
        }

        #endregion

      
    }
}
