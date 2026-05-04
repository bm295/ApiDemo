using FunctionalProgramming.Models;
using FunctionalProgramming.Services;
using System.Text.Json;

namespace FunctionalProgramming.Endpoints.ApiTypes;

public sealed class RestApiDemoEndpoints : IApiDemoEndpointMapper
{
    public void Map(RouteGroupBuilder api)
    {
        api.MapGet("/messages", (IMessageService service) => Results.Ok(service.GetAll()));
        api.MapGet("/messages/{id:int}", (int id, IMessageService service) =>
        {
            var message = service.GetById(id);
            return message is null ? Results.NotFound(new { error = "Message not found." }) : Results.Ok(message);
        });
        api.MapPost("/messages", async (HttpContext context, IMessageService service) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ApiMessageInput>(context.Request.Body, cancellationToken: context.RequestAborted);
            if (body is null || string.IsNullOrWhiteSpace(body.Text))
            {
                return Results.BadRequest(new { error = "Text is required." });
            }

            var message = service.Add(body.Text);
            return Results.Created($"/api/messages/{message.Id}", message);
        });
        api.MapPut("/messages/{id:int}", async (int id, HttpContext context, IMessageService service) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ApiMessageInput>(context.Request.Body, cancellationToken: context.RequestAborted);
            if (body is null || string.IsNullOrWhiteSpace(body.Text))
            {
                return Results.BadRequest(new { error = "Text is required." });
            }

            var message = service.Update(id, body.Text);
            return message is null ? Results.NotFound(new { error = "Message not found." }) : Results.Ok(message);
        });
        api.MapDelete("/messages/{id:int}", (int id, IMessageService service) =>
        {
            return service.Delete(id)
                ? Results.NoContent()
                : Results.NotFound(new { error = "Message not found." });
        });
    }
}
