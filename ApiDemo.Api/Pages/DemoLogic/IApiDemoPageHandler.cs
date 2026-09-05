namespace FunctionalProgramming.Pages.DemoLogic;

public interface IApiDemoPageHandler
{
    string Id { get; }
    void Apply(DemoModel model, string method, string example);
}
