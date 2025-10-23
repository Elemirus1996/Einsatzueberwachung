using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Einsatzueberwachung.Models;

namespace Einsatzueberwachung.Services
{
    public class MasterDataService
    {
        private static MasterDataService? _instance;
        public static MasterDataService Instance => _instance ??= new MasterDataService();

        private readonly string _dataDirectory;
        private readonly string _personalFileName = "personal.json";
        private readonly string _dogsFileName = "dogs.json";

        // Collections für UI-Bindings (öffentlich für ObservableCollection-Binding)
        public ObservableCollection<PersonalEntry> PersonalList { get; } = new ObservableCollection<PersonalEntry>();
        public ObservableCollection<DogEntry> DogList { get; } = new ObservableCollection<DogEntry>();

        /// <summary>
        /// Private Konstruktor für Singleton-Pattern
        /// </summary>
        private MasterDataService()
        {
            // Initialisiere Data Directory
            _dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Einsatzueberwachung", "MasterData");
            Directory.CreateDirectory(_dataDirectory);
            
            LoggingService.Instance.LogInfo("MasterDataService initialized as singleton");
        }

        public async Task LoadDataAsync()
        {
            try
            {
                await LoadPersonalAsync();
                await LoadDogsAsync();
                
                // NEU: Erstelle Testdaten falls keine vorhanden sind
                if (PersonalList.Count == 0)
                {
                    await CreateDefaultTestDataAsync();
                }
                
                LoggingService.Instance.LogInfo($"Master data loaded: {PersonalList.Count} personnel, {DogList.Count} dogs");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Failed to load master data", ex);
                
                // Fallback: Erstelle Testdaten bei Fehlern
                if (PersonalList.Count == 0)
                {
                    await CreateDefaultTestDataAsync();
                }
            }
        }

        /// <summary>
        /// Erstellt Standard-Testdaten falls keine Stammdaten vorhanden sind
        /// </summary>
        private async Task CreateDefaultTestDataAsync()
        {
            try
            {
                LoggingService.Instance.LogInfo("Creating default test data for master data");
                
                // Test-Personal erstellen
                var testPersonal = new PersonalEntry
                {
                    Vorname = "Max",
                    Nachname = "Testperson",
                    Skills = PersonalSkills.Gruppenfuehrer | PersonalSkills.Hundefuehrer,
                    Notizen = "Automatisch erstellte Testperson für Einsatzleiter-Auswahl",
                    IsActive = true
                };
                
                PersonalList.Add(testPersonal);
                
                // Weiteres Test-Personal
                var testPersonal2 = new PersonalEntry
                {
                    Vorname = "Anna",
                    Nachname = "Muster",
                    Skills = PersonalSkills.Zugfuehrer | PersonalSkills.Hundefuehrer,
                    Notizen = "Testperson Zugführer",
                    IsActive = true
                };
                
                PersonalList.Add(testPersonal2);
                
                var testPersonal3 = new PersonalEntry
                {
                    Vorname = "Tom",
                    Nachname = "Beispiel",
                    Skills = PersonalSkills.Verbandsfuehrer,
                    Notizen = "Testperson Verbandsführer",
                    IsActive = true
                };
                
                PersonalList.Add(testPersonal3);
                
                // Test-Hunde erstellen  
                var testDog1 = new DogEntry
                {
                    Name = "Rex",
                    Rasse = "Deutscher Schäferhund",
                    Alter = 5,
                    Specializations = DogSpecialization.Flaechensuche | DogSpecialization.Truemmersuche,
                    Notizen = "Testhund für verschiedene Spezialisierungen",
                    IsActive = true
                };
                
                DogList.Add(testDog1);
                
                var testDog2 = new DogEntry
                {
                    Name = "Bella",
                    Rasse = "Border Collie",
                    Alter = 3,
                    Specializations = DogSpecialization.Mantrailing,
                    Notizen = "Mantrailer Testhund",
                    IsActive = true
                };
                
                DogList.Add(testDog2);
                
                // Testdaten speichern
                await SaveDataAsync();
                
                LoggingService.Instance.LogInfo($"Default test data created: {PersonalList.Count} personnel, {DogList.Count} dogs");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Failed to create default test data", ex);
            }
        }

        private async Task LoadPersonalAsync()
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _personalFileName);
                if (!File.Exists(filePath)) return;

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var personalData = await JsonSerializer.DeserializeAsync<List<PersonalEntry>>(fileStream);

                if (personalData != null)
                {
                    PersonalList.Clear();
                    foreach (var person in personalData)
                    {
                        PersonalList.Add(person);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Failed to load personal data", ex);
            }
        }

