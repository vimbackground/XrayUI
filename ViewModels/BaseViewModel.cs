namespace XrayUI.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        protected BaseViewModel()
        {
            Title = string.Empty;
        }

        [ObservableProperty]
        public partial string Title { get; set; }
    }
}

