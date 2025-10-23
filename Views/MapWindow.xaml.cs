using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Einsatzueberwachung.Models;
using Einsatzueberwachung.Services;
using Einsatzueberwachung.ViewModels;
using System.Collections.Generic;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.UI;
using NetTopologySuite.Geometries;
using MapsuiColor = Mapsui.Styles.Color;

namespace Einsatzueberwachung.Views
{
    /// <summary>
    /// MapWindow - Kartenansicht für Suchgebiete
    /// </summary>
    public partial class MapWindow : BaseThemeWindow
    {
        private readonly MapViewModel _viewModel;

        public MapWindow(EinsatzData einsatzData, List<Team> teams, string initialAddress = "")
        {
            try
            {
                LoggingService.Instance.LogInfo($"Creating MapWindow with {teams?.Count ?? 0} teams...");
                
                InitializeComponent();

                _viewModel = new MapViewModel(einsatzData, teams, initialAddress);
                DataContext = _viewModel;

                // Warte bis das Window geladen ist, bevor die Map gesetzt wird
                Loaded += MapWindow_Loaded;
                
                LoggingService.Instance.LogInfo("MapWindow initialized successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error creating MapWindow", ex);
                MessageBox.Show($"Fehler beim Erstellen des Kartenfensters: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
        
        private void MapWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Setze Map im Code-Behind (kein Binding möglich)
                if (_viewModel.Map != null)
                {
                    MapControl.Map = _viewModel.Map;
                    LoggingService.Instance.LogInfo("Map control initialized with map");
                }
                else
                {
                    LoggingService.Instance.LogWarning("ViewModel Map is null after initialization");
                }

                // Registriere Screenshot-Event
                _viewModel.MapScreenshotRequested += OnMapScreenshotRequested;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error setting map to control", ex);
                MessageBox.Show($"Fehler beim Anzeigen der Karte: {ex.Message}",
                    "Karten-Anzeige", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnMapScreenshotRequested(object? sender, MapScreenshotEventArgs e)
        {
            try
            {
                LoggingService.Instance.LogInfo($"Screenshot requested: {e.FilePath}");
                
                // Erstelle Screenshot des MapControl
                CreateMapScreenshot(e.FilePath);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling screenshot request", ex);
            }
        }

        private void CreateMapScreenshot(string filePath)
        {
            try
            {
                // Warte bis Rendering abgeschlossen ist
                MapControl.Dispatcher.Invoke(() =>
                {
                    MapControl.UpdateLayout();
                }, System.Windows.Threading.DispatcherPriority.Render);

                System.Threading.Thread.Sleep(200);

                // Erstelle RenderTargetBitmap
                var width = (int)MapControl.ActualWidth;
                var height = (int)MapControl.ActualHeight;

                if (width == 0 || height == 0)
                {
                    LoggingService.Instance.LogWarning("MapControl has zero size, using default 1920x1080");
                    width = 1920;
                    height = 1080;
                }

                var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    width,
                    height,
                    96, // DPI X
                    96, // DPI Y
                    System.Windows.Media.PixelFormats.Pbgra32);

                renderBitmap.Render(MapControl);

                // Speichere als PNG
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(renderBitmap));

                using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    encoder.Save(fileStream);
                }

                LoggingService.Instance.LogInfo($"Map screenshot saved: {filePath} ({width}x{height})");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error creating map screenshot", ex);
            }
        }

        private void MapControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;

                var position = e.GetPosition(MapControl);
                
                // Konvertiere Screen-Position zu World-Koordinaten (Mapsui-spezifisch)
                // Viewport hat eine ScreenToWorld Methode als Extension
                var viewport = MapControl.Map.Navigator.Viewport;
                var worldX = viewport.ScreenToWorld(position.X, position.Y).X;
                var worldY = viewport.ScreenToWorld(position.X, position.Y).Y;
                
                // Konvertiere von Web Mercator (x, y) zu Lat/Lon
                var latLon = SphericalMercator.ToLonLat(worldX, worldY);
                double lat = latLon.lat;
                double lon = latLon.lon;
                
                // Prüfe Modus: ELW-Setzen oder Zeichnen
                if (_viewModel.IsSettingElw)
                {
                    // ELW-Position setzen
                    _viewModel.SetElwPositionFromMap(lat, lon);
                    return;
                }
                
                if (_viewModel.IsDrawing)
                {
                    // Füge Zeichnungs-Punkt hinzu
                    AddDrawingPoint(lat, lon);
                }
                
                LoggingService.Instance.LogInfo($"Map clicked at Lat={lat:F6}, Lon={lon:F6}");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error handling map click", ex);
            }
        }

        private void AddDrawingPoint(double latitude, double longitude)
        {
            try
            {
                if (_viewModel == null) return;

                // Konvertiere zu Web Mercator für Anzeige
                var mercator = SphericalMercator.FromLonLat(longitude, latitude);
                double x = mercator.x;
                double y = mercator.y;
                
                _viewModel.CurrentDrawingPoints.Add(new Coordinate(x, y));
                
                // WICHTIG: Benachrichtige ViewModel über Punktänderung für Command-Updates
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Force Command CanExecute update
                    ((RelayCommand)_viewModel.FinishDrawingCommand).RaiseCanExecuteChanged();
                });
                
                // Zeichne temporären Punkt auf der Karte
                DrawTemporaryPoint(x, y);
                
                // Zeige Punktanzahl im Log
                var pointCount = _viewModel.CurrentDrawingPoints.Count;
                var canFinish = pointCount >= 3;
                LoggingService.Instance.LogInfo($"Drawing point {pointCount} added: Lat={latitude:F6}, Lon={longitude:F6}, Mercator=({x:F2}, {y:F2}), CanFinish={canFinish}");
                
