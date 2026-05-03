using FunctionalProgramming.Endpoints;
using FunctionalProgramming.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IMessageService, MessageService>();
builder.Services.AddSingleton<IWebhookLogger, WebhookLogger>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseWebSockets();
app.MapApiEndpoints();
app.MapWebSocketEndpoints();
app.MapRazorPages();

app.Run();
