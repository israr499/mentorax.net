using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Load Ocelot from BOTH appsettings.json and ocelot.json
// Environment-specific ocelot.{env}.json will override if present
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddOcelot(builder.Configuration);

// CORS - must allow the WebApp domain
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// CORS must come BEFORE Ocelot
app.UseCors("AllowAll");

// Only show Swagger in Development (optional in prod)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// NOTE: Do NOT call app.UseHttpsRedirection() before Ocelot on Azure
// Azure App Service handles HTTPS termination at the load balancer level.
// Ocelot must be the LAST middleware - it handles all routing.
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

await app.UseOcelot();

app.Run();