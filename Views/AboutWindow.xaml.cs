using System.Windows;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            
            // ViewModel als DataContext setzen
            var viewModel = new AboutViewModel();
            viewModel.RequestClose += (sender, args) => 
            {
                DialogResult = true;
            };
            viewModel.ShowMessage += (sender, message) =>
            {
                MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            
            DataContext = viewModel;
            
            LoggingService.Instance?.LogInfo("AboutWindow loaded with enhanced MVVM pattern and command support");
        }
    }
}
