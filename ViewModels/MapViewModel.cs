using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI;
using NetTopologySuite.Geometries;
using MapsuiColor = Mapsui.Styles.Color;
using MediaColor = System.Windows.Media.Color;
using System.Threading.Tasks;
using Mapsui.Utilities;
using System.Collections.Generic;

namespace Einsatzueberwachung.ViewModels
{
    public class MapViewModel : BaseViewModel
    {
        private readonly EinsatzData _einsatzData;
        private readonly List<Team> _availableTeams;
        private string _searchAddress;
        
        private int _colorIndex = 0;

        // Map & Layers
        private Map? _map;
        private bool _isDrawing = false;
        private bool _showSatellite = false;
        private SearchArea? _selectedArea;
        private ObservableCollection<Coordinate> _currentDrawingPoints = new();
        private bool _isSettingElw = false;

        public Map? Map
        {
            get => _map;
            set { _map = value; OnPropertyChanged(); }
        }

        public bool IsDrawing
        {
            get => _isDrawing;
            set { _isDrawing = value; OnPropertyChanged(); }
        }

        public bool IsSettingElw
        {
            get => _isSettingElw;
            set { _isSettingElw = value; OnPropertyChanged(); }
        }

        public bool ShowSatellite
        {
            get => _showSatellite;
            set 
            { 
                _showSatellite = value; 
                OnPropertyChanged();
                UpdateMapLayer();
            }
        }

        public SearchArea? SelectedArea
        {
            get => _selectedArea;
            set { _selectedArea = value; OnPropertyChanged(); }
        }

