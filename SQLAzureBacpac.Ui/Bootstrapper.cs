using System;
using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;
using Ninject;
using SQLAzureBacpac.Ui.ViewModels;

namespace SQLAzureBacpac.Ui
{
    public class Bootstrapper : BootstrapperBase
    {
        private IKernel _kernel;

        protected override void Configure()
        {
            _kernel = new StandardKernel();

            _kernel.Bind<IWindowManager>().To<WindowManager>().InSingletonScope();
            _kernel.Bind<IEventAggregator>().To<EventAggregator>().InSingletonScope();

        }

        public Bootstrapper()
        {
            Initialize();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<ShellViewModel>();
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            _kernel.Dispose();
            base.OnExit(sender, e);
        }

        protected override object GetInstance(Type service, string key)
        {
            if (service == null)
                throw new ArgumentNullException("service");

            return _kernel.Get(service);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _kernel.GetAll(service);
        }

        protected override void BuildUp(object instance)
        {
            _kernel.Inject(instance);
        }
    }
}
