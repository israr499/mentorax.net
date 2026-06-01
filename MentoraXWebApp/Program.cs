using MentoraXWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Read Gateway URL from configuration (appsettings.json or Azure App Settings)
// Read Gateway URL from configuration (appsettings.json or Azure App Settings)
// Falls back to empty string — will surface as a clear error at call time, not startup crash
var gatewayBaseUrl = builder.Configuration["ApiGateway:BaseUrl"] ?? string.Empty;

// Register API Gateway HttpClient
builder.Services.AddHttpClient("Gateway", client =>
{
    // Ensure trailing slash so relative URLs resolve correctly
    var url = gatewayBaseUrl.TrimEnd('/') + "/";
    client.BaseAddress = new Uri(url);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(60); // increase for Azure cold starts
});

// Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();