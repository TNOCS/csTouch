using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using csDataServerPlugin;
using csShared;
using csShared.Timeline;
using Caliburn.Micro;
using DataServer;
using ESRI.ArcGIS.Client;
using SharpMap.Geometries;
using Xunit;
using Xunit.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace csTest
{
    public class CsTestClass
    {
        private readonly ITestOutputHelper _output;

        public CsTestClass(ITestOutputHelper output)
        {
            this._output = output;
        }

        [WpfFact]
        public static async void WpfFact_TestSTA()
        {
            Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
            await Task.Yield();
            Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState()); // still there
        }


        [WpfFact]
        public void DataServer_LoadsServices()
        {
            /*
            DataServerBase ds = null;
            ds = new DataServerBase();
            ds.Start("PoiLayers", Mode.server, null, false, true);
            Assert.NotNull(ds);
            Assert.Equal(ds.IsRunning, true);
            Assert.Equal(ds.Services.Count > 0, true);
            */

            var appState = AppStateSettings.GetInstance();
            var dataServerPlugin = new DataServerPlugin();
            dataServerPlugin.AppState = appState;
            dataServerPlugin.Init();
            dataServerPlugin.Start();
            var ds = dataServerPlugin.Dsb;

            //ds.Start("PoiLayers", Mode.server, null, false, true);
            Assert.NotNull(ds);
            Assert.Equal(ds.IsRunning, true);
            Assert.Equal(ds.Services.Count > 0, true);
        }

        [WpfFact]
        public void PoiService_SetExtent()
        {
            var appState = AppStateSettings.GetInstance();
            appState.ViewDef.AcceleratedLayers = new GroupLayer();
            appState.ViewDef.Layers = new GroupLayer();
            appState.ViewDef.MapControl = new Map();
            appState.TimelineManager = new TimelineManager();
            var dataServerPlugin = new DataServerPlugin();
            dataServerPlugin.AppState = appState;
            dataServerPlugin.Init();
            dataServerPlugin.Start();

            var ds = dataServerPlugin.Dsb;

            /*
            DataServerBase ds = null;
            ds = new DataServerBase();
            ds.Start("PoiLayers", Mode.server, null, false, true);
            */


            Assert.NotNull(ds);
            Assert.Equal(ds.IsRunning, true);
            Assert.Equal(ds.Services.OfType<PoiService>().Any(), true);

            var nl = new BoundingBox(1.41852205678639, 50.6702276112004, 9.595187398461, 53.4954126495966);
            var amersfoort = new BoundingBox(5.16706546164613, 52.0767306960208, 5.67810704550079, 52.2530563435811);
            var veenendaal = new BoundingBox(5.28710700035366, 51.969451898025, 5.79814858420832, 52.1462014821955);
            var service = ds.Services.OfType<PoiService>().Skip(0).First();
            service.Start();
            //service.Init(Mode.server, ds);

            service.Initialized += (sender, args) =>
            {
                _output.WriteLine("Initialized: {0} pois", service.PoIs.Count);

                var timer = new Stopwatch();
                timer.Start();
                service.SetExtent(nl); // nl
                timer.Stop();
                _output.WriteLine("{0} pois in nl({1:N2}s)", service.ContentInExtent.Count(), timer.Elapsed.TotalSeconds);
                timer.Reset();

                timer.Start();
                service.SetExtent(amersfoort); // amersfoort
                timer.Stop();
                _output.WriteLine("{0} pois in amersfoort({1:N2}s)", service.ContentInExtent.Count(), timer.Elapsed.TotalSeconds);
                timer.Reset();

                timer.Start();
                service.SetExtent(veenendaal); // veenendaal
                timer.Stop();
                _output.WriteLine("{0} pois in veenendaal({1:N2}s)", service.ContentInExtent.Count(), timer.Elapsed.TotalSeconds);

            };

            service.Start();
            //service.InitPoiService();
        }
    }
}
