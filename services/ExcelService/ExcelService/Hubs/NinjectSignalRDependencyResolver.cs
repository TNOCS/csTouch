using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Ninject;

namespace ExcelService.Hubs
{
    internal class NinjectSignalRDependencyResolver : DefaultDependencyResolver
    {
        private readonly IKernel kernel;
        public NinjectSignalRDependencyResolver(IKernel kernel)
        {
            this.kernel = kernel;
        }

        public override object GetService(Type serviceType)
        {
            return kernel.TryGet(serviceType) ?? base.GetService(serviceType);
        }

        public override IEnumerable<object> GetServices(Type serviceType)
        {
            return kernel.GetAll(serviceType).Concat(base.GetServices(serviceType));
        }
    }
}
