namespace CoinLore;

using Clients;
using Configurations;
using Interfaces;
using Services;
using System.Reflection;

public static class ServiceExtensions
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CoinLoreConfig>(configuration.GetSection(nameof(CoinLoreConfig)));
        services.Configure<MappingConfig>(configuration.GetSection(nameof(MappingConfig)));
    }

    public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddScoped<ICoinPriceService, CoinPriceService>();
        services.AddScoped<ICoinMappingService, CoinMappingService>();

        services.AddHttpClient<ICoinLoreClient, CoinLoreClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["CoinLoreConfig:BaseUrl"]);
        });
    }

    public static void AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(setup =>
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
                setup.IncludeXmlComments(xmlPath);
        });
    }
}