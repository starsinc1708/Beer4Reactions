# Beer4Reactions - обзор решения

## Назначение и ключевые сценарии
- Telegram-бот собирает фотографии из разрешённых чатов, сохраняет их в базе вместе с метаданными и фиксирует реакции пользователей.
- Реагирование учитывает одиночные фото и альбомы, исключает самореакции автора и позволяет анализировать результаты.
- Формирует оперативную и месячную статистику (топ фото, топ альбомы, топ авторы, популярные эмодзи) и поддерживает закреплённое сообщение со сводкой.
- Раз в месяц отправляет поздравления победителям и сохраняет сводную статистику в таблицу `MonthlyStatistics`.
- Предоставляет REST API для управления сообщениями бота, работы с топ-сообщением и получения статистики.

## Технологический стек
- .NET 9, ASP.NET Core Minimal API (веб-хост и REST API).
- Telegram.Bot 22.0 для работы с Bot API и polling-обновлениями.
- Entity Framework Core 9 + Npgsql для работы с PostgreSQL и миграциями.
- Serilog (Console, File) для структурированного логирования.
- BackgroundService из Microsoft.Extensions.Hosting для фоновых задач.
- Swashbuckle (Swagger) для документации.
- Docker и docker-compose для локального запуска инфраструктуры (бот, PostgreSQL, Adminer).

## Логика обработки
1. **Сообщения с фото**. `TelegramBotHostedService` запускает polling и передаёт апдейты в `TelegramUpdateHandler`. Метод `HandleMessageAsync` проверяет тип `Photo`, создаёт/обновляет автора через `UserService.GetOrCreateUserAsync` и сохраняет данные о медиа через `PhotoService.SavePhotoAsync` (включая поддержку `MediaGroup`).
2. **Реакции**. `ReactionService.HandleReactionAsync` синхронизирует пользователя, сравнивает старые и новые реакции (`ProcessReactionChangesAsync`) и вызывает `AddReactionAsync`/`RemoveReactionAsync`, обеспечивая уникальность реакции для фото или альбома.
3. **Текущее топ-сообщение**. `TopMessageUpdateService` по таймеру вызывает `TopMessageService.UpdateTopMessageAsync`, который через `StatisticsService.GenerateCurrentStatisticsAsync` собирает актуальную статистику и редактирует закреплённое сообщение, если текст изменился.
4. **Месячная статистика и поздравления**. `MonthlyStatisticsService` планирует запуск на первое число 09:00 локального времени. `CheckAndProcessMonthlyStatisticsAsync` пересчитывает метрики (`AggregateStatisticsForChatAsync`) и отправляет поздравления через `SendWinnersCongratulationsAsync`, используя `PhotoService`, `UserService` и `ReactionService`.
5. **HTTP API**. Минимальные эндпоинты в `Endpoints/*` позволяют отправлять/редактировать/прикреплять сообщения (`MessagesEndpoints`), управлять топ-сообщением (`TopMessagesEndpoints`), получать статистику и инициировать тестовую отправку победителей (`StatisticsEndpoints`).

## Структура проекта
- `Program.cs` - точка входа. Конфигурирует Serilog, DI, PostgreSQL (`AddDbContext<AppDbContext>`), TelegramBotClient, кэш, контроллеры, Swagger. Применяет миграции (`Database.Migrate()`), прогоняет обновление топ-сообщений и регистрирует минимальные API и middleware.
- `Configuration/`
  - `TelegramBotSettings.cs` - токен бота и список разрешённых чат-ID.
  - `BotSettings.cs` - интервалы обновления топ-сообщения и статистики, смещение времени.
