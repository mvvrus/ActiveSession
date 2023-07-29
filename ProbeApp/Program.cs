using MVVrus.AspNetCore.ActiveSession;
using ProbeApp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddActiveSessions<SimpleRunner>();
builder.Services.AddMemoryCache();
var app = builder.Build();

app.UseActiveSessions();
app.MapGet("/", () => "Hello World!");

app.Run();
