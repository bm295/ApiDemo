using System.Reflection;

namespace FunctionalProgramming.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        var mapperTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IApiDemoEndpointMapper).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .OrderBy(t => t.Name)
            .ToArray();

        foreach (var mapperType in mapperTypes)
        {
            if (Activator.CreateInstance(mapperType) is IApiDemoEndpointMapper mapper)
            {
                mapper.Map(api);
            }
        }

        return app;
    }
}
