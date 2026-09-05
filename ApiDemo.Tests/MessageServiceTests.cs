using System.Text.Json.Nodes;
using FunctionalProgramming.Services;

namespace ApiDemo.Tests;

public sealed class MessageServiceTests
{
    [Fact]
    public void AddAfterDelete_UsesANewId()
    {
        var service = new MessageService();
        Assert.True(service.Delete(1));

        var added = service.Add("new message", new JsonObject());

        Assert.Equal(3, added.Id);
        Assert.NotNull(service.GetById(2));
    }

    [Fact]
    public void ReadResults_DoNotExposeStoredMutableState()
    {
        var service = new MessageService();
        var message = service.GetById(1)!;
        message.Attributes["changed"] = true;

        Assert.False(service.GetById(1)!.Attributes.ContainsKey("changed"));
    }

    [Fact]
    public void Add_ClonesCallerAttributes()
    {
        var service = new MessageService();
        var attributes = new JsonObject { ["source"] = "test" };

        var added = service.Add("message", attributes);
        attributes["source"] = "changed";

        Assert.Equal("test", added.Attributes["source"]!.GetValue<string>());
        Assert.Equal("test", service.GetById(added.Id)!.Attributes["source"]!.GetValue<string>());
    }

    [Fact]
    public void Update_MergesAttributesAndPreservesTextWhenBlank()
    {
        var service = new MessageService();
        var updated = service.Update(1, " ", new JsonObject { ["region"] = "apac" });

        Assert.NotNull(updated);
        Assert.Equal("Hello from REST", updated!.Text);
        Assert.Equal("general", updated.Attributes["audience"]!.GetValue<string>());
        Assert.Equal("apac", updated.Attributes["region"]!.GetValue<string>());
    }
}
