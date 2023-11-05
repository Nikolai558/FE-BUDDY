// This namespace contains the models related to the FeBuddyWPF application.
namespace FeBuddyWPF.Models
{
    // This class represents the application configuration.
    public class AppConfig
    {
        // The folder where configurations are stored.
        public string ConfigurationsFolder { get; set; }

        // The file name for application properties.
        public string AppPropertiesFileName { get; set; }

        // The file name for user-defined settings.
        public string UserDefinedSettingsFileName { get; set; }

        // The privacy statement for the application.
        public string PrivacyStatement { get; set; }
    }
}
