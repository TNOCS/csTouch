using System.Web.Http.Dependencies;
using Ninject;

namespace ExcelService.Controllers
{
    public class NinjectResolver : NinjectDependencyScope, IDependencyResolver
    {
        private readonly IKernel kernel;

        public NinjectResolver(IKernel kernel)
            : base(kernel)
        {
            this.kernel = kernel;
        }

        public IDependencyScope BeginScope()
        {
            return new NinjectDependencyScope(kernel);
        }
    }
}
