using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;

namespace Nonamebtww.Shop.Infrastructure.Persistence.Seed;

/// <summary>Исходные демо-данные каталога (без привязки к сущностям, чтобы их было легко править).</summary>
internal static class DemoCatalog {
  internal sealed record CategoryData(string Key, string? ParentKey, string Name, string Description, string? Photo);

  internal sealed record ProductData(
    string CategoryKey,
    string Sku,
    string Name,
    string Description,
    string Characteristics,
    decimal Price,
    int Stock,
    string? Photo);

  internal sealed record UserData(
    UserRole Role,
    string Login,
    string Email,
    string Phone,
    string Password,
    string FirstName,
    string LastName);

  // Демо-пользователи: по одному на роль. Логины/пароли переданы Mobile-треду.
  public static readonly UserData[] Users = [
    new(UserRole.Customer, "buyer", "buyer@nonamebtww.shop", "+79000000001", "Buyer123!", "Иван", "Покупателев"),
    new(UserRole.SalesManager, "manager", "manager@nonamebtww.shop", "+79000000002", "Manager123!", "Мария", "Менеджерова"),
    new(UserRole.Owner, "owner", "owner@nonamebtww.shop", "+79000000003", "Owner123!", "Олег", "Владельцев"),
    new(UserRole.Logistician, "logist", "logist@nonamebtww.shop", "+79000000004", "Logist123!", "Лидия", "Логистова"),
    new(UserRole.Courier, "courier", "courier@nonamebtww.shop", "+79000000005", "Courier123!", "Кирилл", "Курьеров"),
  ];

  public static readonly CategoryData[] Categories = [
    new("electronics", null, "Электроника", "Смартфоны, ноутбуки и аудиотехника", null),
    new("smartphones", "electronics", "Смартфоны", "Смартфоны разных ценовых категорий", null),
    new("laptops", "electronics", "Ноутбуки", "Ноутбуки для работы и учёбы", null),
    new("audio", "electronics", "Аудио", "Наушники и колонки", null),
    new("home", null, "Дом", "Товары для дома", null),
    new("kitchen", "home", "Кухня", "Техника и посуда для кухни", null),
    new("accessories", null, "Аксессуары", "Чехлы, кабели, зарядные устройства", null),
  ];

  public static readonly ProductData[] Products = [
    new("smartphones", "SM-001", "Смартфон Nova 12", "Смартфон с ярким AMOLED-экраном и быстрой зарядкой.", "Экран: 6.5\"; Память: 128 ГБ; Камера: 50 Мп", 29990m, 25, null),
    new("smartphones", "SM-002", "Смартфон Nova 12 Pro", "Флагманская модель с тройной камерой.", "Экран: 6.7\"; Память: 256 ГБ; Камера: 108 Мп", 49990m, 12, null),
    new("smartphones", "SM-003", "Смартфон Lite 5", "Доступный смартфон на каждый день.", "Экран: 6.1\"; Память: 64 ГБ; Камера: 13 Мп", 12990m, 40, null),
    new("laptops", "NB-001", "Ноутбук WorkBook 14", "Лёгкий ноутбук для работы и учёбы.", "Экран: 14\"; ОЗУ: 16 ГБ; SSD: 512 ГБ", 64990m, 8, null),
    new("laptops", "NB-002", "Ноутбук GameBook 16", "Игровой ноутбук с дискретной видеокартой.", "Экран: 16\"; ОЗУ: 32 ГБ; SSD: 1 ТБ", 129990m, 3, null),
    new("audio", "AU-001", "Наушники SoundPods", "Беспроводные наушники с шумоподавлением.", "Bluetooth 5.3; Работа: 30 ч; ANC", 8990m, 60, null),
    new("audio", "AU-002", "Колонка BoomBox Mini", "Компактная колонка с защитой от воды.", "Мощность: 10 Вт; IP67; Работа: 12 ч", 4490m, 35, null),
    new("kitchen", "KT-001", "Электрочайник Steam 1.7", "Чайник из нержавеющей стали.", "Объём: 1.7 л; Мощность: 2200 Вт", 2990m, 50, null),
    new("kitchen", "KT-002", "Блендер Mix Pro", "Стационарный блендер для смузи и супов.", "Мощность: 1000 Вт; Чаша: 1.5 л", 6490m, 0, null),
    new("accessories", "AC-001", "Кабель USB-C 1 м", "Плетёный кабель с поддержкой быстрой зарядки.", "Длина: 1 м; Ток: 3 А", 690m, 200, null),
    new("accessories", "AC-002", "Зарядное устройство 65 Вт", "Компактное GaN-зарядное устройство.", "Мощность: 65 Вт; Порты: 2×USB-C, 1×USB-A", 3490m, 30, null),
    new("accessories", "AC-003", "Чехол для Nova 12", "Силиконовый чехол с микрофиброй внутри.", "Материал: силикон; Цвет: чёрный", 990m, 75, null),
  ];
}
