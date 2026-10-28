namespace Nonamebtww.Shop.Infrastructure.Persistence.Seed;

/// <summary>Исходные демо-данные каталога (без привязки к сущностям, чтобы их было легко править).</summary>
internal static class DemoCatalog {
  internal sealed record CategoryData(Guid Id, Guid? ParentId, string Name, string Description, string? Photo);

  internal sealed record ProductData(
    Guid Id,
    Guid CategoryId,
    string Sku,
    string Name,
    string Description,
    string Characteristics,
    decimal Price,
    int Stock,
    string? Photo);

  internal sealed record UserData(
    Guid Id,
    string RoleName,
    string Login,
    string Email,
    string Phone,
    string Password,
    string FirstName,
    string LastName);

  internal sealed record RoleData(Guid Id, string Name, string Description);

  // Роли — по ДВИ / IDEF0.
  public static readonly RoleData Buyer = new(G("10000000-0000-0000-0000-000000000001"), "Покупатель", "Просмотр каталога, корзина, избранное, заказы, отзывы");
  public static readonly RoleData SalesManager = new(G("10000000-0000-0000-0000-000000000002"), "Менеджер по продажам", "Управление каталогом и заказами");
  public static readonly RoleData Owner = new(G("10000000-0000-0000-0000-000000000003"), "Владелец", "Полный доступ, управление правами и блокировкой пользователей");
  public static readonly RoleData Logist = new(G("10000000-0000-0000-0000-000000000004"), "Логист", "Распределение доставки, назначение курьеров");
  public static readonly RoleData Courier = new(G("10000000-0000-0000-0000-000000000005"), "Курьер", "Выполнение доставки заказов");

  public static readonly RoleData[] Roles = [Buyer, SalesManager, Owner, Logist, Courier];

  // Демо-пользователи: по одному на роль. Логины/пароли переданы Mobile-треду.
  public static readonly UserData[] Users = [
    new(G("20000000-0000-0000-0000-000000000001"), Buyer.Name, "buyer", "buyer@nonamebtww.shop", "+79000000001", "Buyer123!", "Иван", "Покупателев"),
    new(G("20000000-0000-0000-0000-000000000002"), SalesManager.Name, "manager", "manager@nonamebtww.shop", "+79000000002", "Manager123!", "Мария", "Менеджерова"),
    new(G("20000000-0000-0000-0000-000000000003"), Owner.Name, "owner", "owner@nonamebtww.shop", "+79000000003", "Owner123!", "Олег", "Владельцев"),
    new(G("20000000-0000-0000-0000-000000000004"), Logist.Name, "logist", "logist@nonamebtww.shop", "+79000000004", "Logist123!", "Лидия", "Логистова"),
    new(G("20000000-0000-0000-0000-000000000005"), Courier.Name, "courier", "courier@nonamebtww.shop", "+79000000005", "Courier123!", "Кирилл", "Курьеров"),
  ];

  private static readonly Guid Electronics = G("30000000-0000-0000-0000-000000000001");
  private static readonly Guid Smartphones = G("30000000-0000-0000-0000-000000000002");
  private static readonly Guid Laptops = G("30000000-0000-0000-0000-000000000003");
  private static readonly Guid Audio = G("30000000-0000-0000-0000-000000000004");
  private static readonly Guid Home = G("30000000-0000-0000-0000-000000000005");
  private static readonly Guid Kitchen = G("30000000-0000-0000-0000-000000000006");
  private static readonly Guid Accessories = G("30000000-0000-0000-0000-000000000007");

  public static readonly CategoryData[] Categories = [
    new(Electronics, null, "Электроника", "Смартфоны, ноутбуки и аудиотехника", "category_electronics.png"),
    new(Smartphones, Electronics, "Смартфоны", "Смартфоны разных ценовых категорий", "category_smartphones.png"),
    new(Laptops, Electronics, "Ноутбуки", "Ноутбуки для работы и учёбы", "category_laptops.png"),
    new(Audio, Electronics, "Аудио", "Наушники и колонки", "category_audio.png"),
    new(Home, null, "Дом", "Товары для дома", "category_home.png"),
    new(Kitchen, Home, "Кухня", "Техника и посуда для кухни", "category_kitchen.png"),
    new(Accessories, null, "Аксессуары", "Чехлы, кабели, зарядные устройства", "category_accessories.png"),
  ];

