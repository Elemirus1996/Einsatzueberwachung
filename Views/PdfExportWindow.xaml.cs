using System;
using System.Collections.Generic;
using System.Windows;
using Einsatzueberwachung.ViewModels;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// PdfExportWindow - Enhanced with Theme Integration v1.9.0
    /// Now inherits from BaseThemeWindow for automatic theme support
    /// PDF-Export mit vollständiger Orange-Design-Integration und Unified Theme System
    /// </summary>
    public partial class PdfExportWindow : BaseThemeWindow
    {
        private PdfExportViewModel? _viewModel;

        public PdfExportWindow(EinsatzData einsatzData, List<Team> teams)
        {
            InitializeComponent();
            InitializeThemeSupport(); // Initialize theme after component initialization
            InitializeViewModel(einsatzData, teams);
            
            LoggingService.Instance.LogInfo("PdfExportWindow initialized with Unified Theme System v1.9.0");
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

        protected override void ApplyThemeToWindow(bool isDarkMode)
        {
            try
            {
                // Apply theme-specific styling
                base.ApplyThemeToWindow(isDarkMode);
                
                LoggingService.Instance.LogInfo($"Theme applied to PdfExportWindow: {(isDarkMode ? "Dark" : "Light")} mode with Orange design");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to PdfExportWindow", ex);
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
                
                LoggingService.Instance.LogInfo("PdfExportWindow closed and cleaned up via MVVM");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during PdfExportWindow cleanup", ex);
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Check if export is in progress
                if (_viewModel?.IsExporting == true)
                {
                    var result = MessageBox.Show(
                        "Ein PDF-Export ist noch aktiv. Möchten Sie den Vorgang abbrechen und das Fenster schließen?",
                        "Export aktiv",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                    
                    // Cancel the export
                    _viewModel?.CancelExportCommand?.Execute(null);
                }
                
                base.OnClosing(e);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during PdfExportWindow closing", ex);
                base.OnClosing(e);
            }
        }

        #endregion
    }
}
