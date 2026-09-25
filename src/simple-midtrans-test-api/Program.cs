using Carter;
using DotNetEnv;
using Sindika.AspNet.Midtrans.Configuration;
using Sindika.AspNet.Midtrans.Extensions;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddCarter();
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddMidtrans((options) =>
{
    options.ClientKey = Environment.GetEnvironmentVariable("MIDTRANS_CLIENT_KEY");
    options.IsProduction = false;
    options.ServerKey = Environment.GetEnvironmentVariable("MIDTRANS_SERVER_KEY");
    options.Snap = new SnapOptions
    {
        TimeoutInSeconds = 86400,
    };
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapCarter();
app.Run();