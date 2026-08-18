var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

const string FrontendCors = "frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCors, policy => policy
        .WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(FrontendCors);

app.MapGet("/health", () => new HealthStatus("faturamento", "ok"))
    .WithName("GetHealth");

app.Run();

record HealthStatus(string Service, string Status);
