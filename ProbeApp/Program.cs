using MVVrus.AspNetCore.ActiveSession;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddActiveSessions<Object, Object>();
var app = builder.Build();

//app.UseActiveSessions();
app.MapGet("/", () => "Hello World!");

app.Run();
