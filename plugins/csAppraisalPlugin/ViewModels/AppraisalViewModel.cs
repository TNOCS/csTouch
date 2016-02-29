using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Caliburn.Micro;
using csAppraisalPlugin.Interfaces;
using csAppraisalPlugin.Views;
using csCommon.csMapCustomControls.CircularMenu;
using csShared;
using csShared.Interfaces;

//TODO Bij het openen van de Appraisal plugin, laat de tabs klein worden
//DONE Menu voor het maken van een screenshot/appraisal
//DONE Uitlijnen appraisals verticaal
//TODO Uitlijnen appraisals horizontaal (vooral als er slechts een is, dan staat die niet centraal)
//TODO Onderscheiden van appraisals die nieuw zijn, of aangepast worden, en van diegene waarvan de analyse al gedaan is
//TODO Editen van appraisal titel werkt niet altijd
//DONE Nieuwe blanco appraisal aanmaken, en persisteren
//DONE Persisteren van appraisals (titel, criteria assen)
//DONE Appraisals naast elkaar kunnen tonen (2x2)
//DONE Editen van criteria assen, range (bv 1-10), aantal lijnen/issues+kleur, weights
//DONE Tonen van gewogen gemiddelde (per lijn/issue)
//DONE Gewogen gemiddelde aan/uit kunnen zetten
//TODO Standalone kunnen draaien, dus ook zonder kaart
//TODO Lock een Appraisal zdd hij niet verandert
//DONE Output opslaan in netwerk map als XML, HTML oid
//DONE SpiderChart aanpassen voor 1 of 2 items
//DONE SpiderChart Waarde van spider terughalen en in Appraisal zetten ==> Raise ValuesChanged event
//TODO SpiderChart Tonen van de waarde van de slider tijdens het bewegen
namespace csAppraisalPlugin.ViewModels
{
    [Export(typeof (IAppraisal)), PartCreationPolicy(CreationPolicy.Shared)]
    public class AppraisalViewModel : Conductor<Screen>.Collection.OneActive, IAppraisal, IPluginScreen
    {
        private readonly ISpider spiderViewModel;
        private readonly ISetWeights setWeightsViewModel;
        private readonly ICompareResults compareResultsViewModel;
        private readonly ISpiderImageCombi spiderImageCombiViewModel;
        private IAppraisalTab appraisalTab;
        private AppraisalPlugin plugin;
        private bool isSpiderShown = true;
        private bool isWeightShown;
        private bool isComparisonShown;

        [ImportingConstructor]
        public AppraisalViewModel(ISpider spiderViewModel, ISetWeights setWeightsViewModel, ICompareResults compareResultsViewModel, ISpiderImageCombi spiderImageCombiViewModel)
        {
            this.spiderViewModel = spiderViewModel;
            this.setWeightsViewModel = setWeightsViewModel;
            this.compareResultsViewModel = compareResultsViewModel;
            this.spiderImageCombiViewModel = spiderImageCombiViewModel;

            this.compareResultsViewModel.GotoDetailedViewModel += (sender, args) => {
                spiderImageCombiViewModel.SelectedAppraisal = args.Appraisal;
                ActivateItem(Items[3]);
            };

            this.spiderImageCombiViewModel.CloseView += (sender, args) => ShowComparison();

            Items.Add(spiderViewModel as Screen);
            Items.Add(setWeightsViewModel as Screen);
            Items.Add(compareResultsViewModel as Screen);
            Items.Add(spiderImageCombiViewModel as Screen);

            CreateCircularMenu();
        }

        private ImageSource _iCopy;

        public ImageSource iCopy
        {
            get { return _iCopy; }
            set { _iCopy = value; NotifyOfPropertyChange(()=>iCopy); }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            ActivateItem(Items[0]);
        }

        public bool SlideNow { get; set; }

        public AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        private AppraisalView av;

        public void Start()
        {
            
        }

        public void Stop()
        {}

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            av = (AppraisalView) view;
            //nav2.Items.Add(ssmap);
            //AppState.CircularMenus.Add(nav2);
        }

