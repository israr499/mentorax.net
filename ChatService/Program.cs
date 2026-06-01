//using Microsoft.EntityFrameworkCore;
//using ChatService.Data;
//using ChatService.Hubs;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//// Database Connection
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//builder.Services.AddDbContext<ChatDbContext>(options =>
//    options.UseSqlServer(connectionString));

//// Add SignalR
//builder.Services.AddSignalR();

//// ? FIX CORS - Allow your WebApp origin
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowWebApp", policy =>
//    {
//        policy.WithOrigins("https://localhost:7027")  // Your WebApp URL
//              .AllowAnyMethod()
//              .AllowAnyHeader()
//              .AllowCredentials();  // Required for SignalR
//    });
//});

//var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//// ? USE CORS - Must be in this order
//app.UseCors("AllowWebApp");

//app.UseAuthorization();
//app.MapControllers();

//// Map SignalR Hub
//app.MapHub<ChatHub>("/chathub");

//app.Run();


using Microsoft.EntityFrameworkCore;
using ChatService.Data;
using ChatService.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add SignalR
builder.Services.AddSignalR();

// ? FIXED CORS - Allow both local AND Azure WebApp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7027",                                                           // Local
                "https://mentorax-webapp-e5g6aectascma9hj.southeastasia-01.azurewebsites.net"      // Azure
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // Required for SignalR WebSockets
    });
});

var app = builder.Build();

// Swagger (keep for all environments)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// ? CORS must be in this order
app.UseCors("AllowWebApp");

app.UseAuthorization();
app.MapControllers();

// Map SignalR Hub
app.MapHub<ChatHub>("/chathub");

app.Run();