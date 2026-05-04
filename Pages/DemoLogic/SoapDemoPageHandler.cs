namespace FunctionalProgramming.Pages.DemoLogic;

public sealed class SoapDemoPageHandler : IApiDemoPageHandler
{
    public string Id => "soap";

    public void Apply(DemoModel model, string method, string example)
    {
        model.Title = "SOAP Demo";
        model.Description = "Post XML SOAP envelope.";
        model.Example = "POST /api/soap\nContent-Type: text/xml";
    }
}