  public static readonly ProductData[] Products = [
    new(G("40000000-0000-0000-0000-000000000001"), Smartphones, "SM-001", "Смартфон Nova 12", "Смартфон с ярким AMOLED-экраном и быстрой зарядкой.", "Экран: 6.5\"; Память: 128 ГБ; Камера: 50 Мп", 29990m, 25, "product_sm001.png"),
    new(G("40000000-0000-0000-0000-000000000002"), Smartphones, "SM-002", "Смартфон Nova 12 Pro", "Флагманская модель с тройной камерой.", "Экран: 6.7\"; Память: 256 ГБ; Камера: 108 Мп", 49990m, 12, "product_sm002.png"),
    new(G("40000000-0000-0000-0000-000000000003"), Smartphones, "SM-003", "Смартфон Lite 5", "Доступный смартфон на каждый день.", "Экран: 6.1\"; Память: 64 ГБ; Камера: 13 Мп", 12990m, 40, "product_sm003.png"),
    new(G("40000000-0000-0000-0000-000000000004"), Laptops, "NB-001", "Ноутбук WorkBook 14", "Лёгкий ноутбук для работы и учёбы.", "Экран: 14\"; ОЗУ: 16 ГБ; SSD: 512 ГБ", 64990m, 8, "product_nb001.png"),
    new(G("40000000-0000-0000-0000-000000000005"), Laptops, "NB-002", "Ноутбук GameBook 16", "Игровой ноутбук с дискретной видеокартой.", "Экран: 16\"; ОЗУ: 32 ГБ; SSD: 1 ТБ", 129990m, 3, "product_nb002.png"),
    new(G("40000000-0000-0000-0000-000000000006"), Audio, "AU-001", "Наушники SoundPods", "Беспроводные наушники с шумоподавлением.", "Bluetooth 5.3; Работа: 30 ч; ANC", 8990m, 60, "product_au001.png"),
    new(G("40000000-0000-0000-0000-000000000007"), Audio, "AU-002", "Колонка BoomBox Mini", "Компактная колонка с защитой от воды.", "Мощность: 10 Вт; IP67; Работа: 12 ч", 4490m, 35, "product_au002.png"),
    new(G("40000000-0000-0000-0000-000000000008"), Kitchen, "KT-001", "Электрочайник Steam 1.7", "Чайник из нержавеющей стали.", "Объём: 1.7 л; Мощность: 2200 Вт", 2990m, 50, "product_kt001.png"),
    new(G("40000000-0000-0000-0000-000000000009"), Kitchen, "KT-002", "Блендер Mix Pro", "Стационарный блендер для смузи и супов.", "Мощность: 1000 Вт; Чаша: 1.5 л", 6490m, 0, "product_kt002.png"),
    new(G("40000000-0000-0000-0000-000000000010"), Accessories, "AC-001", "Кабель USB-C 1 м", "Плетёный кабель с поддержкой быстрой зарядки.", "Длина: 1 м; Ток: 3 А", 690m, 200, "product_ac001.png"),
    new(G("40000000-0000-0000-0000-000000000011"), Accessories, "AC-002", "Зарядное устройство 65 Вт", "Компактное GaN-зарядное устройство.", "Мощность: 65 Вт; Порты: 2×USB-C, 1×USB-A", 3490m, 30, "product_ac002.png"),
    new(G("40000000-0000-0000-0000-000000000012"), Accessories, "AC-003", "Чехол для Nova 12", "Силиконовый чехол с микрофиброй внутри.", "Материал: силикон; Цвет: чёрный", 990m, 75, "product_ac003.png"),
  ];

  private static Guid G(string value) => Guid.Parse(value);
}
