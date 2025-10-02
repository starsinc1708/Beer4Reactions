## Быстрый старт

### 1. Получение токена бота

1. Напишите [@BotFather](https://t.me/BotFather) в Telegram
2. Создайте нового бота: `/newbot`
3. Скопируйте токен в `appsettings.json`
4. Добавьте бота в нужные чаты и сделайте администратором

### 2. Конфигурация

Отредактируйте `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=_;Port=_;Database=_;Username=_;Password=_"
  },
  "TelegramBot": {
    "ApiToken": "YOUR_BOT_TOKEN_FROM_BOTFATHER",
    "AllowedChatIds": [-1001234567890, -1009876543210]
  },
  "BotSettings": {
    "TopMessageUpdateIntervalMinutes": 5,
    "MonthlyStatisticsSchedule": "0 0 1 * *"
  }
}
```

## Swagger UI

В режиме разработки доступен по адресу: `/swagger`