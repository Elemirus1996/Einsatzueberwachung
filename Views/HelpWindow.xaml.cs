using System.Windows;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    public partial class HelpWindow : Window
    {
        private readonly HelpViewModel _viewModel;

        public HelpWindow()
        {
            InitializeComponent();
            
            _viewModel = new HelpViewModel();
            DataContext = _viewModel;
            
            LoggingService.Instance?.LogInfo("HelpWindow v1.9.0 initialized with MVVM pattern and Orange design");
        }

        protected override void OnClosed(System.EventArgs e)
        {
            LoggingService.Instance?.LogInfo("HelpWindow closed");
            base.OnClosed(e);
        }
    }
}
