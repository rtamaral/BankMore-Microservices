using BankMore.Api.Application.Kafka.Handlers;
using BankMore.Api.Application.Kafka.Messages;
using BankMore.Api.Application.Services;
using BankMore.Api.Infrastructure.Messaging;
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
using System.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== Swagger =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BankMore API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http, // alterado para Http (Bearer)
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Digite 'Bearer {token}'"
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
            }, Array.Empty<string>()
        }
    });
});

// ===== Controllers e MediatR =====
builder.Services.AddControllers();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(RegisterAccountCommand).Assembly,
        typeof(GetBalanceQuery).Assembly
    );
});

// ===== Connection Factory e IDbConnection =====
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

// ===== Serviços =====
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<IJwtService, JwtService>();

// ===== Repositórios =====
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IMovementRepository, MovementRepository>();
builder.Services.AddScoped<ITransferRepository, TransferRepository>();

// ===== KafkaFlow =====
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

// ===== TransferKafkaProducer =====
builder.Services.AddScoped<ITransferKafkaProducer, TransferKafkaProducer>();

// ===== JWT =====
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
});

// ===== Build =====
var app = builder.Build();

// ===== Inicializa Kafka =====
var kafkaBus = app.Services.CreateKafkaBus();
await kafkaBus.StartAsync();

// ===== Middleware =====
app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BankMore API v1");
    c.RoutePrefix = string.Empty; // abre Swagger na raiz
});

// **UseAuthentication antes de UseAuthorization**
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