        private void CreateCircularMenu() {
            var nav = new CircularMenuItem {
                Id = "AppraisalMenu",
                Icon = "pack://application:,,,/csAppraisalPlugin;component/icons/navigation.png",
                Items = new List<CircularMenuItem>()
            };

            var map = new CircularMenuItem {
                Title = "Map",
                Position = 1,
                Element =
                    "M2.925457,45.511272C10.741428,45.502132,18.48247,50.500496,21.411001,52.619106L20.564001,62.264 0,53.770779 2.1689987,45.528053C2.4211235,45.517071,2.6733284,45.511566,2.925457,45.511272z M43.952001,43.591896C55.724,52.525307,62.183001,53.360489,62.789,53.421089L64,61.205421 44.424,54.828056 44.396,54.156773z M40.450001,43.115406L40.919001,54.26947 40.950001,54.990753 24.056,62.232002 24.903,52.581406z M32.606,36.478134L32.606,43.690895 25.280001,48.306694 26.096001,39.011951z M59.924001,35.005447L62.139,49.239574C59.452001,49.095078 54.612,46.333439 50.368,43.548397 53.144,35.378548 59.758,35.013317 59.924001,35.005447z M16.933001,22.882633L23.807001,25.352081 23.771,25.766691 23.009001,34.433929C13.212002,29.659979,16.569,23.494579,16.933001,22.882633z M8.8899994,19.993994L13.364,21.601459C9.25,30.491262,18.677,36.603863,22.634001,38.703369L21.754,48.719387C6.2389984,39.027451,3.1920013,41.771053,3.1920013,41.771053L2.8279991,43.028809z M39.475,19.857397L40.272,38.861565 36.321,41.351074 36.242001,33.327076C35.919001,31.54977,33.817,31.872763,33.817,31.872763L26.479,34.649315 27.239,25.997887 27.308001,25.195833z M42.948,19.718L46.328,20.843176 48.265,28.147562 49.341001,32.198376 51.812,28.815638 55.427,23.871042 58.343,24.841661 59.338,31.237457C50.179,32.483341,47.031,41.246284,47.031,41.246284L47.372,41.519039C45.925,40.510029,44.669,39.595921,43.755,38.916862z M53.679668,4.3845386C51.646579,4.4165239 49.847217,5.8707604 49.457722,7.9460926 49.009715,10.31923 50.56956,12.603175 52.941967,13.049685 55.314495,13.496293 57.60103,11.937162 58.046438,9.5640144 58.494245,7.1922369 56.93172,4.9063015 54.561872,4.4602423 54.26532,4.4044247 53.970109,4.3799686 53.679668,4.3845386z M53.876964,0.00037478212C54.370403,0.0050314565 54.870273,0.05313564 55.372135,0.14771746 59.963256,1.0122223 62.984005,5.4340973 62.119318,10.023382 61.814724,11.634684 61.072536,13.050685 60.038754,14.176887L55.179538,20.820293 50.197683,27.634001 48.033721,19.474491 45.924445,11.519484C45.372331,10.095582 45.19521,8.50704 45.497207,6.8932085 46.267306,2.8053779 49.85896,-0.037543376 53.876964,0.00037478212z"
            };
            map.Selected += (e, f) => {
                Plugin.Active = false;
                AppState.TriggerScriptCommand(this, "HideTab");
            };
            nav.Items.Add(map);

            var evaluate = new CircularMenuItem {
                Title = "Spider",
                Position = 2,
                Element =
                    "F1M87.929,49C81.31,42.237,77.629,33.351,77.527,23.888L86.063,15.352 84.649,13.938 76.112,22.475C66.648,22.371,57.763,18.69,51,12.073L51,0 49,0 49,12.072C42.236,18.691,33.35,22.372,23.887,22.474L15.351,13.938 13.937,15.352 22.474,23.889C22.371,33.352,18.69,42.238,12.072,49.001L0,49.001 0,51.001 12.072,51.001C18.691,57.765,22.371,66.651,22.473,76.114L13.938,84.649 15.352,86.063 23.889,77.526C33.351,77.629,42.237,81.31,49,87.927L49,100 51,100 51,87.929C57.764,81.311,66.649,77.63,76.113,77.528L84.648,86.063 86.062,84.649 77.526,76.113C77.628,66.65,81.309,57.764,87.928,51L100,51 100,49 87.929,49z M52.414,49L58.034,43.38C58.369,45.413,59.161,47.325,60.362,49L52.414,49z M51,47.586L51,39.638C52.674,40.839,54.586,41.631,56.621,41.966L51,47.586z M49,47.587L43.379,41.966C45.413,41.631,47.325,40.839,49,39.638L49,47.587z M47.585,49L39.643,49C40.843,47.326,41.634,45.416,41.969,43.384L47.585,49z M47.586,51L41.967,56.619C41.632,54.586,40.84,52.674,39.639,51L47.586,51z M49,52.415L49,60.361C47.326,59.16,45.414,58.369,43.381,58.034L49,52.415z M51,52.415L56.622,58.037C54.588,58.372,52.675,59.164,51,60.367L51,52.415z M52.414,51L60.362,51C59.16,52.675,58.368,54.586,58.034,56.62L52.414,51z M62.941,49C61.034,46.959,59.952,44.346,59.858,41.556L66.783,34.631C67.247,39.941,69.308,44.917,72.734,49L62.941,49z M58.445,40.142C55.653,40.047,53.04,38.965,51,37.059L51,27.265C55.083,30.692,60.059,32.753,65.37,33.217L58.445,40.142z M49,37.059C46.96,38.966,44.347,40.048,41.555,40.142L34.629,33.216C39.94,32.752,44.917,30.692,49,27.265L49,37.059z M40.145,41.56C40.051,44.349,38.969,46.961,37.064,49L27.27,49C30.695,44.918,32.755,39.943,33.22,34.635L40.145,41.56z M37.059,51C38.966,53.04,40.048,55.653,40.142,58.443L33.215,65.37C32.752,60.06,30.691,55.083,27.265,51L37.059,51z M41.557,59.858C44.348,59.953,46.961,61.034,49,62.94L49,72.734C44.917,69.308,39.941,67.247,34.631,66.783L41.557,59.858z M51,62.947C53.04,61.039,55.654,59.956,58.446,59.862L65.759,67.175C60.292,67.606,55.171,69.728,51,73.288L51,62.947z M59.858,58.445C59.952,55.654,61.034,53.041,62.941,51.001L72.735,51.001C69.309,55.084,67.248,60.061,66.783,65.371L59.858,58.445z M85.196,49L75.427,49C71.169,44.599,68.788,38.851,68.688,32.727L75.594,25.821C76.108,34.449,79.458,42.536,85.196,49z M74.18,24.407L67.274,31.313C61.149,31.213,55.401,28.832,51,24.572L51,14.806C57.464,20.543,65.55,23.893,74.18,24.407z M49,14.805L49,24.573C44.598,28.832,38.85,31.213,32.725,31.312L25.82,24.407C34.449,23.893,42.536,20.543,49,14.805z M24.406,25.821L31.316,32.731C31.215,38.853,28.835,44.6,24.578,49L14.805,49C20.542,42.536,23.892,34.45,24.406,25.821z M14.805,51.001L24.573,51.001C28.832,55.402,31.212,61.151,31.312,67.275L24.406,74.181C23.893,65.553,20.543,57.466,14.805,51.001z M25.821,75.593L32.728,68.687C38.852,68.788,44.599,71.169,49,75.427L49,85.194C42.536,79.457,34.45,76.107,25.821,75.593z M51,85.197L51,75.996C55.478,71.58,61.385,69.133,67.674,69.089L74.18,75.595C65.551,76.109,57.464,79.458,51,85.197z M75.594,74.181L68.688,67.275C68.789,61.151,71.17,55.403,75.428,51.001L85.196,51.001C79.457,57.465,76.107,65.552,75.594,74.181z"
            };

            evaluate.Selected += (e, f) => {
                ShowSpider();
                Plugin.Active = true;
                //AppState.TriggerScriptCommand(this, "HideTab");
            };
            nav.Items.Add(evaluate);

            var weights = new CircularMenuItem {
                Title = "Weights",
                Position = 3,
                Element =
                    "M7.80571937561035,8.10778141021729C10.7026662826538,8.10778141021729 13.0521078109741,9.59734630584717 13.0521078109741,11.4346685409546 13.0521078109741,12.7155809402466 11.9100923538208,13.8271646499634 10.2369184494019,14.3831624984741L10.2369184494019,16.4256200790405 15.6114377975464,17.321099281311 15.6114377975464,33.4444341659546 1.43051147460938E-05,33.4444341659546 0,17.321099281311 5.37451934814453,16.4543943405151 5.37451934814453,14.3831624984741C3.7013463973999,13.8271608352661 2.55912494659424,12.7155809402466 2.55912494659424,11.4346685409546 2.55912494659424,9.59734630584717 4.90856456756592,8.10778141021729 7.80571937561035,8.10778141021729z M28.3923807144165,0C32.216477394104,0 35.3177347183228,1.96669864654541 35.3177347183228,4.39169025421143 35.3177347183228,6.08204650878906 33.8103685379028,7.55008411407471 31.6016931533813,8.28389263153076L31.6016931533813,10.9792242050171 38.6959619522095,12.1616067886353 38.6959619522095,33.4444341659546 18.0888242721558,33.4444341659546 18.0887956619263,12.1616067886353 25.1830644607544,11.0173139572144 25.1830644607544,8.28389263153076C22.9745950698853,7.55008411407471 21.467022895813,6.08204650878906 21.467022895813,4.39169025421143 21.467022895813,1.96669864654541 24.5680723190308,0 28.3923807144165,0z"
            };
            weights.Selected += (e, f) => {
                Plugin.Active = true;
                AppState.TriggerScriptCommand(this, "HideTab");
                ShowWeights();
            };
            nav.Items.Add(weights);

            var comp = new CircularMenuItem {
                Title = "Compare",
                Position = 4,
                Element =
                    "M36.142006,10.962172L32.702682,19.484018 39.562656,19.484018z M21.377508,3.9540111C20.638903,3.9540111 20.040033,4.5530071 20.040033,5.2910022 20.040033,6.0299968 20.638903,6.628993 21.377508,6.6289934 22.116112,6.628993 22.714981,6.0299968 22.714981,5.2910022 22.714981,4.5530071 22.116112,3.9540111 21.377508,3.9540111z M6.6772561,2.9800028L2.754442,12.676995 10.570013,12.676995z M22.358694,0L23.716986,0.50600021 22.821655,2.9272763 22.930405,2.9933538C23.667496,3.4914021,24.152054,4.3347625,24.152054,5.2910132L24.149601,5.3880271 36.3316,8.6201219C36.687801,8.7148008,36.912186,9.0652198,36.856457,9.4242076L36.854034,9.4358653 40.896374,19.484018 42.039047,19.484018C42.379021,19.484018 42.668999,19.764019 42.668999,20.110018 42.668999,20.435331 42.414135,20.702635 42.10215,20.734777L42.051029,20.737402 42.044209,20.979973C41.883854,23.824837 39.268093,26.088999 36.061001,26.088999 32.853897,26.088999 30.238142,23.824837 30.077787,20.979973L30.070986,20.738018 30.069948,20.738018C29.729973,20.738018 29.449993,20.457018 29.449993,20.110018 29.449993,19.764019 29.729973,19.484018 30.069948,19.484018L31.367119,19.484018 35.273426,9.8214015 23.728537,6.7583441 23.678848,6.8401408C23.529467,7.0612735,23.349003,7.2596955,23.143724,7.429131L23.073011,7.482017 23.073011,29.562002 40.337971,29.562002 40.337971,32.895 2.5100434,32.895 2.5100434,29.562002 19.739973,29.562002 19.739973,7.5201239 19.618309,7.429131C19.002472,6.9208245,18.60998,6.1516399,18.60998,5.2910132L18.611324,5.2378539 7.7520838,2.3584441 11.894357,12.676995 12.589052,12.676995C12.939026,12.676995 13.219004,12.957995 13.219004,13.303996 13.219004,13.629309 12.97293,13.895735 12.6538,13.927768L12.601033,13.9304 12.594211,14.173107C12.433858,17.018515 9.818099,19.284 6.611002,19.284 3.4039052,19.284 0.78814697,17.018515 0.62779361,14.173107L0.62097508,13.93055 0.56520373,13.927768C0.24607517,13.895735 0,13.629309 0,13.303996 0,12.957995 0.27997872,12.676995 0.62995219,12.676995L1.4269502,12.676995 5.9680638,1.4442006 5.9658251,1.4318957C5.9537902,1.3398513 5.9600406,1.243728 5.9875469,1.1482275 6.0688004,0.83705931 6.3415871,0.62754457 6.6449142,0.61463474 6.7149138,0.61165541 6.7865396,0.61914628 6.8578115,0.63827442L19.011492,3.8599045 19.083187,3.7418858C19.581125,3.0047764,20.424408,2.5200102,21.381016,2.5200097L21.417152,2.520924z"
            };
            comp.Selected += (e, f) => {
                Plugin.Active = true;
                ShowComparison();
            };
            nav.Items.Add(comp);

            var export = new CircularMenuItem
            {
                Title = "Export",
                Position = 7,
                Icon = "pack://application:,,,/csAppraisalPlugin;component/icons/powerpoint.png",
            };
            export.Selected += (e, f) => {
                Plugin.Export();                                     
            };
            nav.Items.Add(export);

            AppState.CircularMenus.Add(nav);

            //var nav2 = new CircularMenuItem()
            //{
            //    Icon = "pack://application:,,,/csAppraisalPlugin;component/icons/tool.png",
            //    Items = new List<CircularMenuItem>()

            //};
            //var ss = new CircularMenuItem {
            //    Title = "Screenshot",
            //    Position = 1,
            //    Element = "M16.6693,45.905003L43.4757,45.905003C44.112999,45.905003,44.628002,46.421967,44.628002,47.056145L44.628002,48.788155C44.628002,49.425034,44.112999,49.941998,43.4757,49.941998L16.6693,49.941998C16.0326,49.941998,15.517,49.425034,15.517,48.788155L15.517,47.056145C15.517,46.421967,16.0326,45.905003,16.6693,45.905003z M49.41954,39.667637C48.692036,39.667637 48.101837,40.270634 48.101837,41.014027 48.101837,41.76012 48.692036,42.362915 49.41954,42.362915 50.145443,42.362915 50.735249,41.76012 50.735249,41.014027 50.735249,40.270634 50.145443,39.667637 49.41954,39.667637z M44.825417,39.607838C44.097614,39.607838 43.509415,40.210533 43.509415,40.953926 43.509415,41.69762 44.097614,42.303116 44.825417,42.303116 45.55172,42.303116 46.141525,41.69762 46.141525,40.953926 46.141525,40.210533 45.55172,39.607838 44.825417,39.607838z M6.3522615,5.5287595C5.911829,5.5287595,5.5541072,5.8554168,5.5541077,6.2604427L5.5541077,37.508957C5.5541072,37.913853,5.911829,38.240654,6.3522615,38.240654L55.388371,38.240654C55.828773,38.240654,56.188175,37.913853,56.188175,37.508957L56.188175,6.2604427C56.188175,5.8554168,55.828773,5.5287595,55.388371,5.5287595z M0.98016852,0L61.087196,0C61.629902,-2.3297311E-09,62.068005,0.436275,62.068005,0.97923898L62.068005,42.791309C62.068005,43.333004,61.629902,43.772999,61.087196,43.772999L38.711491,43.772999 38.711491,42.46711C38.711491,41.924217,38.272388,41.484024,37.729683,41.484024L24.010818,41.484024C23.471514,41.484024,23.030712,41.924217,23.030712,42.46711L23.030712,43.772999 0.98016852,43.772999C0.43945515,43.772999,-3.5527137E-15,43.333004,0,42.791309L0,0.97923898C-3.5527137E-15,0.436275,0.43945515,-2.3297311E-09,0.98016852,0z"
            //};
            //ss.Selected += (e, f) => ScreenshotScreen();
            //nav.Items.Add(ss);
            //nav2.Items.Add(ss);

            var ssmap = new CircularMenuItem {
                Title = "Screenshot",
                Position = 5,
                Element =
                    "M2.925457,45.511272C10.741428,45.502132,18.48247,50.500496,21.411001,52.619106L20.564001,62.264 0,53.770779 2.1689987,45.528053C2.4211235,45.517071,2.6733284,45.511566,2.925457,45.511272z M43.952001,43.591896C55.724,52.525307,62.183001,53.360489,62.789,53.421089L64,61.205421 44.424,54.828056 44.396,54.156773z M40.450001,43.115406L40.919001,54.26947 40.950001,54.990753 24.056,62.232002 24.903,52.581406z M32.606,36.478134L32.606,43.690895 25.280001,48.306694 26.096001,39.011951z M59.924001,35.005447L62.139,49.239574C59.452001,49.095078 54.612,46.333439 50.368,43.548397 53.144,35.378548 59.758,35.013317 59.924001,35.005447z M16.933001,22.882633L23.807001,25.352081 23.771,25.766691 23.009001,34.433929C13.212002,29.659979,16.569,23.494579,16.933001,22.882633z M8.8899994,19.993994L13.364,21.601459C9.25,30.491262,18.677,36.603863,22.634001,38.703369L21.754,48.719387C6.2389984,39.027451,3.1920013,41.771053,3.1920013,41.771053L2.8279991,43.028809z M39.475,19.857397L40.272,38.861565 36.321,41.351074 36.242001,33.327076C35.919001,31.54977,33.817,31.872763,33.817,31.872763L26.479,34.649315 27.239,25.997887 27.308001,25.195833z M42.948,19.718L46.328,20.843176 48.265,28.147562 49.341001,32.198376 51.812,28.815638 55.427,23.871042 58.343,24.841661 59.338,31.237457C50.179,32.483341,47.031,41.246284,47.031,41.246284L47.372,41.519039C45.925,40.510029,44.669,39.595921,43.755,38.916862z M53.679668,4.3845386C51.646579,4.4165239 49.847217,5.8707604 49.457722,7.9460926 49.009715,10.31923 50.56956,12.603175 52.941967,13.049685 55.314495,13.496293 57.60103,11.937162 58.046438,9.5640144 58.494245,7.1922369 56.93172,4.9063015 54.561872,4.4602423 54.26532,4.4044247 53.970109,4.3799686 53.679668,4.3845386z M53.876964,0.00037478212C54.370403,0.0050314565 54.870273,0.05313564 55.372135,0.14771746 59.963256,1.0122223 62.984005,5.4340973 62.119318,10.023382 61.814724,11.634684 61.072536,13.050685 60.038754,14.176887L55.179538,20.820293 50.197683,27.634001 48.033721,19.474491 45.924445,11.519484C45.372331,10.095582 45.19521,8.50704 45.497207,6.8932085 46.267306,2.8053779 49.85896,-0.037543376 53.876964,0.00037478212z"
            };
            ssmap.Selected += (e, f) => ScreenshotMap();
            nav.Items.Add(ssmap);
        }

