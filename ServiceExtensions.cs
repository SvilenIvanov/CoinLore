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
        services.Configure<PortfolioConfig>(configuration.GetSection(nameof(PortfolioConfig)));
    }

    public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICoinPriceService, CoinPriceService>();
        services.AddScoped<ICoinMappingService, CoinMappingService>();
        services.AddScoped<IPortfolioService, PortfolioService>();

        services.AddSingleton<IPortfolioRepository, InMemoryPortfolioRepository>();
        services.AddSingleton<ISymbolToIdMappingService, SymbolToIdMappingService>();

        services.AddHttpClient<ICoinLoreClient, CoinLoreClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["CoinLoreConfig:BaseUrl"]);
        });

        services.AddHostedService<PriceUpdateBackgroundService>();
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