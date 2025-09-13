using BankMore.Api.Application.Kafka.Handlers;
using BankMore.Api.Application.Kafka.Messages;
using BankMore.Api.Application.Services;
using BankMore.Api.Infrastructure.Messaging;
using BankMore.Api.SwaggerExamples;
using BankMore.Application.Commands;
using BankMore.Application.Queries;
using BankMore.Infrastructure.Database;
using BankMore.Infrastructure.Repositories;
using BankMore.Infrastructure.Repositories.Impl;
using BankMore.Services;
using KafkaFlow;
using KafkaFlow.Serializer;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BankMore API", Version = "v1" });

    // Configuração para JWT (Http, Bearer)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Digite: Bearer {seu token JWT}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    //exemplos automáticos no Swagger
    c.ExampleFilters();
});

// registra providers de exemplo
builder.Services.AddSwaggerExamplesFromAssemblyOf<TransferCommandExample>();

//Controllers e MediatR
builder.Services.AddControllers();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(RegisterAccountCommand).Assembly,
        typeof(GetBalanceQuery).Assembly
    );
});

//Connection Factory e IDbConnection
builder.Services.AddSingleton<SqlServerConnectionFactory>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    return new SqlServerConnectionFactory(connectionString);
});

builder.Services.AddTransient<IDbConnection>(sp =>
{
    var factory = sp.GetRequiredService<SqlServerConnectionFactory>();
    return factory.CreateConnection();
});

//Serviços
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<IJwtService, JwtService>();

//Repositórios
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IMovementRepository, MovementRepository>();
builder.Services.AddScoped<ITransferRepository, TransferRepository>();
builder.Services.AddScoped<ITariffRepository, TariffRepository>();

//KafkaFlow
builder.Services.AddKafka(kafka => kafka
    .UseConsoleLog()
    .AddCluster(cluster => cluster
        .WithBrokers(new[] { "localhost:9092" })
        .AddProducer("transferProducer", producer => producer
            .DefaultTopic("transferencias-realizadas")
            .AddMiddlewares(m => m.AddSingleTypeSerializer<TransferKafkaMessage, JsonCoreSerializer>()))
        .AddProducer("tarifaProducer", producer => producer
            .DefaultTopic("tarifas-realizadas")
            .AddMiddlewares(m => m.AddSerializer<JsonCoreSerializer>()))
        .AddConsumer(consumer => consumer
            .Topic("transferencias-realizadas")
            .WithGroupId("tarifa-group")
            .WithName("tarifa-consumer")
            .WithBufferSize(100)
            .WithWorkersCount(1)
            .AddMiddlewares(m => m.AddTypedHandlers(h => h
                .AddHandler<TariffMessageHandler>()
                .AddHandler<TarifaHandler>())))
        .AddConsumer(consumer => consumer
            .Topic("tarifas-realizadas")
            .WithGroupId("conta-corrente-group")
            .WithName("conta-corrente-consumer")
            .WithBufferSize(100)
            .WithWorkersCount(1)
            .AddMiddlewares(m => m.AddTypedHandlers(h => h
                .AddHandler<ContaCorrenteTarifaHandler>()))))
);

//TransferKafkaProducer
builder.Services.AddScoped<ITransferKafkaProducer, TransferKafkaProducer>();

//JWT
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "default_secret_key_123456";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false, 
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };

    // Eventos para debug JWT
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "JWT Authentication failed");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("? JWT Token validated successfully");

            if (logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var claim in context.Principal.Claims)
                {
                    logger.LogDebug("Claim - Type: {ClaimType}, Value: {ClaimValue}",
                        claim.Type, claim.Value);
                }
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("?? JWT Challenge - Error: {Error}, Description: {Description}",
                context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

//Authorization
builder.Services.AddAuthorization();

//Build
var app = builder.Build();

//Inicializa Kafka
var kafkaBus = app.Services.CreateKafkaBus();
await kafkaBus.StartAsync();

//Middleware
app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BankMore API v1");
    app.UseSwaggerUI(options => options.DefaultModelsExpandDepth(-1));
    c.RoutePrefix = string.Empty; // abre Swagger na raiz
});


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
