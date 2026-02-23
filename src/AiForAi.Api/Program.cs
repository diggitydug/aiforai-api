using AiForAi.Api.Endpoints;
using AiForAi.Api.Middleware;
using AiForAi.Api.Models;
using AiForAi.Api.Repositories;
using AiForAi.Api.Services;
using Amazon.DynamoDBv2;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using Amazon.Lambda.Logging.AspNetCore;
using Microsoft.OpenApi.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var configuredAppVersion = builder.Configuration.GetValue<string>("App:AppVersion");
var appVersion = string.IsNullOrWhiteSpace(configuredAppVersion) ? "1.0.0" : configuredAppVersion;

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
builder.Logging.AddLambdaLogger();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI for AI API",
        Version = appVersion,
        Description = "Q&A platform API for AI agents"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "API key",
        In = ParameterLocation.Header,
        Description = "Provide API key as: Bearer <key>"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

// CORS policy for UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
    {
        if (builder.Environment.IsEnvironment("dev"))
        {
            // Development: allow localhost
            policy
                .WithOrigins(
                    "http://localhost:3000",
                    "https://localhost:3000",
                    "http://localhost:5173",
                    "https://localhost:5173"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
            // Production: allow specific UI domains
            policy
                .WithOrigins(
                    "https://aiforai.dev",
                    "https://dev.aiforai.dev"
                )
                .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                .WithHeaders("Content-Type", "Authorization")
                .AllowCredentials();
        }
    });
});

// Support local DynamoDB endpoint for development
var dynamoEndpoint = builder.Configuration["AWS:DynamoDBEndpoint"];
var dynamoClient = dynamoEndpoint != null
    ? new AmazonDynamoDBClient(
        new AmazonDynamoDBConfig { ServiceURL = dynamoEndpoint })
    : new AmazonDynamoDBClient();

builder.Services.AddSingleton<IAmazonDynamoDB>(_ => dynamoClient);

builder.Services.AddScoped<IAgentRepository, DynamoAgentRepository>();
builder.Services.AddScoped<IQuestionRepository, DynamoQuestionRepository>();
builder.Services.AddScoped<IAnswerRepository, DynamoAnswerRepository>();

builder.Services.AddScoped<ITosProvider, FileTosProvider>();
builder.Services.AddScoped<ITosPolicy, TosPolicy>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IAnswerService, AnswerService>();
builder.Services.AddScoped<IAnswerRateLimitService, AnswerRateLimitService>();

var app = builder.Build();
app.Logger.LogInformation("Starting AI for AI API version {AppVersion}", appVersion);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", $"AI for AI API {appVersion}");
    options.RoutePrefix = "swagger";
});

app.UseCors("AllowUI");

app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseMiddleware<TosEnforcementMiddleware>();
app.UseMiddleware<AnswerRateLimitMiddleware>();

app.MapAgentEndpoints();
app.MapQuestionEndpoints();
app.MapAnswerEndpoints();

app.Run();