                // Visuelles Feedback im UI
                if (pointCount >= 3)
                {
                    // Zeige eine Nachricht, dass jetzt fertiggestellt werden kann
                    // (Optional: könnte auch ein Tooltip oder StatusBar-Text sein)
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error adding drawing point", ex);
            }
        }

        private void DrawTemporaryPoint(double x, double y)
        {
            try
            {
                if (MapControl?.Map == null) return;

                var drawingLayer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "DrawingLayer") as WritableLayer;
                if (drawingLayer == null)
                {
                    LoggingService.Instance.LogWarning("DrawingLayer not found - creating new one");
                    
                    // Erstelle DrawingLayer wenn nicht vorhanden
                    drawingLayer = new WritableLayer
                    {
                        Name = "DrawingLayer",
                        Style = null,
                        IsMapInfoLayer = true
                    };
                    MapControl.Map.Layers.Add(drawingLayer);
                    LoggingService.Instance.LogInfo("DrawingLayer created");
                }

                // Erstelle temporären Marker für den Punkt - verwende NTS Point (nicht System.Windows.Point)
                var pointGeometry = new NetTopologySuite.Geometries.Point(x, y);
                var pointFeature = new GeometryFeature { Geometry = pointGeometry };
                
                // WICHTIG: Verwende RGB-Farben statt String-Namen!
                pointFeature.Styles.Add(new SymbolStyle
                {
                    SymbolScale = 0.8,
                    Fill = new Brush(new MapsuiColor(255, 140, 0)), // Orange in RGB
                    Outline = new Pen(new MapsuiColor(255, 87, 0), 2) // DarkOrange in RGB
                });

                drawingLayer.Add(pointFeature);
                
                // Wenn wir mehr als 1 Punkt haben, zeichne auch Linien
                if (_viewModel.CurrentDrawingPoints.Count > 1)
                {
                    DrawTemporaryLines();
                }
                
                // WICHTIG: Force Refresh der Map
                MapControl.Map.RefreshData();
                MapControl.Refresh(); // WPF Control auch refreshen
                
                var pointCount = _viewModel.CurrentDrawingPoints.Count;
                LoggingService.Instance.LogInfo($"Temporary point {pointCount} drawn at ({x:F2}, {y:F2}) - Layer has {drawingLayer.GetFeatures().Count()} features");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error drawing temporary point", ex);
                MessageBox.Show($"Fehler beim Zeichnen des Punktes:\n{ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DrawTemporaryLines()
        {
            try
            {
                if (MapControl?.Map == null || _viewModel == null) return;

                var drawingLayer = MapControl.Map.Layers.FirstOrDefault(l => l.Name == "DrawingLayer") as WritableLayer;
                if (drawingLayer == null) return;

                // Entferne alte Linien (behalte nur Punkte)
                var linesToRemove = drawingLayer.GetFeatures()
                    .OfType<GeometryFeature>()
                    .Where(f => f.Geometry is LineString)
                    .ToList();
                
                foreach (var line in linesToRemove)
                {
                    drawingLayer.TryRemove(line);
                }

                // Zeichne neue Linien zwischen allen Punkten
                if (_viewModel.CurrentDrawingPoints.Count >= 2)
                {
                    var coords = _viewModel.CurrentDrawingPoints.ToArray();
                    var geometryFactory = new GeometryFactory();
                    var lineString = geometryFactory.CreateLineString(coords);
                    
                    var lineFeature = new GeometryFeature { Geometry = lineString };
                    lineFeature.Styles.Add(new VectorStyle
                    {
                        Line = new Pen(new MapsuiColor(255, 140, 0), 3) // Orange in RGB
                        {
                            PenStyle = PenStyle.Dash
                        }
                    });
                    
                    drawingLayer.Add(lineFeature);
                }

                // Wenn wir 3+ Punkte haben, zeichne auch eine Linie zum Startpunkt (gestrichelt)
                if (_viewModel.CurrentDrawingPoints.Count >= 3)
                {
                    var geometryFactory = new GeometryFactory();
                    var firstPoint = _viewModel.CurrentDrawingPoints[0];
                    var lastPoint = _viewModel.CurrentDrawingPoints[_viewModel.CurrentDrawingPoints.Count - 1];
                    
                    var closingLine = geometryFactory.CreateLineString(new[] { lastPoint, firstPoint });
                    var closingFeature = new GeometryFeature { Geometry = closingLine };
                    closingFeature.Styles.Add(new VectorStyle
                    {
                        Line = new Pen(new MapsuiColor(255, 69, 0), 2) // OrangeRed in RGB
                        {
                            PenStyle = PenStyle.Dot
                        }
                    });
                    
                    drawingLayer.Add(closingFeature);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error drawing temporary lines", ex);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Speichere Suchgebiete (optional - könnte in EinsatzData gespeichert werden)
                LoggingService.Instance.LogInfo($"MapWindow closed with {_viewModel.SearchAreas.Count} search areas");
                Close();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error closing MapWindow", ex);
            }
        }

        protected override void OnWindowClosed(EventArgs e)
        {
            try
            {
                // Deregistriere Events
                if (_viewModel != null)
                {
                    _viewModel.MapScreenshotRequested -= OnMapScreenshotRequested;
                }
                
                LoggingService.Instance.LogInfo("MapWindow closed and cleaned up");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error during MapWindow cleanup", ex);
            }
            finally
            {
                base.OnWindowClosed(e);
            }
        }
    }
}
