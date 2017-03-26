using System;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace Infrastructure.Logging
{
    public static class ConfigurationFactory
    {
        private const string SettingNameElasticSearchUrl = "ElasticSearch.Url";
        private const string SettingNameElasticSearchIndexFormat = "ElasticSearch.IndexFormat";
        private const string SettingNameLoggingLevel = "LoggingLevel";

        private const string PropertyNameService = "Service";
        private const string PropertyNameEnvironmentId = "EnvironmentId";

        public static LoggerConfiguration CreateConfiguration(
            Func<string, string> settingsResolver,
            string serviceName,
            LoggingLevelSwitch loggingLevelSwitch,
            string bufferFileFolderLocation)
        {
            if (settingsResolver == null) throw new ArgumentNullException(nameof(settingsResolver));
            if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentNullException(nameof(serviceName));
            if (loggingLevelSwitch == null) throw new ArgumentNullException(nameof(loggingLevelSwitch));

            var loggingLevel = settingsResolver(SettingNameLoggingLevel);
            var esUrl = settingsResolver(SettingNameElasticSearchUrl);
            var esIndexFormat = settingsResolver(SettingNameElasticSearchIndexFormat);

            LogEventLevel logLevel;
            if (Enum.TryParse(loggingLevel, out logLevel))
            {
                loggingLevelSwitch.MinimumLevel = logLevel;
            }

            return new LoggerConfiguration()
                .MinimumLevel.ControlledBy(loggingLevelSwitch)
                .WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(new Uri(esUrl))
                    {
                        AutoRegisterTemplate = true,
                        IndexFormat = esIndexFormat,
                        BufferBaseFilename = bufferFileFolderLocation
                    })
                .Enrich.WithProperty(PropertyNameService, serviceName);
        }
    }
}
