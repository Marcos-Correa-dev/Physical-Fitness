using KmValidator.Application.Ports;
using KmValidator.Infrastructure.Services;
using KmValidator.Infrastructure;
using KmValidator.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Registra dependências da camada de infraestrutura, domínio e casos de uso (Hexagonal)
builder.Services.AddInfrastructure(builder.Configuration);

// 1.1 Cliente HTTP tipado e serviço de integração Google Fit / Health Connect
builder.Services.AddHttpClient<IGoogleFitService, GoogleFitService>();

// 2. Controladores e JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// 3. Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Physical Fitness API - Monitoramento e Validação de Atividades",
        Version = "v1",
        Description = "API de ingestão de telemetria de relógios inteligentes (Garmin, Apple Watch, Mi Band 10) e validação de atividades físicas."
    });
});

// 4. Configuração de CORS para permitir requisições do frontend SvelteKit
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSvelteKit", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 5. Inicialização e Semeadura automática do Banco PostgreSQL
await DatabaseSeeder.SeedAsync(app.Services);

// 6. Configuração do Swagger e Swagger UI
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});

// Suporte para Swagger UI na rota /swagger
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Physical Fitness API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Physical Fitness API - Swagger";
});

// Redirecionamentos de conveniência para evitar 404 e erro de versão do Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
app.MapGet("/swagger.json", () => Results.Redirect("/swagger/v1/swagger.json"));
app.MapGet("/v1/swagger.json", () => Results.Redirect("/swagger/v1/swagger.json"));
app.MapGet("/swagger/swagger/v1/swagger.json", () => Results.Redirect("/swagger/v1/swagger.json"));

app.UseCors("AllowSvelteKit");
app.UseAuthorization();
app.MapControllers();

app.Run();
