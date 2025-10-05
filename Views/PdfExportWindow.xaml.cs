using System;
using System.Collections.Generic;
using System.Windows;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// Interaction logic for PdfExportWindow.xaml - MVVM Implementation v1.9.0
    /// PDF-Export mit vollständiger Orange-Design-Integration
    /// </summary>
    public partial class PdfExportWindow : Window
    {
        private PdfExportViewModel? _viewModel;

        public PdfExportWindow(EinsatzData einsatzData, List<Team> teams)
        {
            InitializeComponent();
            InitializeViewModel(einsatzData, teams);
            
            LoggingService.Instance.LogInfo("PdfExportWindow initialized with MVVM + Orange Design v1.9.0");
        }

        private void InitializeViewModel(EinsatzData einsatzData, List<Team> teams)
        {
            try
            {
                _viewModel = new PdfExportViewModel(einsatzData, teams);
                DataContext = _viewModel;

                // Subscribe to ViewModel events
                _viewModel.ExportSuccessful += OnExportSuccessful;
                _viewModel.ExportCancelled += OnExportCancelled;
                
                LoggingService.Instance.LogInfo("PdfExportViewModel initialized and connected");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing PdfExportViewModel", ex);
                
                // Fallback: Show error message and close window
                MessageBox.Show($"Fehler beim Initialisieren des PDF-Export ViewModels:\n{ex.Message}", 
                               "Initialisierung fehlgeschlagen", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        #region Event Handlers

        private void OnExportSuccessful()
        {
            try
            {
                DialogResult = true;
                Close();
                LoggingService.Instance.LogInfo("PDF export completed successfully, window closing");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling export successful event", ex);
            }
        }

        private void OnExportCancelled()
        {
            try
            {
                DialogResult = false;
                Close();
                LoggingService.Instance.LogInfo("PDF export cancelled, window closing");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling export cancelled event", ex);
            }
        }

        #endregion

        #region Window Lifecycle

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Cleanup ViewModel events
                if (_viewModel != null)
                {
                    _viewModel.ExportSuccessful -= OnExportSuccessful;
                    _viewModel.ExportCancelled -= OnExportCancelled;
                    _viewModel.Dispose();
                    _viewModel = null;
                }
                
                base.OnClosed(e);
                LoggingService.Instance.LogInfo("PdfExportWindow closed and cleaned up via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during PdfExportWindow cleanup", ex);
                base.OnClosed(e);
            }
        }

        #endregion
    }
}