        public string SearchAddress
        {
            get => _searchAddress;
            set { _searchAddress = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SearchArea> SearchAreas { get; } = new();
        public ObservableCollection<string> AvailableTeams { get; } = new();
        public ObservableCollection<Coordinate> CurrentDrawingPoints
        {
            get => _currentDrawingPoints;
            set { _currentDrawingPoints = value; OnPropertyChanged(); }
        }

        public ICommand StartDrawingCommand { get; }
        public ICommand FinishDrawingCommand { get; }
        public ICommand CancelDrawingCommand { get; }
        public ICommand DeleteAreaCommand { get; }
        public ICommand SearchAddressCommand { get; }
        public ICommand PrintMapCommand { get; }
        public ICommand ToggleMapTypeCommand { get; }
        public ICommand SelectAreaCommand { get; }
        public ICommand AssignTeamToAreaCommand { get; }
        public ICommand SaveMapDataCommand { get; }
        public ICommand SetElwPositionCommand { get; }
        public ICommand ClearElwPositionCommand { get; }

        public MapViewModel(EinsatzData einsatzData, List<Team> teams, string initialAddress = "")
        {
            _einsatzData = einsatzData;
            _searchAddress = initialAddress;
            _availableTeams = teams ?? new List<Team>();

            // Initialisiere Karte
            InitializeMap();

            // Lade verfügbare Teams aus MainWindow
            LoadAvailableTeams(teams);

            // Commands
            StartDrawingCommand = new RelayCommand(ExecuteStartDrawing, CanExecuteStartDrawing);
            FinishDrawingCommand = new RelayCommand(ExecuteFinishDrawing, CanExecuteFinishDrawing);
            CancelDrawingCommand = new RelayCommand(ExecuteCancelDrawing, CanExecuteCancelDrawing);
            DeleteAreaCommand = new RelayCommand(ExecuteDeleteArea, CanExecuteDeleteArea);
            SearchAddressCommand = new RelayCommand(ExecuteSearchAddress);
            PrintMapCommand = new RelayCommand(ExecutePrintMap);
            ToggleMapTypeCommand = new RelayCommand(ExecuteToggleMapType);
            SelectAreaCommand = new RelayCommand<SearchArea>(ExecuteSelectArea);
            AssignTeamToAreaCommand = new RelayCommand(ExecuteAssignTeamToArea, CanExecuteAssignTeamToArea);
            SaveMapDataCommand = new RelayCommand(ExecuteSaveMapData);
            SetElwPositionCommand = new RelayCommand(ExecuteSetElwPosition, CanExecuteSetElwPosition);
            ClearElwPositionCommand = new RelayCommand(ExecuteClearElwPosition, CanExecuteClearElwPosition);

            LoggingService.Instance.LogInfo($"MapViewModel initialized with {teams?.Count ?? 0} teams");

            // Wenn eine Adresse angegeben wurde, zoome dorthin
            if (!string.IsNullOrWhiteSpace(initialAddress))
            {
                ExecuteSearchAddress();
            }
        }

        private void InitializeMap()
        {
            try
            {
                LoggingService.Instance.LogInfo("Initializing Mapsui map...");

                // Erstelle die Map
                Map = new Map
                {
                    CRS = "EPSG:3857" // Web Mercator
                };

                // Füge OpenStreetMap Layer hinzu mit Error-Handling
                try
                {
                    var tileLayer = OpenStreetMap.CreateTileLayer();
                    tileLayer.Name = "OpenStreetMap";
                    
                    // WICHTIG: Setze MinVisible und MaxVisible für stabiles Rendering
                    tileLayer.MinVisible = 0;
                    tileLayer.MaxVisible = double.MaxValue;
                    
                    Map.Layers.Add(tileLayer);
                    LoggingService.Instance.LogInfo("OSM TileLayer added successfully");
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("Error adding OSM layer", ex);
                    // Map ohne TileLayer fortsetzen
                }

                // Erstelle Layer für Suchgebiete
                var searchAreasLayer = new WritableLayer
                {
                    Name = "SearchAreas",
                    Style = null,
                    IsMapInfoLayer = true
                };
                Map.Layers.Add(searchAreasLayer);
                LoggingService.Instance.LogInfo("SearchAreas layer added");

                // Erstelle Layer für temporäre Zeichnungen
                var drawingLayer = new WritableLayer
                {
                    Name = "DrawingLayer",
                    Style = null,
                    IsMapInfoLayer = true
                };
                Map.Layers.Add(drawingLayer);
                LoggingService.Instance.LogInfo("DrawingLayer added");

                // Erstelle Layer für ELW-Marker
                var elwLayer = new WritableLayer
                {
                    Name = "ElwLayer",
                    Style = null,
                    IsMapInfoLayer = true
                };
                Map.Layers.Add(elwLayer);
                LoggingService.Instance.LogInfo("ElwLayer added");

                // Setze initialen Zoom und Position
                // Standard: Deutschland Zentrum
                var initialLon = 10.4515; // Deutschland Mitte
                var initialLat = 51.1657;

                // Konvertiere zu Web Mercator
                var mercatorCenter = SphericalMercator.FromLonLat(initialLon, initialLat);
                
                // Setze Navigator mit sicheren Werten
                Map.Navigator.CenterOn(mercatorCenter.x, mercatorCenter.y);
                Map.Navigator.ZoomTo(1000000); // Weiterer Zoom für bessere Übersicht
                
                // Lade existierende SearchAreas
                LoadExistingSearchAreas();

                // Lade ELW-Position falls vorhanden
                if (_einsatzData?.ElwPosition != null)
                {
                    var elwPos = _einsatzData.ElwPosition.Value;
                    DrawElwMarker(elwPos.Latitude, elwPos.Longitude);
                    LoggingService.Instance.LogInfo($"Loaded existing ELW position: Lat={elwPos.Latitude:F6}, Lon={elwPos.Longitude:F6}");
                }

                // Geocoding asynchron durchführen NACH der Map-Initialisierung
                if (!string.IsNullOrWhiteSpace(_searchAddress))
                {
                    _ = GeocodeAndCenterAsync(_searchAddress);
                }

                LoggingService.Instance.LogInfo("Mapsui map initialized successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error initializing Mapsui map", ex);
                MessageBox.Show($"Fehler beim Initialisieren der Karte:\n{ex.Message}\n\nDie Karte wird mit Standardeinstellungen geladen.",
                    "Karten-Initialisierung", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                // Fallback: Erstelle minimale Map
                try
                {
                    Map = new Map
                    {
                        CRS = "EPSG:3857"
                    };
                    
                    // Deutschland Zentrum in Web Mercator
                    var fallbackCenter = SphericalMercator.FromLonLat(10.4515, 51.1657);
                    Map.Navigator.CenterOn(fallbackCenter.x, fallbackCenter.y);
                    Map.Navigator.ZoomTo(1000000); // Weiterer Zoom für mehr Stabilität
                }
                catch (Exception fallbackEx)
                {
                    LoggingService.Instance.LogError("Critical error in map fallback initialization", fallbackEx);
                }
            }
        }
        
        private async Task GeocodeAndCenterAsync(string address)
        {
            try
            {
                LoggingService.Instance.LogInfo($"Geocoding address asynchronously: {address}");
                var result = await GeocodeAddress(address);
                
                if (result.HasValue && Map != null)
                {
                    var mercatorCenter = SphericalMercator.FromLonLat(result.Value.Longitude, result.Value.Latitude);
                    Map.Navigator.CenterOn(mercatorCenter.x, mercatorCenter.y);
                    Map.Navigator.ZoomTo(20000); // Näher ran zoomen wenn Adresse gefunden
                    
                    LoggingService.Instance.LogInfo($"Map centered on geocoded address: {address}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogWarning($"Could not geocode address: {ex.Message}");
            }
        }
        
        private MediaColor GetNextColor()
        {
            var colors = new[]
            {
                MediaColor.FromRgb(255, 87, 0),    // Orange
                MediaColor.FromRgb(76, 175, 80),   // Green
                MediaColor.FromRgb(33, 150, 243),  // Blue
                MediaColor.FromRgb(156, 39, 176),  // Purple
                MediaColor.FromRgb(255, 193, 7),   // Yellow
                MediaColor.FromRgb(244, 67, 54),   // Red
                MediaColor.FromRgb(0, 188, 212),   // Cyan
                MediaColor.FromRgb(255, 152, 0)    // Amber
            };

            return colors[_colorIndex++ % colors.Length];
        }
        
        private void LoadExistingSearchAreas()
        {
            try
            {
                if (_einsatzData?.SearchAreas == null || !_einsatzData.SearchAreas.Any())
                {
                    LoggingService.Instance.LogInfo("No existing search areas to load");
                    return;
                }

                foreach (var area in _einsatzData.SearchAreas)
                {
                    SearchAreas.Add(area);
                    DrawSearchAreaOnMap(area);
                }

                LoggingService.Instance.LogInfo($"Loaded {SearchAreas.Count} existing search areas from EinsatzData");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading existing search areas", ex);
            }
        }

        private void UpdateMapLayer()
        {
            try
            {
                if (Map == null) return;

                // Für Mapsui 5.0 Beta - einfach nur OSM verwenden
                // Satelliten-Ansicht ist in dieser Beta-Version nicht vollständig unterstützt
                ShowSatellite = false;

                LoggingService.Instance.LogInfo("Map layer: OpenStreetMap only (Satellite view not available in Mapsui 5.0 Beta)");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating map layer", ex);
            }
        }

        private void LoadAvailableTeams(List<Team> teams)
        {
            try
            {
                AvailableTeams.Clear();
                
                if (teams == null || !teams.Any())
                {
                    LoggingService.Instance.LogInfo("No teams available for assignment");
                    return;
                }

                foreach (var team in teams)
                {
                    AvailableTeams.Add(team.TeamName);
                }

                LoggingService.Instance.LogInfo($"Loaded {AvailableTeams.Count} available teams for area assignment");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error loading available teams", ex);
            }
        }

        private void RemoveSearchAreaFromMap(SearchArea area)
        {
            try
            {
                if (Map == null) return;

                var searchAreaLayer = Map.Layers.FirstOrDefault(l => l.Name == "SearchAreas") as WritableLayer;
                if (searchAreaLayer == null) return;

                // Find and remove features associated with this area
                var featuresToRemove = searchAreaLayer.GetFeatures()
                    .Where(f => f["AreaId"]?.ToString() == area.Id)
                    .ToList();

                foreach (var feature in featuresToRemove)
                {
                    searchAreaLayer.TryRemove(feature);
                }

                Map.RefreshData();
                LoggingService.Instance.LogInfo($"Search area removed from map: {area.Name}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error removing search area from map", ex);
            }
        }

        private bool CanExecuteStartDrawing()
        {
            return !IsDrawing && !IsSettingElw;
        }

        private void ExecuteStartDrawing()
        {
            IsDrawing = true;
            CurrentDrawingPoints.Clear();
            
            // Aktualisiere Command-States
            ((RelayCommand)StartDrawingCommand).RaiseCanExecuteChanged();
            ((RelayCommand)FinishDrawingCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelDrawingCommand).RaiseCanExecuteChanged();
            
            LoggingService.Instance.LogInfo("Started drawing search area");
        }

        private bool CanExecuteFinishDrawing()
        {
            return IsDrawing && CurrentDrawingPoints.Count >= 3;
        }

        private void ExecuteFinishDrawing()
        {
            try
            {
                if (CurrentDrawingPoints.Count < 3)
                {
                    MessageBox.Show("Ein Suchgebiet benötigt mindestens 3 Punkte.",
                        "Ungültiges Gebiet", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // WICHTIG: CurrentDrawingPoints enthält Web Mercator Koordinaten (x, y)
                // Diese müssen zurück zu Lat/Lon konvertiert werden!
                var latLonCoords = CurrentDrawingPoints
                    .Select(c =>
                    {
                        var latLon = SphericalMercator.ToLonLat(c.X, c.Y);
                        return (latLon.lat, latLon.lon); // (Latitude, Longitude)
                    })
                    .ToList();

                // Erstelle neues Suchgebiet mit korrekten Lat/Lon Koordinaten
                var searchArea = new SearchArea
                {
                    Name = $"Gebiet {SearchAreas.Count + 1}",
                    Color = GetNextColor(),
                    Coordinates = latLonCoords
                };

                SearchAreas.Add(searchArea);
                
                // WICHTIG: Synchronisiere mit EinsatzData für Persistence
                if (_einsatzData != null)
                {
                    _einsatzData.SearchAreas.Add(searchArea);
                    LoggingService.Instance.LogInfo($"Search area added to EinsatzData: {searchArea.Name}");
                }
                
                // Lösche temporäre Zeichnung
                ClearDrawingLayer();
                
                // Zeichne fertiges Suchgebiet
                DrawSearchAreaOnMap(searchArea);

                IsDrawing = false;
                CurrentDrawingPoints.Clear();

                // Aktualisiere Command-States
                ((RelayCommand)StartDrawingCommand).RaiseCanExecuteChanged();
                ((RelayCommand)FinishDrawingCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CancelDrawingCommand).RaiseCanExecuteChanged();

                LoggingService.Instance.LogInfo($"Finished drawing search area: {searchArea.Name} with {searchArea.Coordinates.Count} points - First coord: Lat={searchArea.Coordinates[0].Latitude}, Lon={searchArea.Coordinates[0].Longitude}, Area: {searchArea.FormattedArea}");
                
                MessageBox.Show($"Suchgebiet '{searchArea.Name}' wurde erfolgreich erstellt!\n\n" +
                               $"Punkte: {searchArea.Coordinates.Count}\n" +
                               $"Fläche: {searchArea.FormattedArea}\n\n" +
                               $"Sie können dieses Gebiet nun einem Team zuordnen.",
                               "Suchgebiet erstellt", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error finishing drawing", ex);
                MessageBox.Show($"Fehler beim Erstellen des Suchgebiets: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteCancelDrawing()
        {
            return IsDrawing;
        }

        private void ExecuteCancelDrawing()
        {
            IsDrawing = false;
            CurrentDrawingPoints.Clear();
            
            // Lösche temporäre Zeichnung von der Karte
            ClearDrawingLayer();
            
            // Aktualisiere Command-States
            ((RelayCommand)StartDrawingCommand).RaiseCanExecuteChanged();
            ((RelayCommand)FinishDrawingCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelDrawingCommand).RaiseCanExecuteChanged();
            
            LoggingService.Instance.LogInfo("Cancelled drawing search area");
        }

        private void ClearDrawingLayer()
        {
            try
            {
                if (Map == null) return;

                var drawingLayer = Map.Layers.FirstOrDefault(l => l.Name == "DrawingLayer") as WritableLayer;
                if (drawingLayer != null)
                {
                    drawingLayer.Clear();
                    Map.RefreshData();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error clearing drawing layer", ex);
            }
        }

        private bool CanExecuteDeleteArea()
        {
            return SelectedArea != null;
        }

        private void ExecuteDeleteArea()
        {
            if (SelectedArea == null) return;

            var result = MessageBox.Show(
                $"Möchten Sie das Suchgebiet '{SelectedArea.Name}' wirklich löschen?",
                "Suchgebiet löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SearchAreas.Remove(SelectedArea);
                RemoveSearchAreaFromMap(SelectedArea);
                SelectedArea = null;
                LoggingService.Instance.LogInfo("Search area deleted");
            }
        }

        private async void ExecuteSearchAddress()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchAddress))
                {
                    MessageBox.Show("Bitte geben Sie eine Adresse ein.",
                        "Keine Adresse", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                LoggingService.Instance.LogInfo($"Searching for address: {SearchAddress}");

                // Verwende Nominatim Geocoding API
                var coordinates = await GeocodeAddress(SearchAddress);
                
                if (coordinates.HasValue && Map != null)
                {
                    var point = SphericalMercator.FromLonLat(coordinates.Value.Longitude, coordinates.Value.Latitude).ToMPoint();
                    Map.Navigator.CenterOn(point);
                    Map.Navigator.ZoomTo(14); // Näher ranzoomen
                   
                    LoggingService.Instance.LogInfo($"Address found and centered: {coordinates.Value.Latitude}, {coordinates.Value.Longitude}");
                }
                else
                {
                    MessageBox.Show("Die Adresse konnte nicht gefunden werden. Bitte überprüfen Sie die Eingabe.",
                        "Adresse nicht gefunden", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error searching address", ex);
                MessageBox.Show($"Fehler bei der Adresssuche: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task<(double Latitude, double Longitude)?> GeocodeAddress(string address)
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "EinsatzueberwachungApp/1.0");

                var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";
                var response = await httpClient.GetStringAsync(url);

                // Parse JSON response
                if (response.Contains("\"lat\":"))
                {
                    var latStart = response.IndexOf("\"lat\":\"") + 7;
                    var latEnd = response.IndexOf("\"", latStart);
                    var lat = double.Parse(response.Substring(latStart, latEnd - latStart).Replace(".", ","));

                    var lonStart = response.IndexOf("\"lon\":\"") + 7;
                    var lonEnd = response.IndexOf("\"", lonStart);
                    var lon = double.Parse(response.Substring(lonStart, lonEnd - lonStart).Replace(".", ","));

                    return (lat, lon);
                }

                return null;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Geocoding error", ex);
                return null;
            }
        }

        private void ExecutePrintMap()
        {
            try
            {
                var printOptions = MessageBox.Show(
                    "Möchten Sie die Karte als PDF exportieren?\n\n" +
                    "Ja = PDF-Export mit Details\n" +
                    "Nein = Abbrechen",
                    "Karte drucken",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (printOptions == MessageBoxResult.No)
                {
                    return;
                }

                // PDF-Export
                ExportMapToPdf();
                
                LoggingService.Instance.LogInfo("Print map requested");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error printing map", ex);
                MessageBox.Show($"Fehler beim Drucken: {ex.Message}",
                    "Druckfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportMapToPdf()
        {
            try
            {
                LoggingService.Instance.LogInfo("Starting PDF export...");
                
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Karte als PDF speichern",
                    Filter = "PDF Dateien (*.pdf)|*.pdf",
                    FileName = $"Karte_{_einsatzData?.Einsatzort ?? "Einsatz"}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                    DefaultExt = ".pdf"
                };

                if (saveDialog.ShowDialog() != true)
                {
                    LoggingService.Instance.LogInfo("PDF export cancelled by user");
                    return;
                }

                LoggingService.Instance.LogInfo($"PDF save path: {saveDialog.FileName}");

                // Erstelle temporäres Karten-Bild
                MessageBox.Show("Erstelle Screenshot der Karte...\n\nBitte warten Sie einen Moment.",
                    "PDF-Export", MessageBoxButton.OK, MessageBoxImage.Information);
                
                string? mapImagePath = CreateMapScreenshot();

                if (mapImagePath == null || !File.Exists(mapImagePath))
                {
                    LoggingService.Instance.LogError("Map screenshot was not created or file does not exist");
                    MessageBox.Show("Fehler beim Erstellen des Karten-Screenshots.\n\nBitte stellen Sie sicher, dass die Karte vollständig geladen ist.",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LoggingService.Instance.LogInfo($"Screenshot created successfully at: {mapImagePath}");
                LoggingService.Instance.LogInfo($"Screenshot file size: {new FileInfo(mapImagePath).Length} bytes");

                // Erstelle PDF
                var pdfService = new MapPdfExportService();
                
                LoggingService.Instance.LogInfo("Calling MapPdfExportService.ExportMapToPdf...");
                var success = pdfService.ExportMapToPdf(
                    saveDialog.FileName,
                    _einsatzData,
                    SearchAreas.ToList(),
                    mapImagePath);

                // Lösche temporäres Bild
                try
                {
                    if (File.Exists(mapImagePath))
                    {
                        File.Delete(mapImagePath);
                        LoggingService.Instance.LogInfo("Temporary screenshot deleted");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogWarning($"Could not delete temporary screenshot: {ex.Message}");
                }

                if (success && File.Exists(saveDialog.FileName))
                {
                    var fileInfo = new FileInfo(saveDialog.FileName);
                    LoggingService.Instance.LogInfo($"Map exported to PDF successfully: {saveDialog.FileName} ({fileInfo.Length} bytes)");
                    
                    var result = MessageBox.Show(
                        $"Karte wurde erfolgreich als PDF gespeichert!\n\n" +
                        $"Datei: {saveDialog.FileName}\n" +
                        $"Größe: {fileInfo.Length / 1024} KB\n\n" +
                        $"Möchten Sie die Datei öffnen?",
                        "PDF-Export erfolgreich",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = saveDialog.FileName,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.LogError("Could not open PDF file", ex);
                            MessageBox.Show($"PDF wurde gespeichert, konnte aber nicht geöffnet werden:\n{ex.Message}",
                                "Hinweis", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                else
                {
                    LoggingService.Instance.LogError($"PDF export failed or file was not created. Success={success}, FileExists={File.Exists(saveDialog.FileName)}");
                    MessageBox.Show($"Fehler beim PDF-Export.\n\nBitte prüfen Sie die Log-Datei für Details.\n\nErwartet: {saveDialog.FileName}",
                        "PDF-Export fehlgeschlagen", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error exporting map to PDF", ex);
                MessageBox.Show($"Fehler beim PDF-Export:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string? CreateMapScreenshot()
        {
            try
            {
                // Temporärer Pfad für Screenshot
                var tempPath = Path.Combine(Path.GetTempPath(), $"map_screenshot_{Guid.NewGuid()}.png");
                
                LoggingService.Instance.LogInfo($"Requesting map screenshot at: {tempPath}");
                
                // Notify MapWindow to create screenshot
                MapScreenshotRequested?.Invoke(this, new MapScreenshotEventArgs(tempPath));
                
                // Warte auf Erstellung (mit Timeout)
                int attempts = 0;
                int maxAttempts = 20; // 2 Sekunden max
                
                while (attempts < maxAttempts)
                {
                    System.Threading.Thread.Sleep(100);
                    
                    if (File.Exists(tempPath))
                    {
                        // Warte noch etwas um sicherzustellen dass das Schreiben abgeschlossen ist
                        System.Threading.Thread.Sleep(200);
                        
                        var fileInfo = new FileInfo(tempPath);
                        if (fileInfo.Length > 0)
                        {
                            LoggingService.Instance.LogInfo($"Screenshot created successfully: {fileInfo.Length} bytes");
                            return tempPath;
                        }
                    }
                    
                    attempts++;
                }
                
                LoggingService.Instance.LogError($"Screenshot was not created after {maxAttempts * 100}ms");
                return null;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error creating map screenshot", ex);
                return null;
            }
        }

        private void ExecuteToggleMapType()
        {
            ShowSatellite = !ShowSatellite;
        }

        private bool CanExecuteSetElwPosition()
        {
            return !IsDrawing && !IsSettingElw;
        }

        private void ExecuteSetElwPosition()
        {
            try
            {
                IsSettingElw = true;
                
                // Aktualisiere Command-States
                ((RelayCommand)SetElwPositionCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StartDrawingCommand).RaiseCanExecuteChanged();
                
                LoggingService.Instance.LogInfo("ELW position setting mode activated");
                
                MessageBox.Show("Klicken Sie auf die Karte, um die ELW-Position (Einsatzleitwagen) zu setzen.\n\n" +
                               "Der ELW-Marker dient als Orientierungspunkt für die Teams.",
                               "ELW-Position setzen", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error activating ELW position mode", ex);
            }
        }

        private bool CanExecuteClearElwPosition()
        {
            return _einsatzData?.ElwPosition != null;
        }

        private void ExecuteClearElwPosition()
        {
            try
            {
                if (_einsatzData != null)
                {
                    _einsatzData.ElwPosition = null;
                }
                
                // Entferne ELW-Marker von der Karte
                ClearElwLayer();
                
                // Aktualisiere Command-State
                ((RelayCommand)ClearElwPositionCommand).RaiseCanExecuteChanged();
                
                LoggingService.Instance.LogInfo("ELW position cleared");
                
                MessageBox.Show("ELW-Position wurde entfernt.",
                    "ELW entfernt", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error clearing ELW position", ex);
            }
        }

        public void SetElwPositionFromMap(double latitude, double longitude)
        {
            try
            {
                if (_einsatzData == null) return;
                
                _einsatzData.ElwPosition = (latitude, longitude);
                
                // Zeichne ELW-Marker auf der Karte
                DrawElwMarker(latitude, longitude);
                
                IsSettingElw = false;
                
                // Aktualisiere Command-States
                ((RelayCommand)SetElwPositionCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ClearElwPositionCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StartDrawingCommand).RaiseCanExecuteChanged();
                
                LoggingService.Instance.LogInfo($"ELW position set to: Lat={latitude:F6}, Lon={longitude:F6}");
                
                MessageBox.Show($"ELW-Position gesetzt!\n\n" +
                               $"Latitude: {latitude:F6}\n" +
                               $"Longitude: {longitude:F6}",
                               "ELW Position", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error setting ELW position from map", ex);
            }
        }

        private void DrawElwMarker(double latitude, double longitude)
        {
            try
            {
                if (Map == null) return;

                var elwLayer = Map.Layers.FirstOrDefault(l => l.Name == "ElwLayer") as WritableLayer;
                if (elwLayer == null) return;

                // Lösche alte ELW-Marker
                elwLayer.Clear();

                // Konvertiere zu Web Mercator
                var mercator = SphericalMercator.FromLonLat(longitude, latitude);

                // Erstelle ELW-Marker als roter Stern
                var pointGeometry = new NetTopologySuite.Geometries.Point(mercator.x, mercator.y);
                var elwFeature = new GeometryFeature { Geometry = pointGeometry };
                
                // Großer roter Marker für ELW
                elwFeature.Styles.Add(new SymbolStyle
                {
                    SymbolType = SymbolType.Triangle,
                    SymbolScale = 1.5,
                    Fill = new Brush(new MapsuiColor(220, 20, 60)), // Crimson Red
                    Outline = new Pen(new MapsuiColor(255, 255, 255), 3) // White border
                });

                elwLayer.Add(elwFeature);
                Map.RefreshData();

                LoggingService.Instance.LogInfo($"ELW marker drawn at Lat={latitude:F6}, Lon={longitude:F6}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error drawing ELW marker", ex);
            }
        }

        private void ClearElwLayer()
        {
            try
            {
                if (Map == null) return;

                var elwLayer = Map.Layers.FirstOrDefault(l => l.Name == "ElwLayer") as WritableLayer;
                if (elwLayer != null)
                {
                    elwLayer.Clear();
                    Map.RefreshData();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error clearing ELW layer", ex);
            }
        }

        private bool CanExecuteAssignTeamToArea()
        {
            return SelectedArea != null && AvailableTeams.Any();
        }

        private void ExecuteAssignTeamToArea()
        {
            try
            {
                if (SelectedArea == null)
                {
                    MessageBox.Show("Bitte wählen Sie zuerst ein Suchgebiet aus.",
                        "Kein Gebiet ausgewählt", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Erstelle einfachen ComboBox-Dialog für Team-Auswahl
                var selectedTeam = ShowTeamSelectionDialog();
                
                if (!string.IsNullOrEmpty(selectedTeam))
                {
                    SelectedArea.AssignedTeam = selectedTeam;
                    
                    // Aktualisiere die Farbe basierend auf dem Team
                    UpdateAreaColorForTeam(SelectedArea);
                    
                    // Zeichne Area neu
                    RemoveSearchAreaFromMap(SelectedArea);
                    DrawSearchAreaOnMap(SelectedArea);
                    
                    // Speichere Änderungen
                    SaveMapDataToEinsatzData();
                    
                    LoggingService.Instance.LogInfo($"Team '{selectedTeam}' assigned to area '{SelectedArea.Name}'");
                    
                    MessageBox.Show($"Team '{selectedTeam}' wurde dem Suchgebiet '{SelectedArea.Name}' zugeordnet.",
                        "Team zugeordnet", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error assigning team to area", ex);
                MessageBox.Show($"Fehler beim Zuordnen des Teams: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteSaveMapData()
        {
            try
            {
                SaveMapDataToEinsatzData();
                
                MessageBox.Show($"Kartendaten gespeichert!\n\n" +
                               $"Suchgebiete: {SearchAreas.Count}\n" +
                               $"Zugeordnete Teams: {SearchAreas.Count(a => !string.IsNullOrEmpty(a.AssignedTeam))}",
                               "Speichern erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                
                LoggingService.Instance.LogInfo($"Map data saved - {SearchAreas.Count} areas");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error saving map data", ex);
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string? ShowTeamSelectionDialog()
        {
            try
            {
                // Erstelle ein einfaches Auswahl-Fenster
                var dialog = new Window
                {
                    Title = "Team auswählen",
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow
                };

                var stackPanel = new System.Windows.Controls.StackPanel
                {
                    Margin = new Thickness(20)
                };

                stackPanel.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = $"Wählen Sie ein Team für '{SelectedArea?.Name}':",
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                var listBox = new System.Windows.Controls.ListBox
                {
                    Height = 150,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                foreach (var team in AvailableTeams)
                {
                    listBox.Items.Add(team);
                }

                if (listBox.Items.Count > 0)
                {
                    listBox.SelectedIndex = 0;
                }

                stackPanel.Children.Add(listBox);

                var buttonPanel = new System.Windows.Controls.StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var okButton = new System.Windows.Controls.Button
                {
                    Content = "Zuordnen",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0),
                    IsDefault = true
                };
                okButton.Click += (s, e) => dialog.DialogResult = true;
                buttonPanel.Children.Add(okButton);

                var cancelButton = new System.Windows.Controls.Button
                {
                    Content = "Abbrechen",
                    Width = 100,
                    Height = 30,
                    IsCancel = true
                };
                cancelButton.Click += (s, e) => dialog.DialogResult = false;
                buttonPanel.Children.Add(cancelButton);

                stackPanel.Children.Add(buttonPanel);
                dialog.Content = stackPanel;

                var result = dialog.ShowDialog();
                
                if (result == true && listBox.SelectedItem != null)
                {
                    return listBox.SelectedItem.ToString();
                }

                return null;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error showing team selection dialog", ex);
                return null;
            }
        }

        private void UpdateAreaColorForTeam(SearchArea area)
        {
            try
            {
                if (string.IsNullOrEmpty(area.AssignedTeam))
                {
                    // Keine Team-Zuordnung - verwende Standardfarbe
                    return;
                }

                // Finde das Team in der Liste
                var teamIndex = AvailableTeams.ToList().IndexOf(area.AssignedTeam);
                
                if (teamIndex >= 0)
                {
                    // Verwende eine Farbe basierend auf dem Team-Index
                    var teamColors = new[]
                    {
                        MediaColor.FromRgb(255, 87, 0),    // Orange (Team 1)
                        MediaColor.FromRgb(76, 175, 80),   // Grün (Team 2)
                        MediaColor.FromRgb(33, 150, 243),  // Blau (Team 3)
                        MediaColor.FromRgb(156, 39, 176),  // Lila (Team 4)
                        MediaColor.FromRgb(255, 193, 7),   // Gelb (Team 5)
                        MediaColor.FromRgb(244, 67, 54),   // Rot (Team 6)
                        MediaColor.FromRgb(0, 188, 212),   // Cyan (Team 7)
                        MediaColor.FromRgb(255, 152, 0)    // Amber (Team 8)
                    };

                    area.Color = teamColors[teamIndex % teamColors.Length];
                    LoggingService.Instance.LogInfo($"Area color updated for team {area.AssignedTeam} (index {teamIndex})");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error updating area color for team", ex);
            }
        }

        private void SaveMapDataToEinsatzData()
        {
            try
            {
                if (_einsatzData == null) return;

                // Synchronisiere SearchAreas mit EinsatzData
                _einsatzData.SearchAreas.Clear();
                
                foreach (var area in SearchAreas)
                {
                    _einsatzData.SearchAreas.Add(area);
                }

                LoggingService.Instance.LogInfo($"Saved {SearchAreas.Count} search areas to EinsatzData");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error saving map data to EinsatzData", ex);
                throw;
            }
        }

        private void DrawSearchAreaOnMap(SearchArea area)
        {
            try
            {
                if (Map == null) return;

                var searchAreaLayer = Map.Layers.FirstOrDefault(l => l.Name == "SearchAreas") as WritableLayer;
                if (searchAreaLayer == null) return;

                // WICHTIG: area.Coordinates sind als (Latitude, Longitude) gespeichert
                // Diese müssen zu Web Mercator konvertiert werden!
                var mercatorCoordinates = area.Coordinates
                    .Select(c =>
                    {
                        var mercator = SphericalMercator.FromLonLat(c.Longitude, c.Latitude);
                        return new Coordinate(mercator.x, mercator.y);
                    })
                    .ToList();
                
                // Schließe das Polygon
                mercatorCoordinates.Add(mercatorCoordinates[0]);

                var geometryFactory = new GeometryFactory();
                var polygon = geometryFactory.CreatePolygon(mercatorCoordinates.ToArray());

                // Erstelle Feature
                var feature = new GeometryFeature { Geometry = polygon };
                
                // Füge Area-ID als Attribut hinzu für späteres Löschen
                feature["AreaId"] = area.Id;

                // Style
                feature.Styles.Add(new VectorStyle
                {
                    Fill = new Brush(new MapsuiColor(area.Color.R, area.Color.G, area.Color.B, 100)),
                    Outline = new Pen(new MapsuiColor(area.Color.R, area.Color.G, area.Color.B, 255), 3),
                });

                searchAreaLayer.Add(feature);
                Map.RefreshData();

                LoggingService.Instance.LogInfo($"Search area drawn on map: {area.Name} - First coord: Lat={area.Coordinates[0].Latitude}, Lon={area.Coordinates[0].Longitude} -> Mercator=({mercatorCoordinates[0].X:F2}, {mercatorCoordinates[0].Y:F2})");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error drawing search area on map", ex);
            }
        }

        private void ExecuteSelectArea(SearchArea? area)
        {
            try
            {
                if (area == null || Map == null) return;

                LoggingService.Instance.LogInfo($"Selecting area: {area.Name}");
                
                // Berechne Bounding Box des Suchgebiets
                if (area.Coordinates.Count < 3)
                {
                    LoggingService.Instance.LogWarning($"Area {area.Name} has too few coordinates");
                    return;
                }

                // WICHTIG: Coordinates sind als (Latitude, Longitude) gespeichert
                // aber SphericalMercator erwartet (Longitude, Latitude)
                var mercatorCoords = area.Coordinates
                    .Select(c => SphericalMercator.FromLonLat(c.Longitude, c.Latitude)) // Korrekte Reihenfolge!
                    .ToList();

                if (!mercatorCoords.Any()) return;

                // Berechne Bounds
                var minX = mercatorCoords.Min(c => c.x);
                var maxX = mercatorCoords.Max(c => c.x);
                var minY = mercatorCoords.Min(c => c.y);
                var maxY = mercatorCoords.Max(c => c.y);

                // Berechne Zentrum
                var centerX = (minX + maxX) / 2;
                var centerY = (minY + maxY) / 2;

                // Debug-Logging
                LoggingService.Instance.LogInfo($"Area bounds - MinX: {minX:F2}, MaxX: {maxX:F2}, MinY: {minY:F2}, MaxY: {maxY:F2}");
                LoggingService.Instance.LogInfo($"Center: ({centerX:F2}, {centerY:F2})");

                // Berechne Bounds-Extent für ZoomToBox
                var extent = new MRect(minX, minY, maxX, maxY);
                
                // Füge Padding hinzu (20% auf jeder Seite für bessere Sicht)
                var paddingFactor = 0.2;
                var paddedExtent = extent.Grow(extent.Width * paddingFactor, extent.Height * paddingFactor);

                // Verwende ZoomToBox statt manuelle Resolution
                Map.Navigator.ZoomToBox(paddedExtent);
                
                SelectedArea = area;
                
                LoggingService.Instance.LogInfo($"Zoomed to area {area.Name} using ZoomToBox - Extent: {paddedExtent}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error selecting area: {ex.Message}", ex);
                
                // Fallback: Einfach zum ersten Punkt zoomen
                try
                {
                    if (area?.Coordinates.Any() == true)
                    {
                        var firstPoint = area.Coordinates[0];
                        var mercator = SphericalMercator.FromLonLat(firstPoint.Longitude, firstPoint.Latitude);
                        Map?.Navigator.CenterOn(mercator.x, mercator.y);
                        Map?.Navigator.ZoomTo(20000); // Sicherer Zoom-Level
                        LoggingService.Instance.LogInfo($"Used fallback centering to: Lat={firstPoint.Latitude}, Lon={firstPoint.Longitude}");
                    }
                }
                catch (Exception fallbackEx)
                {
                    LoggingService.Instance.LogError($"Fallback also failed: {fallbackEx.Message}", fallbackEx);
                }
            }
        }

        public event EventHandler<MapScreenshotEventArgs>? MapScreenshotRequested;
    }

    public class MapScreenshotEventArgs : EventArgs
    {
        public string FilePath { get; }

        public MapScreenshotEventArgs(string filePath)
        {
            FilePath = filePath;
        }
    }
}