        public void ScreenshotMap()
        {
            av.MainImage.Source = Plugin.CreateNewMapAppraisal();
            var nea = new NotificationEventArgs()
            {
                Header = "Map Screenshot saved",                
                Foreground = Brushes.Black,
                Background = AppState.AccentBrush,
                Options = new List<string>() {"Set score"}
            };
            nea.OptionClicked += (e, b) =>
            {

                //AppState.TriggerNotification("test");
                ShowSpider();
                Plugin.Active = true;
            };
            AppState.TriggerNotification(nea);
            //(av.FindResource("SlideImage") as Storyboard).Begin();
        }

        public void ScreenshotScreen()
        {
            av.MainImage.Source = Plugin.CreateNewAppraisal();
            AppState.TriggerNotification("Screenshot saved");
            //(av.FindResource("SlideImage") as Storyboard).Begin();
        }

        #region IAppraisal Members

        public AppraisalPlugin Plugin
        {
            get { return plugin; }
            set
            {
                plugin = spiderViewModel.Plugin = setWeightsViewModel.Plugin = compareResultsViewModel.Plugin = value;
                AppraisalTab = AppState.Container.GetExportedValue<IAppraisalTab>();
                AppraisalTab.Plugin = Plugin;
                NotifyOfPropertyChange(() => Active);
                plugin.ActiveChanged += (e, s) => NotifyOfPropertyChange(() => Active);
            }
        }

