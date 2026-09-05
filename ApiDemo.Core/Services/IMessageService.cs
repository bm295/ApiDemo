using FunctionalProgramming.Models;
using System.Text.Json.Nodes;

namespace FunctionalProgramming.Services;

public interface IMessageService
{
    IReadOnlyList<ApiMessage> GetAll();
    ApiMessage? GetById(int id);
    ApiMessage Add(string text, JsonObject attributes);
    ApiMessage? Update(int id, string? text, JsonObject attributes);
    bool Delete(int id);
}
