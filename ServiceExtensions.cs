namespace CoinLore;

using Clients;
using Configurations;
using Interfaces;
using Services;

public static class ServiceExtensions
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CoinLoreConfig>(configuration.GetSection(nameof(CoinLoreConfig)));
    }

    public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICoinPriceService, CoinPriceService>();

        services.AddHttpClient<ICoinLoreClient, CoinLoreClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["CoinLoreConfig:BaseUrl"]);
        });
    }
}