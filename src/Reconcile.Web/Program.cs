using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Reconcile.Web;
using Reconcile.Web.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ReconcileDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Reconcile")));

builder.Services.Configure<DemoOptions>(builder.Configuration.GetSection(DemoOptions.Section));
builder.Services.AddScoped<Reconcile.Web.Services.ReconciliationService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

var demo = app.Configuration.GetSection(DemoOptions.Section).Get<DemoOptions>() ?? new DemoOptions();

if (demo.MigrateOnStartup)
{
    // A container deployment starts the app and SQL Server together; the database
    // can take a minute to accept connections. Retry, then give up loudly.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ReconcileDbContext>();
    var deadline = DateTime.UtcNow.AddSeconds(120);
    while (true)
    {
        try { await db.Database.MigrateAsync(); break; }
        catch (Exception ex) when (DateTime.UtcNow < deadline)
        {
            app.Logger.LogWarning("Database not ready ({Message}); retrying", ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

// Behind a reverse proxy or tunnel the app sees plain HTTP; honour the proxy's scheme.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Runs/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Runs}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
