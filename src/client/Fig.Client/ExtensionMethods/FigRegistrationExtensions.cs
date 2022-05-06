using System.Threading.Tasks;
using Fig.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fig.Client.ExtensionMethods
{
    public static class FigRegistrationExtensions
    {
        public static async Task<IServiceCollection> AddFig<TService, TImplementation>(
            this IServiceCollection services,
            ILogger logger,
            FigOptions options)
            where TService : class
            where TImplementation : SettingsBase, TService
        {
            new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("fig").Bind(options);

            if (options.ApiUri == null)
            {
                options.ReadUriFromEnvironmentVariable();
            }

            var provider = new FigConfigurationProvider(logger, options);
            var settings = await provider.Initialize<TImplementation>();

            services.AddSingleton<TService>(a => settings);

            return services;
        }
    }
}