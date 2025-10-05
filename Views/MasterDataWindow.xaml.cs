using System.Windows;
using Einsatzueberwachung.ViewModels;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// Interaction logic for MasterDataWindow.xaml
    /// Vollständig auf MVVM-Pattern umgestellt - minimales Code-Behind
    /// </summary>
    public partial class MasterDataWindow : Window
    {
        public MasterDataWindow()
        {
            InitializeComponent();
            
            // MVVM: DataContext wird über XAML gesetzt
            // Automatisches Laden der Daten beim Öffnen des Fensters
            Loaded += MasterDataWindow_Loaded;
        }

        private async void MasterDataWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // MVVM: LoadData-Command ausführen
            if (DataContext is MasterDataViewModel viewModel)
            {
                if (viewModel.LoadDataCommand.CanExecute(null))
                {
                    await ((RelayCommand)viewModel.LoadDataCommand).ExecuteAsync();
                }
            }
        }
    }
}
