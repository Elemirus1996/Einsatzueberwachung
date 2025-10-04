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

        public ObservableCollection<PersonalEntry> PersonalList { get; private set; }
        public ObservableCollection<DogEntry> DogList { get; private set; }

        private MasterDataService()
        {
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Einsatzueberwachung", "MasterData");

            Directory.CreateDirectory(_dataDirectory);

            PersonalList = new ObservableCollection<PersonalEntry>();
            DogList = new ObservableCollection<DogEntry>();
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

        public void AddPersonal(PersonalEntry person)
        {
            PersonalList.Add(person);
            _ = SaveDataAsync(); // Fire and forget
        }

        public void UpdatePersonal(PersonalEntry person)
        {
            var existing = GetPersonalById(person.Id);
            if (existing != null)
            {
                var index = PersonalList.IndexOf(existing);
                PersonalList[index] = person;
                _ = SaveDataAsync();
            }
        }

        public void RemovePersonal(string id)
        {
            var person = GetPersonalById(id);
            if (person != null)
            {
                PersonalList.Remove(person);
                _ = SaveDataAsync();
            }
        }

        public void AddDog(DogEntry dog)
        {
            DogList.Add(dog);
            _ = SaveDataAsync();
        }

        public void UpdateDog(DogEntry dog)
        {
            var existing = GetDogById(dog.Id);
            if (existing != null)
            {
                var index = DogList.IndexOf(existing);
                DogList[index] = dog;
                _ = SaveDataAsync();
            }
        }

        public void RemoveDog(string id)
        {
            var dog = GetDogById(id);
            if (dog != null)
            {
                DogList.Remove(dog);
                _ = SaveDataAsync();
            }
        }
    }
}
