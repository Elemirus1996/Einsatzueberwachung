using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Einsatzueberwachung.Models;

namespace Einsatzueberwachung.Services
{
    public class TemplateService
    {
        private static TemplateService? _instance;
        public static TemplateService Instance => _instance ??= new TemplateService();

        private readonly string _templatesDirectory;
        private readonly string _templatesFile = "einsatz_templates.json";

        public ObservableCollection<EinsatzTemplate> Templates { get; } = new ObservableCollection<EinsatzTemplate>();

        private TemplateService()
        {
            _templatesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Einsatzueberwachung", "Templates");
            
            Directory.CreateDirectory(_templatesDirectory);
            
            LoadTemplates();
            CreateDefaultTemplates();
        }

        private void CreateDefaultTemplates()
        {
            if (!Templates.Any())
            {
                // Standard Einsatz Template
                var standardTemplate = new EinsatzTemplate
                {
                    Name = "Standard Einsatz",
                    Description = "Standardkonfiguration für normale Einsätze",
                    StandardTeamCount = 3,
                    FirstWarningMinutes = 10,
                    SecondWarningMinutes = 20,
                    IsDefault = true
                };
                Templates.Add(standardTemplate);

                // Übung Template
                var uebungTemplate = new EinsatzTemplate
                {
                    Name = "Übung",
                    Description = "Konfiguration für Trainingsübungen",
                    StandardTeamCount = 2,
                    FirstWarningMinutes = 15,
                    SecondWarningMinutes = 30
                };
                Templates.Add(uebungTemplate);

                // Großeinsatz Template
                var grosseinsatzTemplate = new EinsatzTemplate
                {
                    Name = "Großeinsatz",
                    Description = "Konfiguration für größere Einsätze",
                    StandardTeamCount = 6,
                    FirstWarningMinutes = 8,
                    SecondWarningMinutes = 15
                };
                Templates.Add(grosseinsatzTemplate);

                SaveTemplates();
            }
        }

        public async Task SaveTemplates()
        {
            try
            {
                var filePath = Path.Combine(_templatesDirectory, _templatesFile);
                var json = JsonSerializer.Serialize(Templates, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                await File.WriteAllTextAsync(filePath, json);
                LoggingService.Instance.LogInfo("Templates saved successfully");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Failed to save templates", ex);
            }
        }

        public async Task LoadTemplates()
        {
            try
            {
                var filePath = Path.Combine(_templatesDirectory, _templatesFile);
                if (!File.Exists(filePath)) return;

                var json = await File.ReadAllTextAsync(filePath);
                var templates = JsonSerializer.Deserialize<EinsatzTemplate[]>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (templates != null)
                {
                    Templates.Clear();
                    foreach (var template in templates)
                    {
                        Templates.Add(template);
                    }
                    LoggingService.Instance.LogInfo($"Loaded {templates.Length} templates");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Failed to load templates", ex);
            }
        }

        public EinsatzTemplate? GetDefaultTemplate()
        {
            return Templates.FirstOrDefault(t => t.IsDefault) ?? Templates.FirstOrDefault();
        }

        public async Task AddTemplate(EinsatzTemplate template)
        {
            Templates.Add(template);
            await SaveTemplates();
        }

        public async Task RemoveTemplate(EinsatzTemplate template)
        {
            if (!template.IsDefault) // Prevent deletion of default template
            {
                Templates.Remove(template);
                await SaveTemplates();
            }
        }

        public async Task UpdateTemplate(EinsatzTemplate template)
        {
            await SaveTemplates(); // Templates are reference types, so just save
        }
    }
}