using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using Serilog;
using ZororoMemorialPark.Services;
using QuestPDF.Infrastructure;
using ZororoMemorialPark.Data.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("Logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog(); // Replace default logger

// Add services to the container.
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var connectionString = Environment.GetEnvironmentVariable("DB_STRING_ZDBZ");
    //?? throw new InvalidOperationException("Connection string 'DB_CONNECTION_STRING' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    options.SignIn.RequireConfirmedAccount = false) // can later enable email confirmation
    .AddRoles<IdentityRole>() // roles for Admin, Clerk, etc.
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IAppLogger, AppLogger>();
builder.Services.AddSingleton<InvoicePdfService>();

QuestPDF.Settings.License = LicenseType.Community;
builder.Services.AddRazorPages();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/"); // Protect all pages
});
builder.Services.AddScoped<BurialRegisterPdfService>();

builder.Services.AddTransient<RevenueReportPdfService>();
builder.Services.AddTransient<PaymentLedgerPdfService>();
builder.Services.AddTransient<OutstandingBalancesPdfService>();

builder.Services.AddTransient<StockMovementPdfService>();
builder.Services.AddTransient<SupplierPaymentsPdfService>();

builder.Services.AddScoped<SystemSettingsService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHostedService<BurialReminderService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // enable login
app.UseAuthorization();  // enable role checks

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await RoleMigrationSeeder.MigrateRolesAsync(services);

    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!db.SystemSettings.Any())
    {
        db.SystemSettings.Add(new SystemSettings
        {
            CompanyName = "Zororo Memorial",
            LogoPath = "none",
            PhysicalAddress = "Harare, Zimbabwe",
            Phone = "+263 000 000 000",
            Email = "info@zororo.co.zw",
            MaxBurialsPerDay = 3,
            MinutesBetweenBurials = 60,
            SmtpServer ="none",
            SmtpPort = 587,
            SenderName ="none",
            SenderEmail = "none@mail.com",
            SmtpUsername = "none",
            SmtpPassword = "none",
            UseSSL = true
        });

        db.SaveChanges();
    }
}


app.Run();



