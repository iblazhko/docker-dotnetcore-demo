using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog;

namespace WebApi
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            Func<string, string> settingsResolver = (name) => Configuration[name];

            var loggingLevelSwitch = new LoggingLevelSwitch();
            Log.Logger = Infrastructure.Logging.ApplicationLogging.CreateLogger(settingsResolver, "docker-dotnetcore-webapi", loggingLevelSwitch, "./logs-buffer-webapi");

            MongoDbConfiguration.ServerAddress = settingsResolver("MongoDb.ServerAddress");
            MongoDbConfiguration.ServerPort = int.Parse(settingsResolver("MongoDb.ServerPort"));
            MongoDbConfiguration.DatabaseName = settingsResolver("MongoDb.DatabaseName");
            MongoDbConfiguration.UserName = settingsResolver("MongoDb.UserName");
            MongoDbConfiguration.UserPassword = settingsResolver("MongoDb.UserPassword");

            Log.Information($"WebAPI MongoDb: Server {MongoDbConfiguration.ServerAddress}:{MongoDbConfiguration.ServerPort}/{MongoDbConfiguration.DatabaseName}");
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseMvc();
        }
    }
}
