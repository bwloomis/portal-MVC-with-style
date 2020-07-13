using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Data.Abstract;
using Data.Concrete;
using Ninject;
using Ninject.Syntax;
using NLog;
using Assmnts.UasServiceRef;

namespace Assmnts.Infrastructure
{
    public class NinjectDependencyResolver : IDependencyResolver
    {
        private IKernel kernel;

        public NinjectDependencyResolver() {
            kernel = new StandardKernel();
            AddBindings();
        }

        public object GetService(Type serviceType) {
            return kernel.TryGet(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType) {
            return kernel.GetAll(serviceType);
        }

        public IBindingToSyntax<T> Bind<T>() {
            return kernel.Bind<T>();
        }

        public IKernel Kernel {
            get { return kernel; }
        }

        private void AddBindings() {

            Bind<IAuthentication>().To<AuthenticationClient>();
            Bind<IFormsRepository>().To<FormsRepository>();
            Bind<ICachedFormsEntities>().To<CachedFormsEntities>().InSingletonScope();
            Bind<ILogger>().ToMethod(x => LogManager.GetLogger(x.Request.ParentContext.Request.Service.FullName));
        }
    }
}