using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using IO.Swagger.Api;
using Quobject.SocketIoClientDotNet.Client;
using IO.Swagger.Client;
using IO.Swagger.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Dynamic;

namespace csWebDotNetLib
{
    

    public class Subscription
    {
        public string id;
        public string type;
        public string target;       
        public event EventHandler<LayerCallback> LayerCallback;

        public JObject JSON()
        {            
            return JObject.FromObject(new {id = id, target = target, type = type});            
        }

        internal void TriggerLayerCallback(dynamic l)
        {
            LayerCallback?.Invoke(this, l);
        }
    }


    public class SubscriptionCallback
    {
        public string action;
        public object data;

        public JObject JSON()
        {
            return JObject.FromObject(new { action = action});
        }
    }

    public enum LayerUpdateAction
    {
        updateFeature,
        updateLog,
        deleteFeature,
        addLayer,
        deleteLayer
    }

    public class LayerCallback
    {
        public string layerId;
        public string featureId;
        public LayerUpdateAction action;
        public object item;
        
    }

    public class csWebApi
    {
        public ApiClient client;
        public FeatureApi features;
        public LayerApi layers;
        public ResourceApi resources;
        private string server;
        private Socket socket;

        private Dictionary<string, Subscription> subscriptions;

        #region singleton code

        private static csWebApi _instance;

        public static csWebApi Instance => _instance;

        static csWebApi()
        {
            _instance = new csWebApi();
        }
        #endregion

        private csWebApi()
        {
            
        }

        public void Init(string server)
        {
            this.subscriptions = new Dictionary<string, Subscription>();
            this.server = server;
            client = new ApiClient(server + "/api");            
            layers = new LayerApi(client);
            features = new FeatureApi(client); 
            resources = new ResourceApi(client);           
            InitSocketConnection();
            //TestApis();
            
        }

        public void InitSocketConnection()
        {
            socket = Quobject.SocketIoClientDotNet.Client.IO.Socket(server);

            socket.On(Socket.EVENT_CONNECT, () =>
            {
                Console.WriteLine("connected");
                //socket.Emit("hi");

                

            });
            socket.On("msg", o =>
            {
                Console.WriteLine(o.ToString());
            });

            

        }

        public bool IsConnected => socket != null;

        public Subscription GetLayerSubscription(string layerId)
        {
            return this.Subscribe(layerId,"layer");
        }

        public Subscription GetDirectorySubscription()
        {
            return this.Subscribe("", "directory");
        }

        public void UnsubscribeLayer(Subscription s)
        {
            var id = s.id;
            if (this.subscriptions.ContainsKey(id))
            {
                var sub = subscriptions[id];                
                var r = new SubscriptionCallback();
                r.action = "unsubscribe";
                this.socket.Emit(id,  r.JSON());
                this.subscriptions.Remove(id);
            }
        }


        private Subscription Subscribe(string target, string type)
        {
            Subscription s;
            if (!this.subscriptions.ContainsKey(target))
            {
                s = new Subscription() {id = Guid.NewGuid().ToString(), target = target, type = type};

                this.socket.Emit("subscribe", s.JSON());
                this.subscriptions[s.id] = s;
                this.socket.On(s.id, (dynamic d) =>
                {
                    switch ((string) d.action)
                    {
                        case "subscribed":
                            Console.WriteLine("subscription : " + s.id);
                            break;
                        case "layer":
                            //var l = new LayerCallback();
                            var l = JsonConvert.DeserializeObject<LayerCallback>(d.data.ToString());
                            s.TriggerLayerCallback(l);
                            
                            break;
                    }

                });
            }
            else
            {
                s = this.subscriptions[target];

            }
            return s;            
        }

        public void TestApis()
        {
            layers.DeleteLayer("testje");
            var l = new Layer
            {
                Id = "testje",
                Title = "test layer",
                Storage = "file",
                Type = "dynamicgeojson",
                Features = new List<Feature>()
            };
            var f = new Feature() {Type = "Feature", Geometry = new Geometry() {Type = "Point", Coordinates = new List<double?>() {5.3, 54.2}}};
            
            try
            {
                layers.AddLayer(l);
                features.AddFeature("testje", f);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

    }
}
