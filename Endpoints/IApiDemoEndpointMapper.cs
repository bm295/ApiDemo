using Microsoft.AspNetCore.Routing;

namespace FunctionalProgramming.Endpoints;

public interface IApiDemoEndpointMapper
{
    void Map(RouteGroupBuilder api);
}
