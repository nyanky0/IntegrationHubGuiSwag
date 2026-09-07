using SOLTIUS_Web_API_Add_On.Database.Interfaces;
using SOLTIUS_Web_API_Add_On.Exceptions;
using SOLTIUS_Web_API_Add_On.Models.Configuration;
using System.Xml.Linq;

namespace SOLTIUS_Web_API_Add_On.Services.Configuration
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly string _configPath;
        private readonly IDatabaseInitializerFactory _factory;

        public ConfigurationService(IWebHostEnvironment environment, IDatabaseInitializerFactory factory)
        {
            _factory = factory;
            _configPath = Path.Combine(environment.ContentRootPath, "Configuration", "Config.xml");
            EnsureConfigurationFolder();
        }

        public bool ConfigExists()
        {
            return File.Exists(_configPath);
        }

        public DBConfig GetDatabaseConfig()
        {
            if (!File.Exists(_configPath))
                throw new ApiNotConfiguredException();

            XDocument doc = XDocument.Load(_configPath);
            return ParseDatabaseConfig(doc);
        }

        private DBConfig ParseDatabaseConfig(XDocument doc)
        {
            XElement? database = doc.Root?.Element("ExternalDatabase");
            if (database == null)
                throw new InvalidOperationException("External database configuration is missing.");

            if (!Enum.TryParse<DatabaseType>(database.Element("ExternalDBType")?.Value, true, out DatabaseType databaseType))
                throw new ApiConfigInvalidException(
                    $"Invalid DatabaseType '{database.Element("ExternalDBType")?.Value ?? ""}' in Config.xml.");

            string? portValue = database.Element("ExternalDBPort")?.Value;

            return new DBConfig
            {
                DBType = databaseType,
                Server = database.Element("ExternalDBServer")?.Value ?? "",
                Port = int.TryParse(portValue, out int port) ? port : 0,
                DatabaseName = database.Element("ExternalDBName")?.Value ?? "",
                UserName = database.Element("ExternalDBUser")?.Value ?? "",
                Password = database.Element("ExternalDBPass")?.Value ?? ""
            };
        }

        public async Task ConfigureAsync(string xml)
        {
            // Validate & parse XML payload directly
            XDocument doc = XDocument.Parse(xml);

            DBConfig config = ParseDatabaseConfig(doc);
            IDatabaseInitializer initializer = _factory.Create(config);
            await initializer.InitializeAsync(config);

            // Single write to disk
            await File.WriteAllTextAsync(_configPath, xml);
        }

        public async Task SaveConfigurationFileAsync(string xml)
        {
            XDocument.Parse(xml);

            string? directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(_configPath, xml);
        }

        private void EnsureConfigurationFolder()
        {
            string? directory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
        }
    }
}
