using Caliburn.Micro;

namespace SQLAzureBacpac.Ui.ViewModels
{
    public class ExceptionViewModel : Screen
    {
        public string Message { get; set; }

        protected override void OnViewLoaded(object view)
        {
            DisplayName = "Oops...";
            base.OnViewLoaded(view);
        }

        public void Cancel()
        {
            TryClose();
        }
    }
}
