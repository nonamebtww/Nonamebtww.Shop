# Nonamebtww.Shop.Infrastructure

Слой инфраструктуры прототипа. Сервера нет: всё хранится локально в SQLite.

## Подключение (Mobile)

```csharp
builder.Services.AddInfrastructure(o => {
  o.DatabasePath = Path.Combine(FileSystem.AppDataDirectory, "shop.db3");
});
var app = builder.Build();
await app.Services.InitializeInfrastructureAsync(); // схема + демо-данные
```

Опции (`InfrastructureOptions`): `RecreateDatabaseOnStartup`, `SeedDemoData`, `DeclinePayments`
(fake-платёж отклоняет оплату), `FakeApiLatency`, `AccessTokenLifetime`, `UseRemoteApi`, `ApiBaseUrl`, `ApiTimeout`.

Порты регистрируются через `TryAdd`, поэтому Mobile может заранее подменить `ISessionStore` (SecureStorage)
и `IDeviceInfoProvider` (DeviceInfo).

## Что внутри

| Папка | Содержимое |
|---|---|
| `Persistence` | `ShopDbContext` (EF Core + SQLite), конфигурации таблиц по IDEF1X, репозитории агрегатов, `EfUnitOfWork` (сохранение + публикация доменных событий в `IDomainEventHandler<T>`) |
| `Persistence/Seed` | Справочник ролей и демо-данные: пользователи, категории, товары, отзывы, избранное |
| `Security` | PBKDF2-хэширование пароля, локальные токены (HMAC) |
| `Session` | Сессия в JSON-файле в папке приложения |
| `Notifications` | Заглушка «Сервиса уведомлений»: лог + событие `NotificationSent` |
| `Payments` | Заглушка «Платёжной системы» |
| `Caching` | `IPersistenceCache` в таблице `cache` |
| `Api/Local` | Fake-реализации `I*Api`: выполняют use case Application поверх SQLite |
| `Api/Http` | Заготовки `I*Api` на `HttpClient` для будущего backend |

## Демо-пользователи

| Логин | Пароль | Роль |
|---|---|---|
| buyer | Buyer123! | Покупатель |
| manager | Manager123! | Менеджер по продажам |
| owner | Owner123! | Владелец |
| logist | Logist123! | Логист |
| courier | Courier123! | Курьер |

Вход по логину, email (`<логин>@nonamebtww.shop`) или телефону.

## Переход на backend

`AddInfrastructure(o => { o.UseRemoteApi = true; o.ApiBaseUrl = new Uri("https://api.example.com/"); })`
регистрирует `Http*Api` вместо `Local*Api`. Маршруты REST собраны в `Api/Http/HttpApis.cs`.

Схема БД создаётся `EnsureCreated` без миграций: после изменения модели удалите файл БД
или включите `RecreateDatabaseOnStartup`.
