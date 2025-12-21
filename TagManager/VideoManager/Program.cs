using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Components;
using VideoManager.Components;
using VideoManager.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});


builder.Services.AddSingleton<IDbService, DbService>();
builder.Services.AddSingleton<IPizzaService, PizzaService>();
builder.Services.AddSingleton<IRealmService, RealmService>();
builder.Services.AddSingleton<ITagService, TagService>();
builder.Services.AddSingleton<IPizzaTagService, PizzaTagService>();
builder.Services.AddSingleton<IExportSingleRealmService, ExportSingleService>();
builder.Services.AddSingleton<IExportAllRealmService, ExportAllService>();

builder.Services.AddBlazorBootstrap();

var app = builder.Build();

var baseStorageFolder = builder.Configuration["BaseStorage"];
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(baseStorageFolder),
    RequestPath = "/base"
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
