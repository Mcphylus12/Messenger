using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Messenger;

public static class RegistrationExtensions
{
    public static IServiceCollection AddMessenger(this IServiceCollection services, Action<MessageConfiguration> configure, string jsonConfigurationSection = "Messaging")
    {
        services.AddSingleton(sp =>
        {
            var configuration = sp.GetService<IConfiguration>();
            var jsonMessagingConfig = new JsonMessagingConfig();

            if (configuration is not null)
            {
                var messagingSection = configuration.GetSection(jsonConfigurationSection);
                if (messagingSection.Exists())
                {
                    messagingSection.Bind(jsonMessagingConfig);
                }
            }


            var config = new MessageConfiguration(sp);
            configure(config);

            config.Load(jsonMessagingConfig);

            return config;
        });

        services.AddSingleton<Messenger>();
        services.AddTransient<ISender>(sp => sp.GetRequiredService<Messenger>());
        services.AddTransient<IRouter>(sp => sp.GetRequiredService<Messenger>());

        return services;
    }
}

public class JsonMessagingConfig
{
    public Dictionary<string, List<string>> Forwarders { get; set; } = [];
}