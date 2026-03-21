using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Market.Web.Persistence.Data;
using Market.Web.Core.Models;
using Market.Web.Repositories;
using Market.Web.Services;
using Market.Web.Services.Payments;
using Market.Web.Persistence;
using Market.Web.Core.Options;
using Polly;
using Market.Web.Services.AI;
using Hangfire;
using Hangfire.PostgreSql;
using Market.Web.Authorization;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<OpenRouterOptions>()
    .BindConfiguration(OpenRouterOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Hangfire setup
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(ops => ops.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>() 
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IAdminService, AdminService>(); 
builder.Services.AddScoped<IAuctionService, AuctionService>();
builder.Services.AddScoped<IProfileService, ProfileService>();  
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPromptProvider, PromptProvider>();

builder.Services.AddHttpClient<IADescriptionService, OpenRouterAiService>()
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(
            retryCount: 2,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(attempt * 2)))
    .AddTransientHttpErrorPolicy(policy =>
        policy.CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30)));

builder.Services.AddScoped<IAuctionProcessingService, AuctionProcessingService>(); 
builder.Services.AddScoped<IPaymentService, StripePaymentService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        if (context.Database.GetPendingMigrations().Any())
        {
            await context.Database.MigrateAsync();
        }

        await DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Wystąpił błąd podczas seedowania lub migracji bazy danych.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAuthorizationFilter()]
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auctions}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();