using Microsoft.Extensions.DependencyInjection;

namespace Messenger;

public static class RegistrationExtensions
{
    public static IServiceCollection AddMessenger(this IServiceCollection services, Action<MessageConfiguration> configure)
    {
        services.AddSingleton(sp =>
        {
            var config = new MessageConfiguration(sp);
            configure(config);

            return config;
        });

        services.AddTransient<Messenger>();
        services.AddTransient<ISender>(sp => sp.GetRequiredService<Messenger>());
        services.AddTransient<IRouter>(sp => sp.GetRequiredService<Messenger>());
        services.AddSingleton<AsyncRequestTracker>();

        return services;
    }
}
