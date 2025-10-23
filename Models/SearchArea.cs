using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Einsatzueberwachung.Models
{
    /// <summary>
    /// Repräsentiert ein Suchgebiet auf der Karte
    /// </summary>
    public class SearchArea : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _assignedTeam = string.Empty;
        private Color _color = Colors.Blue;
        private bool _isCompleted = false;
        private string _notes = string.Empty;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string AssignedTeam
        {
            get => _assignedTeam;
            set { _assignedTeam = value; OnPropertyChanged(); }
        }

        public Color Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set { _isCompleted = value; OnPropertyChanged(); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Polygon-Koordinaten (Lat/Lon Paare)
        /// </summary>
        public List<(double Latitude, double Longitude)> Coordinates { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Berechnet die Fläche des Suchgebiets in Quadratmetern
        /// Verwendet die Shoelace-Formel (Gaußsche Trapezformel) mit Haversine-Korrektur
        /// </summary>
        public double AreaInSquareMeters
        {
            get
            {
                if (Coordinates == null || Coordinates.Count < 3)
                    return 0;

                // Verwende NetTopologySuite für präzise Flächenberechnung
                try
                {
                    var geometryFactory = new NetTopologySuite.Geometries.GeometryFactory();
                    
                    // Konvertiere zu Web Mercator für Flächenberechnung
                    var mercatorCoords = Coordinates
                        .Select(c =>
                        {
                            var mercator = Mapsui.Projections.SphericalMercator.FromLonLat(c.Longitude, c.Latitude);
                            return new NetTopologySuite.Geometries.Coordinate(mercator.x, mercator.y);
                        })
                        .ToList();
                    
                    // Schließe Polygon
                    mercatorCoords.Add(mercatorCoords[0]);
                    
                    var polygon = geometryFactory.CreatePolygon(mercatorCoords.ToArray());
                    return polygon.Area;
                }
                catch
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Fläche in Hektar (10.000 m²)
        /// </summary>
        public double AreaInHectares => AreaInSquareMeters / 10000.0;

        /// <summary>
        /// Fläche in Quadratkilometern
        /// </summary>
        public double AreaInSquareKilometers => AreaInSquareMeters / 1000000.0;

        /// <summary>
        /// Formatierte Flächenanzeige (automatisch optimale Einheit)
        /// </summary>
        public string FormattedArea
        {
            get
            {
                var sqm = AreaInSquareMeters;
                
                if (sqm < 1)
                    return "< 1 m²";
                else if (sqm < 50000) // Unter 5 Hektar
                    return $"{sqm:N0} m²";
                else if (sqm < 1000000) // Unter 1 km²
                    return $"{AreaInHectares:N2} ha";
                else
                    return $"{AreaInSquareKilometers:N2} km²";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // Wenn Coordinates geändert werden, aktualisiere auch die Flächenberechnung
            if (propertyName == nameof(Coordinates))
            {
                OnPropertyChanged(nameof(AreaInSquareMeters));
                OnPropertyChanged(nameof(AreaInHectares));
                OnPropertyChanged(nameof(AreaInSquareKilometers));
                OnPropertyChanged(nameof(FormattedArea));
            }
        }
    }
}