        #endregion

        public void ShowSpider()
        {
            ActivateItem(Items[0]);
            SetViewToggleButton(0);
        }

        public void ShowWeights()
        {
            ActivateItem(Items[1]);
            SetViewToggleButton(1);
        }

        public void ShowComparison()
        {
            ActivateItem(Items[2]);
            SetViewToggleButton(2);
        }

        public void SetViewToggleButton(int viewIndex)
        {
            IsSpiderShown = IsWeightShown = IsComparisonShown = false;
            switch (viewIndex)
            {
                case 0: IsSpiderShown = true; break;
                case 1: IsWeightShown = true; break;
                case 2: IsComparisonShown = true; break;
            }
        }

        public bool IsSpiderShown
        {
            get { return isSpiderShown; }
            set
            {
                if (value.Equals(isSpiderShown)) return;
                isSpiderShown = value;
                NotifyOfPropertyChange(() => IsSpiderShown);
            }
        }

        public bool IsWeightShown
        {
            get { return isWeightShown; }
            set
            {
                if (value.Equals(isWeightShown)) return;
                isWeightShown = value;
                NotifyOfPropertyChange(() => IsWeightShown);
            }
        }

        public bool IsComparisonShown
        {
            get { return isComparisonShown; }
            set
            {
                if (value.Equals(isComparisonShown)) return;
                isComparisonShown = value;
                NotifyOfPropertyChange(() => IsComparisonShown);
            }
        }

        #region IPluginScreen Members

        public string Name
        {
            get { return "Appraisal Compare"; }
        }

        #endregion

        public IAppraisalTab AppraisalTab
        {
            get { return appraisalTab; }
            set
            {
                appraisalTab = value;
                NotifyOfPropertyChange(() => AppraisalTab);
            }
        }

        public void StartEvaluate()
        {
            Plugin.Active = !Plugin.Active;
        }

        public bool Active
        {
            get { return Plugin != null && Plugin.Active; }
        }

    }
}