using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;

namespace DataServer
{
    public class ServiceList : BindableCollection<Service>
    {

        public List<Service> SubscribedServices
        {
            get { return this.Where(k => k.IsSubscribed).ToList(); }
        }

        public string ToCapability()
        {
            string r = "";
            foreach (Service s in this)
            {
                r += s.ToCapability() + "#";
            }
            return r.Trim('#');
        }
    }
}