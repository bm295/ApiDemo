namespace FunctionalProgramming.Pages.DemoLogic;

public sealed class WebSocketDemoPageHandler : IApiDemoPageHandler
{
    public string Id => "websocket";

    public void Apply(DemoModel model, string method, string example)
    {
        model.Title = "WebSocket Demo";
        model.Description = "Connect to bi-directional endpoint and send messages.";
        model.Example = "Connect ws://localhost:5000/ws and send text frames.";
    }
}
