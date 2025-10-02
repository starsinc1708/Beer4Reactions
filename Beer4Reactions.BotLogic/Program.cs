using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Beer4Reactions.BotLogic.Configuration;
using Beer4Reactions.BotLogic.Data;
using Beer4Reactions.BotLogic.Services;
using Beer4Reactions.BotLogic.BackgroundServices;
using Beer4Reactions.BotLogic.Handlers;
using Beer4Reactions.BotLogic.Endpoints;
using Beer4Reactions.BotLogic.Middleware;
using Microsoft.OpenApi.Models;
using Serilog;

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Error)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database", Serilog.Events.LogEventLevel.Error)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Fatal)
    .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", Serilog.Events.LogEventLevel.Fatal)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", Serilog.Events.LogEventLevel.Fatal)
    .MinimumLevel.Override("Microsoft.AspNetCore.Routing", Serilog.Events.LogEventLevel.Fatal)
    .MinimumLevel.Override("Microsoft.AspNetCore.HttpLogging", Serilog.Events.LogEventLevel.Fatal)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/beer4reactions-.log", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Используем Serilog
builder.Host.UseSerilog();

// Добавляем конфигурацию
builder.Services.Configure<TelegramBotSettings>(
    builder.Configuration.GetSection("TelegramBot"));
builder.Services.Configure<BotSettings>(
    builder.Configuration.GetSection("BotSettings"));

// Добавляем Telegram Bot Client
builder.Services.AddSingleton<ITelegramBotClient>(_ =>
{
    var token = builder.Configuration.GetSection("TelegramBot:ApiToken").Value 
        ?? throw new InvalidOperationException("Telegram bot token not configured");
    return new TelegramBotClient(token);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Добавляем сервисы
builder.Services.AddScoped<PhotoService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ChatValidationService>();
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<ReactionService>();
builder.Services.AddScoped<TopMessageService>();
builder.Services.AddScoped<TelegramUpdateHandler>();
builder.Services.AddScoped<MonthlyStatisticsService>();

// Добавляем HostedServices
builder.Services.AddHostedService<TelegramBotHostedService>();
builder.Services.AddHostedService<MonthlyStatisticsService>();
builder.Services.AddHostedService<TopMessageUpdateService>();

// Добавляем кеширование
builder.Services.AddMemoryCache();

// Добавляем API контроллеры
builder.Services.AddControllers();

// Добавляем Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Beer4Reactions API", Version = "v1" });
});

var app = builder.Build();

// Добавляем middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var chatValidationService = scope.ServiceProvider.GetRequiredService<ChatValidationService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Applying database migrations...");
        context.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully");
        
        // Обновляем активные TopMessage для всех разрешенных чатов
        if (chatValidationService.IsAnyChatAllowed())
        {
            var botSettings = scope.ServiceProvider.GetRequiredService<IOptions<TelegramBotSettings>>();
            
            // Обновляем каждый чат последовательно для избежания конкурентного доступа к DbContext
            foreach (var chatId in botSettings.Value.AllowedChatIds)
            {
                using var chatScope = app.Services.CreateScope();
                var chatTopMessageService = chatScope.ServiceProvider.GetRequiredService<TopMessageService>();
                try
                {
                    await chatTopMessageService.UpdateTopMessageAsync(chatId);
                    logger.LogInformation("STARTUP | TOP MESSAGE UPDATED | Chat[{ChatId}]", chatId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "STARTUP | TOP MESSAGE UPDATE FAILED | Chat[{ChatId}]", chatId);
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations");
        throw;
    }
}

// Добавляем наш HTTP logging middleware
app.UseMiddleware<HttpLoggingMiddleware>();

app.MapMessagesEndpoints();
app.MapTopMessagesEndpoints();
app.MapStatisticsEndpoints();

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
