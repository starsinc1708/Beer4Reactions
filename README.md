# 🍺 Beer4Reactions Bot

Telegram бот для сбора и анализа реакций на фото с поддержкой альбомов и автоматической статистикой.

## 🚀 Быстрый запуск с Docker

### Windows
```powershell
.\start.ps1
```

### Linux/macOS
```bash
chmod +x start.sh
./start.sh
```

### Или вручную
```bash
# Сборка и запуск
docker-compose up -d --build

# Просмотр логов
docker-compose logs -f beer4reactions
```

## 🔧 Настройка

### 1. Получите Chat ID ваших групп
После запуска:
1. Добавьте бота @reactioncounterhelperbot в нужные группы
2. Сделайте его администратором с правами на чтение сообщений
3. Отправьте любое сообщение в группу
4. Проверьте логи: `docker-compose logs beer4reactions`
5. Найдите строку с Chat ID: `Received update from unauthorized chat: -1001234567890`

### 2. Обновите настройки
Отредактируйте `docker-compose.yml`:
```yaml
environment:
  - TelegramBot__AllowedChatIds__0=-1001234567890  # Ваш Chat ID
  - TelegramBot__AllowedChatIds__1=-1009876543210  # Второй чат (опционально)
```

### 3. Перезапустите бота
```bash
docker-compose restart beer4reactions
```

## 📊 Доступные сервисы

| Сервис | URL | Описание |
|--------|-----|----------|
| 🤖 **API** | http://localhost:8080 | Основное приложение |
| 🗄️ **Adminer** | http://localhost:8081 | Управление базой данных |
| 📋 **Swagger** | http://localhost:8080/swagger | API документация (только в dev режиме) |

## 🎯 Возможности

- ✅ **Автоматическое сохранение фото** (одиночных и альбомов)
- ✅ **Умная обработка реакций** (на альбом = на всю группу)
- ✅ **Живая статистика** (обновляется каждые 5 минут)
- ✅ **Месячные отчеты** (автоматически в начале месяца)
- ✅ **HTTP API** для управления
- ✅ **Безопасность** (только разрешенные чаты)

## 📈 API эндпоинты

### Сообщения
- `POST /messages/send` - отправить сообщение
- `PUT /messages/edit/{messageId}` - редактировать сообщение
- `POST /messages/pin/{messageId}` - закрепить сообщение

### TopMessage (живая статистика)
- `POST /topmessages/create/{chatId}` - создать TopMessage
- `PUT /topmessages/update/{chatId}` - обновить TopMessage
- `GET /topmessages/active/{chatId}` - получить активное TopMessage

### Статистика
- `GET /statistics/current/{chatId}` - текущая статистика
- `GET /statistics/monthly/{chatId}` - месячная статистика
- `POST /statistics/monthly/generate/{chatId}` - создать статистику вручную

## 🛠️ Управление

```bash
# Просмотр логов
docker-compose logs -f beer4reactions

# Перезапуск приложения
docker-compose restart beer4reactions

# Остановка всех сервисов
docker-compose down

# Полная очистка (включая данные)
docker-compose down -v
```

## 🔍 Мониторинг

### Проверка здоровья
```bash
# Статус сервисов
docker-compose ps

# Проверка API
curl http://localhost:8080/topmessages/active/{chatId}

# Проверка базы данных
docker-compose exec postgres pg_isready -U postgres
```

### Логи
```bash
# Логи приложения
docker-compose logs beer4reactions

# Логи базы данных
docker-compose logs postgres

# Все логи
docker-compose logs
```

## 🐛 Устранение неполадок

### Проблема: "Chat not allowed"
**Решение**: Добавьте Chat ID в `AllowedChatIds` в docker-compose.yml

### Проблема: Бот не отвечает
**Решение**: 
1. Проверьте токен бота
2. Убедитесь, что бот добавлен в группу как администратор
3. Проверьте логи: `docker-compose logs beer4reactions`

### Проблема: База данных недоступна
**Решение**:
```bash
# Перезапуск базы данных
docker-compose restart postgres

# Проверка состояния
docker-compose exec postgres pg_isready -U postgres
```

## 📋 Архитектура

- **Backend**: C# .NET 9, ASP.NET Core
- **Database**: PostgreSQL 15
- **ORM**: Entity Framework Core
- **Bot Framework**: Telegram.Bot
- **Containerization**: Docker + Docker Compose
- **API**: Minimal API + Swagger

## 📝 Разработка

```bash
# Локальная разработка
cd Beer4Reactions.BotLogic
dotnet restore
dotnet ef database update
dotnet run

# Development с Docker
docker-compose -f docker-compose.dev.yml up -d
```

## 📄 Лицензия

MIT License

---

**Создано для анализа реакций на фото в Telegram чатах** 🍺📊
