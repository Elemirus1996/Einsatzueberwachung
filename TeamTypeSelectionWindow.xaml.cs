using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung
{
    public partial class TeamTypeSelectionWindow : Window
    {
        public MultipleTeamTypes SelectedMultipleTeamTypes { get; private set; }
        private readonly Dictionary<TeamType, CheckBox> _typeCheckBoxes = new Dictionary<TeamType, CheckBox>();

        public TeamTypeSelectionWindow(MultipleTeamTypes? currentSelection = null)
        {
            InitializeComponent();
            SelectedMultipleTeamTypes = currentSelection ?? new MultipleTeamTypes();
            
            // Apply current theme
            ApplyCurrentTheme();
            
            CreateTypeCheckBoxes();
            UpdateSelectedTypesDisplay();
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                // Subscribe to theme changes
                if (Services.ThemeService.Instance != null)
                {
                    var isDarkMode = Services.ThemeService.Instance.IsDarkMode;
                    // The window will automatically use the global theme resources
                    // No manual theme application needed as we use DynamicResource bindings
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error applying theme to TeamTypeSelectionWindow", ex);
            }
        }

        private void CreateTypeCheckBoxes()
        {
            try
            {
                // v1.5: Get all types EXCEPT Allgemein - exclude it completely
                var teamTypes = TeamTypeInfo.GetAllTypes()
                    .Where(t => t.Type != TeamType.Allgemein)  // Filter out Allgemein
                    .OrderBy(t => t.DisplayName);  // Sort alphabetically
                
                foreach (var typeInfo in teamTypes)
                {
                    var checkBox = new CheckBox
                    {
                        Content = new StackPanel
                        {
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = typeInfo.DisplayName,
                                    FontWeight = FontWeights.Medium,
                                    FontSize = 13
                                },
                                new TextBlock
                                {
                                    Text = typeInfo.Description,
                                    FontSize = 10,
                                    Foreground = new SolidColorBrush(Colors.Gray),
                                    TextWrapping = TextWrapping.Wrap,
                                    Margin = new Thickness(0, 1, 0, 0)
                                }
                            }
                        },
                        Style = (Style)FindResource("TeamTypeCheckBox"),
                        Tag = (SolidColorBrush)new BrushConverter().ConvertFrom(typeInfo.ColorHex),
                        IsChecked = SelectedMultipleTeamTypes.HasType(typeInfo.Type)
                    };

                    checkBox.Checked += (s, e) => OnTypeSelectionChanged();
                    checkBox.Unchecked += (s, e) => OnTypeSelectionChanged();

                    _typeCheckBoxes[typeInfo.Type] = checkBox;
                    TypeCheckBoxPanel.Children.Add(checkBox);
                }

                LoggingService.Instance.LogInfo($"TeamTypeSelection v1.5 - Created {teamTypes.Count()} type options (excluding Allgemein)");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error creating type checkboxes", ex);
            }
        }

        private void OnTypeSelectionChanged()
        {
            try
            {
                var selectedTypes = new HashSet<TeamType>();

                foreach (var kvp in _typeCheckBoxes)
                {
                    if (kvp.Value.IsChecked == true)
                    {
                        selectedTypes.Add(kvp.Key);
                    }
                }

                SelectedMultipleTeamTypes.SelectedTypes = selectedTypes;
                UpdateSelectedTypesDisplay();
                
                // Enable/disable OK button based on selection
                BtnOK.IsEnabled = selectedTypes.Any();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling type selection change", ex);
            }
        }

        private void UpdateSelectedTypesDisplay()
        {
            try
            {
                if (!SelectedMultipleTeamTypes.SelectedTypes.Any())
                {
                    TxtSelectedTypes.Text = "Keine Auswahl";
                    // UPDATED: Use design system color
                    TxtSelectedTypes.Foreground = (System.Windows.Media.Brush)FindResource("OnSurfaceVariant");
                }
                else
                {
                    TxtSelectedTypes.Text = SelectedMultipleTeamTypes.DisplayName;
                    // UPDATED: Use design system color
                    TxtSelectedTypes.Foreground = (System.Windows.Media.Brush)FindResource("OnSurface");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating selected types display", ex);
            }
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var checkBox in _typeCheckBoxes.Values)
                {
                    checkBox.IsChecked = false;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error clearing all selections", ex);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!SelectedMultipleTeamTypes.SelectedTypes.Any())
                {
                    MessageBox.Show("Bitte wählen Sie mindestens eine Spezialisierung aus.", 
                        "Keine Auswahl", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                LoggingService.Instance.LogInfo($"Team types selected: {SelectedMultipleTeamTypes.DisplayName}");
                DialogResult = true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error confirming type selection", ex);
                MessageBox.Show($"Fehler beim Bestätigen der Auswahl: {ex.Message}", 
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