- `Data/AppDbContext.cs` - DbContext c DbSet для пользователей, фото, альбомов, реакций, месячных сводок и топ-сообщений. Описывает индексы, ограничения и связи (включая check-констрейнт для Reaction).
- `Models/` - сущности EF (User, Photo, MediaGroup, Reaction, MonthlyStatistic, TopMessage).
- `DTOs/`
  - API модели (`SendMessageRequest`, `EditMessageRequest`, `PinMessageRequest`, `ApiResponse<T>`).
  - Вспомогательные результаты (`TopPhotoResult`, `TopPublisherResult`, `TopReactionReceiver`).
  - Новые DTO для тестовой отправки победителей (`TestMonthlyWinnersRequest`, `MonthlyWinnersResult`).
- `Services/`
  - `ChatValidationService` - проверка, разрешён ли чат.
  - `UserService` - создание и обновление пользователей, выбор топового автора и получателя реакций.
  - `PhotoService` - сохранение фото, поиск топовых фото и альбомов (в том числе `GetWinningPhotoAsync`).
  - `ReactionService` - обработка апдейтов реакции и поиск самой популярной реакции.
  - `StatisticsService` - генерация текста статистики и вспомогательные запросы (топ фото, альбомы, пользователи, реакции).
  - `TopMessageService` - создание, обновление и деактивация закреплённого сообщения со статистикой.
- `Handlers/TelegramUpdateHandler.cs` - маршрутизация апдейтов Telegram между сервисами.
- `BackgroundServices/`
  - `TelegramBotHostedService` - запуск polling и проксирование апдейтов.
  - `TopMessageUpdateService` - периодическое обновление топ-сообщений.
  - `MonthlyStatisticsService` - планировщик ежемесячной статистики и поздравлений (добавлен публичный метод `SendWinnersTestAsync`).
- `Endpoints/`
  - `MessagesEndpoints.cs` - `POST /messages/send`, `PUT /messages/edit/{id}`, `POST /messages/pin/{id}`.
  - `TopMessagesEndpoints.cs` - управление топ-сообщением (`POST /topmessages/create/{chatId}`, `PUT /topmessages/update/{chatId}`, `GET /topmessages/active/{chatId}`).
  - `StatisticsEndpoints.cs` - получение статистики (`GET /statistics/current/{chatId}`, `GET /statistics/monthly/{chatId}`) и новый тестовый вызов (`POST /statistics/monthly/test`).
- `Middleware/HttpLoggingMiddleware.cs` - логирование HTTP запросов/ответов с таймингами.
- `Migrations/` - миграции EF Core.
- `appsettings*.json`, `Dockerfile`, `docker-compose*.yml` - конфигурация и окружение запуска.

## Запуск и конфигурация
- При старте `Program.cs` автоматически применяет миграции. Для production рекомендуется задавать секреты через переменные окружения (`TelegramBot__ApiToken`, `TelegramBot__AllowedChatIds__*`).
- Docker-compose поднимает API, PostgreSQL и Adminer; локальный запуск вне контейнера: `dotnet restore`, `dotnet ef database update`, `dotnet run` (из каталога `Beer4Reactions.BotLogic`).
- Тестовый эндпоинт `POST /statistics/monthly/test` принимает `TestMonthlyWinnersRequest` и запускает отправку победителей за предыдущий или указанный период, позволяя выбрать чат-источник данных, чат для уведомления, а также задать точные `StartDateUtc`/`EndDateUtc` для произвольного диапазона.

## Особенности
- `MonthlyStatisticsService` теперь предоставляет публичный метод `SendWinnersTestAsync`, который переиспользует `SendWinnersCongratulationsAsync` и возвращает `MonthlyWinnersResult` с краткой сводкой.
- `SendWinnersCongratulationsAsync` формирует данные о победителях и возвращает результат (включая описание ошибок), что используется в новом тестовом эндпоинте.
- Эндпоинт `POST /statistics/monthly/test` выполняет проверку допускаемых чатов через `ChatValidationService` и возвращает отправленные результаты (фото, автор, получатель реакций и т.д.).
- Все строки логирования и сервисы сохраняют прежнюю структуру, поэтому штатная работа ежемесячного планировщика не нарушена.
