namespace FunctionalProgramming.Pages.DemoLogic;

public sealed class WebhookDemoPageHandler : IApiDemoPageHandler
{
    public string Id => "webhook";

    public void Apply(DemoModel model, string method, string example)
    {
        model.Title = "Webhook Demo";
        model.Description = "Simulate a third-party callback.";
        model.Example = "POST /api/webhook/orders { \"orderId\":123,\"status\":\"paid\" }";
    }
}
