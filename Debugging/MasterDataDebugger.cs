using System;
using System.Windows;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.Models;

namespace Einsatzueberwachung.Debugging
{
    /// <summary>
    /// Debug-Klasse um das MasterData-Speicherproblem zu analysieren
    /// </summary>
    public static class MasterDataDebugger
    {
        /// <summary>
        /// Testet das komplette PersonalEntry-Speichern
        /// </summary>
        public static void TestPersonalEntrySave()
        {
            try
            {
                LoggingService.Instance.LogInfo("=== TESTING PERSONAL ENTRY SAVE ===");
                
                var masterDataService = MasterDataService.Instance;
                
                // Erstelle ein Test-PersonalEntry
                var testPerson = new PersonalEntry
                {
                    Vorname = "Debug",
                    Nachname = "Test",
                    Skills = PersonalSkills.Hundefuehrer | PersonalSkills.Helfer,
                    Notizen = "Debug Test Person",
                    IsActive = true
                };
                
                LoggingService.Instance.LogInfo($"Created test person: {testPerson.FullName} with ID: {testPerson.Id}");
                LoggingService.Instance.LogInfo($"PersonalList count BEFORE add: {masterDataService.PersonalList.Count}");
                
                // Teste AddPersonal
                masterDataService.AddPersonal(testPerson);
                
                LoggingService.Instance.LogInfo($"PersonalList count AFTER add: {masterDataService.PersonalList.Count}");
                
                // Prüfe ob die Person in der Liste ist
                var foundPerson = masterDataService.GetPersonalById(testPerson.Id);
                if (foundPerson != null)
                {
                    LoggingService.Instance.LogInfo($"SUCCESS: Person found in list: {foundPerson.FullName}");
                }
                else
                {
                    LoggingService.Instance.LogError("ERROR: Person NOT found in list after add!");
                }
                
                LoggingService.Instance.LogInfo("=== PERSONAL ENTRY SAVE TEST COMPLETED ===");
                
                // Zeige Ergebnis
                MessageBox.Show($"Debug Test completed.\n" +
                               $"Person added: {testPerson.FullName}\n" +
                               $"PersonalList count: {masterDataService.PersonalList.Count}\n" +
                               $"Person found: {(foundPerson != null ? "YES" : "NO")}\n\n" +
                               $"Check the log for detailed information.",
                               "MasterData Debug Test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in debug test", ex);
                MessageBox.Show($"Debug test failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Testet das PersonalEditWindow Dialog-Flow
        /// </summary>
        public static void TestPersonalEditWindowFlow()
        {
            try
            {
                LoggingService.Instance.LogInfo("=== TESTING PERSONAL EDIT WINDOW FLOW ===");
                
                var editWindow = new Views.PersonalEditWindow();
                LoggingService.Instance.LogInfo("PersonalEditWindow created");
                
                var result = editWindow.ShowDialog();
                LoggingService.Instance.LogInfo($"PersonalEditWindow closed with result: {result}");
                
                if (result == true)
                {
                    var personEntry = editWindow.PersonalEntry;
                    LoggingService.Instance.LogInfo($"PersonalEntry returned: {personEntry.FullName}, Skills: {personEntry.Skills}");
                    
                    MessageBox.Show($"PersonalEditWindow test completed.\n" +
                                   $"Result: {result}\n" +
                                   $"Person: {personEntry.FullName}\n" +
                                   $"Skills: {personEntry.Skills}",
                                   "PersonalEditWindow Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    LoggingService.Instance.LogInfo("PersonalEditWindow was cancelled");
                    MessageBox.Show("PersonalEditWindow was cancelled", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                LoggingService.Instance.LogInfo("=== PERSONAL EDIT WINDOW FLOW TEST COMPLETED ===");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in PersonalEditWindow test", ex);
                MessageBox.Show($"PersonalEditWindow test failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Testet den kompletten MasterDataViewModel Flow
        /// </summary>
        public static void TestMasterDataViewModelFlow()
        {
            try
            {
                LoggingService.Instance.LogInfo("=== TESTING MASTER DATA VIEW MODEL FLOW ===");
                
                var masterDataService = MasterDataService.Instance;
                var initialCount = masterDataService.PersonalList.Count;
                
                LoggingService.Instance.LogInfo($"Initial PersonalList count: {initialCount}");
                
                // Simuliere das was MasterDataViewModel macht
                var editWindow = new Views.PersonalEditWindow();
                var result = editWindow.ShowDialog();
                
                if (result == true)
                {
                    var personalEntry = editWindow.PersonalEntry;
                    LoggingService.Instance.LogInfo($"Got PersonalEntry from dialog: {personalEntry.FullName}");
                    
                    // Simuliere MasterDataViewModel.ExecuteAddPersonal
                    LoggingService.Instance.LogInfo("Calling MasterDataService.AddPersonal...");
                    masterDataService.AddPersonal(personalEntry);
                    LoggingService.Instance.LogInfo("MasterDataService.AddPersonal completed");
                    
                    var finalCount = masterDataService.PersonalList.Count;
                    LoggingService.Instance.LogInfo($"Final PersonalList count: {finalCount}");
                    
                    MessageBox.Show($"MasterDataViewModel flow test completed.\n" +
                                   $"Initial count: {initialCount}\n" +
                                   $"Final count: {finalCount}\n" +
                                   $"Added: {personalEntry.FullName}\n" +
                                   $"Difference: {finalCount - initialCount}",
                                   "MasterDataViewModel Test", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                LoggingService.Instance.LogInfo("=== MASTER DATA VIEW MODEL FLOW TEST COMPLETED ===");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in MasterDataViewModel test", ex);
                MessageBox.Show($"MasterDataViewModel test failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
