using GastroLeinefeldeAPI.Services;

namespace GastroLeinefeldeAPI.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IMenuParser, MenuParser>();
        
        services.AddHttpClient<IWebsiteClient, WebsiteClient>(client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
        });

        return services;
    }
}