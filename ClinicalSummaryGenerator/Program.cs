using ClinicalSummaryGenerator.Endpoints;
using ClinicalSummaryGenerator.Services;
using ClinicalSummaryGenerator.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddAuthorization();

builder.Services.AddHttpClient<AiService>();
builder.Services.AddSingleton<IClinicalSummaryCache, InMemorySummaryCache>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// app.MapStaticAssets();
// app.MapRazorPages().WithStaticAssets();
app.MapRazorPages();
app.MapSummaryEndpoints();

app.Run();
