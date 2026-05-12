using System.Reflection;
using FunctionalProgramming.Pages.DemoLogic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FunctionalProgramming.Pages;

public class DemoModel : PageModel
{
    public string Title { get; set; } = "API type not found";
    public string Description { get; set; } = "Choose an API type from the home page.";
    public string Example { get; set; } = "";
    public bool IsRestDemo { get; set; }
    public bool IsGraphQlDemo { get; set; }
    public bool IsGrpcDemo { get; set; }
    public string SelectedMethod { get; private set; } = "GET";
    public string SelectedGraphQlExample { get; private set; } = "1";
    public string RestRequestExample { get; set; } = "";
    public string RestResponseExample { get; set; } = "";
    public string RestStatelessNote { get; set; } = "";
    public string RestUniformInterfaceNote { get; set; } = "";
    public string RestUniformInterfaceExample { get; set; } = "";
    public string GraphQlRequestExample { get; set; } = "";
    public string GraphQlResponseExample { get; set; } = "";
    public string GrpcRequestExample { get; set; } = "";
    public string GrpcResponseExample { get; set; } = "";
    public string GrpcHighPerformanceNote { get; set; } = "";

    public void OnGet([FromRoute] string? id, [FromQuery] string? method, [FromQuery] string? example)
    {
        var key = (id ?? string.Empty).ToLowerInvariant();
        var selectedMethod = (method ?? "GET").ToUpperInvariant();
        var selectedGraphQlExample = (example ?? "1").Trim();

        if (selectedMethod is not ("GET" or "POST" or "PUT"))
        {
            selectedMethod = "GET";
        }

        if (selectedGraphQlExample is not ("1" or "2"))
        {
            selectedGraphQlExample = "1";
        }

        SelectedMethod = selectedMethod;
        SelectedGraphQlExample = selectedGraphQlExample;

        var handlers = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IApiDemoPageHandler).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(t => Activator.CreateInstance(t) as IApiDemoPageHandler)
            .Where(h => h is not null)
            .Cast<IApiDemoPageHandler>()
            .ToDictionary(h => h.Id, StringComparer.OrdinalIgnoreCase);

        if (!handlers.TryGetValue(key, out var handler))
        {
            return;
        }

        handler.Apply(this, SelectedMethod, SelectedGraphQlExample);
    }
}