        private async Task LoadDogsAsync()
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _dogsFileName);
                if (!File.Exists(filePath)) return;

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var dogData = await JsonSerializer.DeserializeAsync<List<DogEntry>>(fileStream);

                if (dogData != null)
                {
                    DogList.Clear();
                    foreach (var dog in dogData)
                    {
                        DogList.Add(dog);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Failed to load dog data", ex);
            }
        }

        public async Task SaveDataAsync()
        {
            try
            {
                await SavePersonalAsync();
                await SaveDogsAsync();
                LoggingService.Instance.LogInfo("Master data saved successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Failed to save master data", ex);
                throw;
            }
        }

        private async Task SavePersonalAsync()
        {
            var filePath = Path.Combine(_dataDirectory, _personalFileName);
            var tempFilePath = filePath + ".tmp";

            using var memoryStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(memoryStream, PersonalList.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllBytesAsync(tempFilePath, memoryStream.ToArray());

            if (File.Exists(filePath))
                File.Delete(filePath);
            File.Move(tempFilePath, filePath);
        }

        private async Task SaveDogsAsync()
        {
            var filePath = Path.Combine(_dataDirectory, _dogsFileName);
            var tempFilePath = filePath + ".tmp";

            using var memoryStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(memoryStream, DogList.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllBytesAsync(tempFilePath, memoryStream.ToArray());

            if (File.Exists(filePath))
                File.Delete(filePath);
            File.Move(tempFilePath, filePath);
        }

        /// <summary>
        /// Erzwingt das Neuladen der Stammdaten (z.B. nach Änderungen)
        /// </summary>
        public async Task RefreshDataAsync()
        {
            PersonalList.Clear();
            DogList.Clear();
            await LoadDataAsync();
            LoggingService.Instance.LogInfo("Master data refreshed");
        }

        public PersonalEntry? GetPersonalById(string id)
        {
            return PersonalList.FirstOrDefault(p => p.Id == id);
        }

        public DogEntry? GetDogById(string id)
        {
            return DogList.FirstOrDefault(d => d.Id == id);
        }

        public List<PersonalEntry> GetActivePersonal()
        {
            return PersonalList.Where(p => p.IsActive).ToList();
        }

        /// <summary>
        /// Gibt alle Personaldatensätze zurück (auch inaktive)
        /// </summary>
        public List<PersonalEntry> GetAllPersonal()
        {
            return PersonalList.ToList();
        }

        public List<DogEntry> GetActiveDogs()
        {
            return DogList.Where(d => d.IsActive).ToList();
        }

        public List<PersonalEntry> GetPersonalBySkill(PersonalSkills skill)
        {
            return PersonalList.Where(p => p.IsActive && p.Skills.HasFlag(skill)).ToList();
        }

        public List<DogEntry> GetDogsBySpecialization(DogSpecialization spec)
        {
            return DogList.Where(d => d.IsActive && d.Specializations.HasFlag(spec)).ToList();
        }

        public async Task AddPersonalAsync(PersonalEntry person)
        {
            try
            {
                LoggingService.Instance.LogInfo($"MasterDataService.AddPersonalAsync called for: ID={person.Id}, Name={person.FullName}");
                LoggingService.Instance.LogInfo($"Current PersonalList count before add: {PersonalList.Count}");
                
                PersonalList.Add(person);
                LoggingService.Instance.LogInfo($"PersonalEntry added to PersonalList. New count: {PersonalList.Count}");
                
                LoggingService.Instance.LogInfo("Starting SaveDataAsync (await)...");
                await SaveDataAsync();
                LoggingService.Instance.LogInfo("SaveDataAsync completed successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in MasterDataService.AddPersonalAsync for {person.FullName}", ex);
                throw;
            }
        }

        public void AddPersonal(PersonalEntry person)
        {
            try
            {
                LoggingService.Instance.LogInfo($"MasterDataService.AddPersonal called for: ID={person.Id}, Name={person.FullName}");
                LoggingService.Instance.LogInfo($"Current PersonalList count before add: {PersonalList.Count}");
                
                PersonalList.Add(person);
                LoggingService.Instance.LogInfo($"PersonalEntry added to PersonalList. New count: {PersonalList.Count}");
                
                LoggingService.Instance.LogInfo("Starting synchronous SaveDataAsync...");
                // Make sure save completes before returning
                Task.Run(async () => await SaveDataAsync()).Wait(5000); // Wait max 5 seconds
                LoggingService.Instance.LogInfo("SaveDataAsync completed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in MasterDataService.AddPersonal for {person.FullName}", ex);
                throw;
            }
        }

        public void UpdatePersonal(PersonalEntry person)
        {
            try
            {
                LoggingService.Instance.LogInfo($"MasterDataService.UpdatePersonal called for: ID={person.Id}, Name={person.FullName}");
                
                var existing = GetPersonalById(person.Id);
                if (existing != null)
                {
                    var index = PersonalList.IndexOf(existing);
                    LoggingService.Instance.LogInfo($"Found existing PersonalEntry at index {index}, updating...");
                    
                    PersonalList[index] = person;
                    LoggingService.Instance.LogInfo("PersonalEntry updated in PersonalList");
                    
                    LoggingService.Instance.LogInfo("Starting synchronous SaveDataAsync...");
                    Task.Run(async () => await SaveDataAsync()).Wait(5000); // Wait max 5 seconds
                    LoggingService.Instance.LogInfo("SaveDataAsync completed");
                }
                else
                {
                    LoggingService.Instance.LogWarning($"PersonalEntry with ID {person.Id} not found for update");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in MasterDataService.UpdatePersonal for {person.FullName}", ex);
                throw;
            }
        }

        public void RemovePersonal(string id)
        {
            try
            {
                LoggingService.Instance.LogInfo($"MasterDataService.RemovePersonal called for ID: {id}");
                
                var person = GetPersonalById(id);
                if (person != null)
                {
                    LoggingService.Instance.LogInfo($"Found PersonalEntry to remove: {person.FullName}");
                    PersonalList.Remove(person);
                    LoggingService.Instance.LogInfo($"PersonalEntry removed. New count: {PersonalList.Count}");
                    
                    LoggingService.Instance.LogInfo("Starting synchronous SaveDataAsync...");
                    Task.Run(async () => await SaveDataAsync()).Wait(5000); // Wait max 5 seconds
                    LoggingService.Instance.LogInfo("SaveDataAsync completed");
                }
                else
                {
                    LoggingService.Instance.LogWarning($"PersonalEntry with ID {id} not found for removal");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in MasterDataService.RemovePersonal for ID {id}", ex);
                throw;
            }
        }

        public async Task AddDogAsync(DogEntry dog)
        {
            try
            {
                LoggingService.Instance.LogInfo($"MasterDataService.AddDogAsync called for: ID={dog.Id}, Name={dog.Name}");
                LoggingService.Instance.LogInfo($"Current DogList count before add: {DogList.Count}");
                
                DogList.Add(dog);
                LoggingService.Instance.LogInfo($"DogEntry added to DogList. New count: {DogList.Count}");
                
                LoggingService.Instance.LogInfo("Starting SaveDataAsync (await)...");
                await SaveDataAsync();
                LoggingService.Instance.LogInfo("SaveDataAsync completed successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in MasterDataService.AddDogAsync for {dog.Name}", ex);
                throw;
            }
        }

        public void AddDog(DogEntry dog)
        {
            try
            {
                LoggingService.Instance.LogInfo($"MasterDataService.AddDog called for: ID={dog.Id}, Name={dog.Name}");
                LoggingService.Instance.LogInfo($"Current DogList count before add: {DogList.Count}");
                
                DogList.Add(dog);
                LoggingService.Instance.LogInfo($"DogEntry added to DogList. New count: {DogList.Count}");
                
                LoggingService.Instance.LogInfo("Starting synchronous SaveDataAsync...");
                // Make sure save completes before returning
                Task.Run(async () => await SaveDataAsync()).Wait(5000); // Wait max 5 seconds
                LoggingService.Instance.LogInfo("SaveDataAsync completed");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in MasterDataService.AddDog for {dog.Name}", ex);
                throw;
            }
        }

        public void UpdateDog(DogEntry dog)
        {
            try
            {
                LoggingService.Instance.LogInfo($"MasterDataService.UpdateDog called for: ID={dog.Id}, Name={dog.Name}");
                
                var existing = GetDogById(dog.Id);
                if (existing != null)
                {
                    var index = DogList.IndexOf(existing);
                    LoggingService.Instance.LogInfo($"Found existing DogEntry at index {index}, updating...");
                    
                    DogList[index] = dog;
                    LoggingService.Instance.LogInfo("DogEntry updated in DogList");
                    
                    LoggingService.Instance.LogInfo("Starting synchronous SaveDataAsync...");
                    Task.Run(async () => await SaveDataAsync()).Wait(5000); // Wait max 5 seconds
                    LoggingService.Instance.LogInfo("SaveDataAsync completed");
                }
                else
                {
                    LoggingService.Instance.LogWarning($"DogEntry with ID {dog.Id} not found for update");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in MasterDataService.UpdateDog for {dog.Name}", ex);
                throw;
            }
        }

        public void RemoveDog(string id)
        {
            try
            {
                LoggingService.Instance.LogInfo($"MasterDataService.RemoveDog called for ID: {id}");
                
                var dog = GetDogById(id);
                if (dog != null)
                {
                    LoggingService.Instance.LogInfo($"Found DogEntry to remove: {dog.Name}");
                    DogList.Remove(dog);
                    LoggingService.Instance.LogInfo($"DogEntry removed. New count: {DogList.Count}");
                    
                    LoggingService.Instance.LogInfo("Starting synchronous SaveDataAsync...");
                    Task.Run(async () => await SaveDataAsync()).Wait(5000); // Wait max 5 seconds
                    LoggingService.Instance.LogInfo("SaveDataAsync completed");
                }
                else
                {
                    LoggingService.Instance.LogWarning($"DogEntry with ID {id} not found for removal");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in MasterDataService.RemoveDog for ID {id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Returns all dogs with their complete information
        /// </summary>
        public List<DogEntry> GetAllDogs()
        {
            return DogList.ToList();
        }
    }
}
