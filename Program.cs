using Microsoft.AspNetCore.Authentication.Cookies;
using TinyBank2.Services;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using TinyBank2.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure in-memory authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "AuthToken";
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        options.LoginPath = "/api/account/login";
        options.LogoutPath = "/api/account/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });

// Add authorization services
builder.Services.AddAuthorization();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer(); // Required for endpoint discovery
builder.Services.AddSwaggerGen(); // Registers the Swagger generator

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/log.txt",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: LogEventLevel.Information,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5119); // HTTP port
    options.ListenLocalhost(7273, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS port
    });
});

// Add application services
builder.Services.AddSingleton<IBankService, BankService>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<ITransactionRepository, TransactionRepository>();

// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();  // Enables the middleware to serve the generated Swagger as a JSON endpoint.
    app.UseSwaggerUI();  // Enables the middleware to serve Swagger UI, specifying the Swagger JSON endpoint.
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();  // Add authentication middleware
app.UseAuthorization();   // Add authorization middleware

app.MapControllers();

await app.RunAsync();