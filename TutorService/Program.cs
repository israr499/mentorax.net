//using Microsoft.EntityFrameworkCore;
//using TutorService.Data;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//// Database Connection
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//builder.Services.AddDbContext<TutorDbContext>(options =>
//    options.UseSqlServer(connectionString));

//// HTTP Client for calling User Service
//builder.Services.AddHttpClient("UserService", client =>
//{
//    client.BaseAddress = new Uri(builder.Configuration["UserServiceBaseUrl"]);
//});

//// CORS
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll", policy =>
//    {
//        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
//    });
//});

//var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();
//app.UseCors("AllowAll");
//app.UseAuthorization();
//app.MapControllers();

//app.Run();



using Microsoft.EntityFrameworkCore;
using TutorService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TutorDbContext>(options =>
    options.UseSqlServer(connectionString));

// HTTP Client for User Service
builder.Services.AddHttpClient("UserService", client =>
{
    var userServiceUrl = builder.Configuration["ServiceUrls:UserService"];
    client.BaseAddress = new Uri(userServiceUrl ?? "https://localhost:7292");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ✅ FIX CORS - This is the important part
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()        // Allow any domain
              .AllowAnyMethod()        // Allow GET, POST, PUT, DELETE
              .AllowAnyHeader();       // Allow any headers
    });
});

var app = builder.Build();

// ✅ USE CORS - Must be BEFORE other middleware
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
