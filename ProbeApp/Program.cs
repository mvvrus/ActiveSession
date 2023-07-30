using MVVrus.AspNetCore.ActiveSession;
using ProbeApp;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddActiveSessions<SimpleRunner>();
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSession();
app.UseActiveSessions();

app.MapGet("/SimpleRunner", SimpleRunnerHandler.PageHandler);
app.Run();
