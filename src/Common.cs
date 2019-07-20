using Microsoft.Extensions.Configuration;

namespace Foundation.ObjectService
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public static class Common
    {
        public const string READ_AUTHORIZATION_NAME = "read";
        public const string INSERT_AUTHORIZATION_NAME = "insert";
        public const string UPDATE_AUTHORIZATION_NAME = "update";
        public const string DELETE_AUTHORIZATION_NAME = "delete";
        public const string HEALTH_LIVENESS_ENDPOINT = "/health/live";
        public const string HEALTH_READINESS_ENDPOINT = "/health/ready";
        public const string SWAGGER_FILE = "/swagger/v1/swagger.json";

        /// <summary>
        /// Gets a config value for a variable name, preferring ENV variables over appsettings variables when both are present
        /// </summary>
        /// <param name="configuration">The config object to use for pulling keys and values</param>
        /// <param name="environmentVariableName">The name of the variable to use for getting the config value</param>
        /// <param name="appSettingsVariableName">The name of the appsettings variable to use for getting the config value</param>
        /// <param name="defaultValue">The default value to use (if any) if neither config location has a value for this variable</param>
        /// <returns>string representing the config value</returns>
        public static string GetConfigurationVariable(IConfiguration configuration, string environmentVariableName, string appSettingsVariableName, string defaultValue = "")
        {
            string variableValue = string.Empty;
            if (!string.IsNullOrEmpty(appSettingsVariableName) && !string.IsNullOrEmpty(configuration[appSettingsVariableName]))
            {
                variableValue = configuration[appSettingsVariableName];
            }
            if (!string.IsNullOrEmpty(environmentVariableName) && !string.IsNullOrEmpty(configuration[environmentVariableName]))
            {
                variableValue = configuration[environmentVariableName];
            }

            if (string.IsNullOrEmpty(variableValue) && !string.IsNullOrEmpty(defaultValue))
            {
                variableValue = defaultValue;
            }

            return variableValue;
        }
    }
#pragma warning restore 1591 // disables the warnings about missing Xml code comments
}