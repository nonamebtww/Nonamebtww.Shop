using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;

namespace Nonamebtww.Shop.Infrastructure.Persistence.Seed;

/// <summary>Исходные демо-данные каталога (без привязки к сущностям, чтобы их было легко править).</summary>
internal static class DemoCatalog {
  /// <summary>
  /// Версия демо-данных и схемы. При изменении сида или модели увеличьте её:
  /// DatabaseInitializer пересоздаст локальную БД при следующем запуске.
  /// </summary>
  public const int Version = 3;

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

  internal sealed record ReviewData(string Login, string Sku, int Rating, string Text, int DaysAgo);

  // Демо-пользователи: по одному на роль. Логины/пароли переданы Mobile-треду.
  public static readonly UserData[] Users = [
    new(UserRole.Customer, "buyer", "buyer@nonamebtww.shop", "+79000000001", "Buyer123!", "Иван", "Покупателев"),
    new(UserRole.SalesManager, "manager", "manager@nonamebtww.shop", "+79000000002", "Manager123!", "Мария", "Менеджерова"),
    new(UserRole.Owner, "owner", "owner@nonamebtww.shop", "+79000000003", "Owner123!", "Олег", "Владельцев"),
    new(UserRole.Logistician, "logist", "logist@nonamebtww.shop", "+79000000004", "Logist123!", "Лидия", "Логистова"),
    new(UserRole.Courier, "courier", "courier@nonamebtww.shop", "+79000000005", "Courier123!", "Кирилл", "Курьеров"),
  ];

  public static readonly CategoryData[] Categories = [
    new("electronics", null, "Электроника", "Смартфоны, планшеты, ноутбуки и аудиотехника", null),
    new("smartphones", "electronics", "Смартфоны", "Смартфоны Apple, Samsung, OPPO, realme и vivo", null),
    new("tablets", "electronics", "Планшеты", "Планшеты для работы, учёбы и развлечений", null),
    new("laptops", "electronics", "Ноутбуки", "Ноутбуки для работы, учёбы и творчества", null),
    new("audio", "electronics", "Аудио", "Наушники и умные колонки", null),
    new("wearables", "electronics", "Умные часы", "Смарт-часы и фитнес-гаджеты", null),
    new("home", null, "Дом", "Товары для дома", null),
    new("kitchen", "home", "Кухня", "Техника и посуда для кухни", null),
    new("decor", "home", "Свет и декор", "Светильники и предметы интерьера", null),
    new("accessories", null, "Аксессуары", "Чехлы, зарядные устройства и аксессуары для съёмки", null),
  ];

  private const string Cdn = "https://cdn.dummyjson.com/product-images/";

  // Реальные модели; фото — с CDN DummyJSON (проверены: HTTP 200, image/webp).
  public static readonly ProductData[] Products = [
    // Смартфоны
    new("smartphones", "SM-IP13P", "Apple iPhone 13 Pro 128 ГБ",
      "Флагман Apple с дисплеем ProMotion 120 Гц, тройной камерой 12 Мп с телеобъективом и процессором A15 Bionic.",
      "Экран: 6,1\" Super Retina XDR, 120 Гц\nПроцессор: Apple A15 Bionic\nПамять: 128 ГБ\nКамера: 3 × 12 Мп, оптический зум 3×\nАккумулятор: 3095 мА·ч, MagSafe\nЗащита: IP68\nВес: 204 г",
      79990m, 14, Cdn + "smartphones/iphone-13-pro/1.webp"),
    new("smartphones", "SM-IPX", "Apple iPhone X 64 ГБ",
      "Первый iPhone с OLED-экраном во всю переднюю панель и Face ID. Подходит тем, кто хочет iPhone по доступной цене.",
      "Экран: 5,8\" OLED Super Retina\nПроцессор: Apple A11 Bionic\nПамять: 64 ГБ\nКамера: 2 × 12 Мп\nРазблокировка: Face ID\nЗащита: IP67\nВес: 174 г",
      24990m, 6, Cdn + "smartphones/iphone-x/1.webp"),
    new("smartphones", "SM-GS10", "Samsung Galaxy S10 8/128 ГБ",
      "Смартфон с изогнутым Dynamic AMOLED-экраном, ультразвуковым сканером отпечатков в экране и тройной камерой.",
      "Экран: 6,1\" Dynamic AMOLED, 3040×1440\nПроцессор: Exynos 9820\nОЗУ: 8 ГБ\nПамять: 128 ГБ, microSD\nКамера: 12 + 12 + 16 Мп\nАккумулятор: 3400 мА·ч, беспроводная зарядка\nЗащита: IP68",
      29990m, 9, Cdn + "smartphones/samsung-galaxy-s10/1.webp"),
    new("smartphones", "SM-GS8", "Samsung Galaxy S8 4/64 ГБ",
      "Компактный смартфон с безрамочным Infinity-экраном, защитой от воды и быстрой зарядкой.",
      "Экран: 5,8\" Super AMOLED, 2960×1440\nПроцессор: Exynos 8895\nОЗУ: 4 ГБ\nПамять: 64 ГБ, microSD\nКамера: 12 Мп, f/1.7\nАккумулятор: 3000 мА·ч\nЗащита: IP68",
      15990m, 4, Cdn + "smartphones/samsung-galaxy-s8/1.webp"),
    new("smartphones", "SM-OF19", "OPPO F19 Pro+ 5G 8/128 ГБ",
      "Тонкий смартфон с поддержкой 5G, AMOLED-экраном и зарядкой 50 Вт: от 0 до 100% примерно за 48 минут.",
      "Экран: 6,43\" AMOLED, 2400×1080\nПроцессор: MediaTek Dimensity 800U\nОЗУ: 8 ГБ\nПамять: 128 ГБ\nКамера: 48 + 8 + 2 + 2 Мп\nАккумулятор: 4310 мА·ч, 50 Вт\nСеть: 5G",
      23990m, 18, Cdn + "smartphones/oppo-f19-pro-plus/1.webp"),
    new("smartphones", "SM-RMXT", "realme XT 8/128 ГБ",
      "Смартфон с основной камерой 64 Мп и экраном Super AMOLED — отличный выбор для любителей мобильной фотографии.",
      "Экран: 6,4\" Super AMOLED\nПроцессор: Snapdragon 712\nОЗУ: 8 ГБ\nПамять: 128 ГБ\nКамера: 64 + 8 + 2 + 2 Мп\nАккумулятор: 4000 мА·ч, 20 Вт\nСканер отпечатков: в экране",
      17990m, 22, Cdn + "smartphones/realme-xt/1.webp"),
    new("smartphones", "SM-VX21", "vivo X21 6/128 ГБ",
      "Один из первых смартфонов со сканером отпечатков пальцев в экране и двойной камерой с AI-обработкой.",
      "Экран: 6,28\" Super AMOLED\nПроцессор: Snapdragon 660\nОЗУ: 6 ГБ\nПамять: 128 ГБ\nКамера: 12 + 5 Мп\nФронтальная камера: 12 Мп\nАккумулятор: 3200 мА·ч",
      14990m, 0, Cdn + "smartphones/vivo-x21/1.webp"),

    // Планшеты
    new("tablets", "TB-IPMINI", "Apple iPad mini (2021) Wi-Fi 64 ГБ, сияющая звезда",
      "Компактный планшет с экраном Liquid Retina 8,3\", процессором A15 Bionic, USB-C и поддержкой Apple Pencil 2.",
      "Экран: 8,3\" Liquid Retina, 2266×1488\nПроцессор: Apple A15 Bionic\nПамять: 64 ГБ\nКамера: 12 Мп, фронтальная 12 Мп Center Stage\nРазъём: USB-C\nСтилус: Apple Pencil (2-го поколения)\nВес: 293 г",
      49990m, 7, Cdn + "tablets/ipad-mini-2021-starlight/1.webp"),
    new("tablets", "TB-GTS8P", "Samsung Galaxy Tab S8+ 8/128 ГБ, графит",
      "Большой Super AMOLED-экран 12,4\", стилус S Pen в комплекте и режим DeX для работы как на компьютере.",
      "Экран: 12,4\" Super AMOLED, 120 Гц\nПроцессор: Snapdragon 8 Gen 1\nОЗУ: 8 ГБ\nПамять: 128 ГБ, microSD\nАккумулятор: 10090 мА·ч, 45 Вт\nСтилус: S Pen в комплекте\nВес: 567 г",
      69990m, 5, Cdn + "tablets/samsung-galaxy-tab-s8-plus-grey/1.webp"),

    // Ноутбуки
    new("laptops", "NB-MBP14", "Apple MacBook Pro 14\" M1 Pro 16/512 ГБ, серый космос",
      "Профессиональный ноутбук с экраном Liquid Retina XDR, чипом M1 Pro и автономностью до 17 часов.",
      "Экран: 14,2\" Liquid Retina XDR, 120 Гц\nПроцессор: Apple M1 Pro, 8 CPU / 14 GPU\nОЗУ: 16 ГБ\nSSD: 512 ГБ\nПорты: 3 × Thunderbolt 4, HDMI, SDXC, MagSafe 3\nАвтономность: до 17 ч\nВес: 1,6 кг",
      189990m, 3, Cdn + "laptops/apple-macbook-pro-14-inch-space-grey/1.webp"),
    new("laptops", "NB-ZENDUO", "ASUS ZenBook Pro Duo UX581",
      "Ноутбук с двумя экранами: основной 4K OLED и сенсорный ScreenPad Plus над клавиатурой для монтажа и многозадачности.",
      "Экран: 15,6\" 4K OLED, сенсорный\nДоп. экран: ScreenPad Plus 14\"\nПроцессор: Intel Core i7-10750H\nОЗУ: 16 ГБ\nSSD: 1 ТБ\nВидеокарта: NVIDIA GeForce RTX 2060\nВес: 2,5 кг",
      159990m, 2, Cdn + "laptops/asus-zenbook-pro-dual-screen-laptop/1.webp"),
    new("laptops", "NB-MATEX", "HUAWEI MateBook X Pro 16/1024 ГБ",
      "Тонкий металлический ноутбук с сенсорным 3K-экраном почти без рамок и быстрой зарядкой USB-C.",
      "Экран: 13,9\" 3K LTPS, сенсорный\nПроцессор: Intel Core i7\nОЗУ: 16 ГБ\nSSD: 1 ТБ\nЗвук: 4 динамика\nЗарядка: USB-C, 65 Вт\nВес: 1,33 кг",
      129990m, 6, Cdn + "laptops/huawei-matebook-x-pro/1.webp"),
    new("laptops", "NB-YOGA920", "Lenovo Yoga 920 13,9\"",
      "Ноутбук-трансформер с поворотным на 360° экраном, поддержкой стилуса и корпусом из алюминия.",
      "Экран: 13,9\" Full HD IPS, сенсорный\nПроцессор: Intel Core i7-8550U\nОЗУ: 8 ГБ\nSSD: 256 ГБ\nКорпус: трансформер 360°\nПорты: 2 × Thunderbolt 3, USB-A\nВес: 1,37 кг",
      79990m, 4, Cdn + "laptops/lenovo-yoga-920/1.webp"),
    new("laptops", "NB-XPS13", "Dell XPS 13 9300",
      "Компактный ноутбук с экраном InfinityEdge 16:10 — 13,4 дюйма в корпусе размером с 11-дюймовый.",
      "Экран: 13,4\" FHD+ InfinityEdge, 16:10\nПроцессор: Intel Core i7-1065G7\nОЗУ: 16 ГБ LPDDR4x\nSSD: 512 ГБ\nПорты: 2 × Thunderbolt 3\nАвтономность: до 14 ч\nВес: 1,2 кг",
      109990m, 5, Cdn + "laptops/new-dell-xps-13-9300-laptop/1.webp"),

    // Аудио
    new("audio", "AU-APODS", "Apple AirPods (2-го поколения)",
      "Беспроводные наушники с автоматическим подключением к устройствам Apple и голосовым вызовом Siri.",
      "Тип: вкладыши, TWS\nЧип: Apple H1\nРабота: до 5 ч, с кейсом до 24 ч\nКейс: зарядный, Lightning\nМикрофоны: 2, с подавлением шума\nУправление: двойное касание",
      12990m, 30, Cdn + "mobile-accessories/apple-airpods/1.webp"),
    new("audio", "AU-APMAX", "Apple AirPods Max, серебристый",
      "Полноразмерные наушники с активным шумоподавлением, режимом прозрачности и пространственным звуком.",
      "Тип: полноразмерные, закрытые\nЧип: Apple H1 в каждом наушнике\nШумоподавление: активное\nПространственный звук: да, с отслеживанием головы\nРабота: до 20 ч\nВес: 385 г",
      54990m, 8, Cdn + "mobile-accessories/apple-airpods-max-silver/1.webp"),
    new("audio", "AU-BFLEX", "Beats Flex",
      "Беспроводные наушники на шнурке с магнитными амбушюрами: при соединении музыка ставится на паузу.",
      "Тип: внутриканальные, с шейным шнурком\nЧип: Apple W1\nРабота: до 12 ч\nЗарядка: USB-C, Fast Fuel\nМикрофон: да\nАмбушюры: 4 размера в комплекте",
      5990m, 45, Cdn + "mobile-accessories/beats-flex-wireless-earphones/1.webp"),
    new("audio", "AU-HPMINI", "Apple HomePod mini, серый космос",
      "Компактная умная колонка с объёмным звуком на 360°, Siri и управлением умным домом.",
      "Звук: 360°, вычислительная обработка\nАссистент: Siri\nУмный дом: Apple Home, Thread\nПодключение: Wi-Fi, Bluetooth 5.0\nПитание: USB-C, 20 Вт\nВысота: 84 мм",
      11990m, 12, Cdn + "mobile-accessories/apple-homepod-mini-cosmic-grey/1.webp"),
    new("audio", "AU-ECHOP", "Amazon Echo Plus (2-го поколения)",
      "Умная колонка с голосовым ассистентом Alexa, встроенным хабом Zigbee и датчиком температуры.",
      "Ассистент: Amazon Alexa\nДинамики: 3\" вуфер + 0,8\" твитер\nУмный дом: хаб Zigbee\nДатчики: температуры\nПодключение: Wi-Fi, Bluetooth\nВыход: 3,5 мм",
      8990m, 0, Cdn + "mobile-accessories/amazon-echo-plus/1.webp"),

    // Умные часы
    new("wearables", "WR-AW4G", "Apple Watch Series 4 GPS 40 мм, золотой алюминий",
      "Смарт-часы с ЭКГ, распознаванием падений и экраном, который на 30% больше, чем у предыдущей серии.",
      "Корпус: 40 мм, алюминий\nЭкран: LTPO OLED Retina\nДатчики: ЭКГ, пульс, акселерометр\nФункции: распознавание падений\nЗащита: водонепроницаемость 50 м\nРабота: до 18 ч",
      17990m, 10, Cdn + "mobile-accessories/apple-watch-series-4-gold/1.webp"),

    // Кухня
    new("kitchen", "KT-BLEND", "Стационарный блендер 1,5 л",
      "Мощный блендер для смузи, коктейлей, супов-пюре и колки льда; чаша из прочного стекла.",
      "Мощность: 1000 Вт\nЧаша: 1,5 л, стекло\nСкоростей: 5 + импульсный режим\nНожи: 6 лезвий, нержавеющая сталь\nФункции: колка льда\nЗащита: блокировка без крышки",
      6490m, 15, Cdn + "kitchen-accessories/boxed-blender/1.webp"),
    new("kitchen", "KT-HBLEND", "Погружной блендер с венчиком",
      "Компактный погружной блендер с насадкой-венчиком для взбивания и измельчения.",
      "Мощность: 800 Вт\nСкоростей: 2 + турбо\nНасадки: блендерная, венчик\nПогружная часть: нержавеющая сталь\nДлина шнура: 1,2 м\nВес: 0,9 кг",
      3290m, 25, Cdn + "kitchen-accessories/hand-blender/1.webp"),
    new("kitchen", "KT-MICRO", "Микроволновая печь 20 л",
      "Микроволновка с электронным управлением, грилем и автоматическими программами разморозки.",
      "Объём: 20 л\nМощность СВЧ: 700 Вт\nГриль: 1000 Вт\nУправление: электронное\nАвтопрограмм: 8\nПокрытие камеры: эмаль",
      8990m, 9, Cdn + "kitchen-accessories/microwave-oven/1.webp"),
    new("kitchen", "KT-STOVE", "Электроплитка настольная",
      "Настольная плитка с чугунной конфоркой — для дачи, общежития или как дополнительная конфорка.",
      "Конфорок: 1\nМощность: 1500 Вт\nТип конфорки: чугунная\nРегулировка: плавная, 5 режимов\nКорпус: металл\nДиаметр конфорки: 18,5 см",
      2490m, 20, Cdn + "kitchen-accessories/electric-stove/1.webp"),
    new("kitchen", "KT-WOK", "Сковорода-вок из углеродистой стали 30 см",
      "Классический вок для быстрой обжарки на сильном огне; подходит для газовых и индукционных плит.",
      "Диаметр: 30 см\nМатериал: углеродистая сталь\nТолщина стенок: 1,5 мм\nПлиты: газ, электро, индукция\nРучка: дерево\nВес: 1,4 кг",
      2190m, 0, Cdn + "kitchen-accessories/carbon-steel-wok/1.webp"),
    new("kitchen", "KT-POT", "Кастрюля 3 л со стеклянной крышкой",
      "Кастрюля из нержавеющей стали с капсульным дном для равномерного нагрева.",
      "Объём: 3 л\nМатериал: нержавеющая сталь 18/10\nДно: капсульное\nКрышка: жаропрочное стекло\nПлиты: все, включая индукцию\nПосудомойка: можно",
      2790m, 18, Cdn + "kitchen-accessories/silver-pot-with-glass-cap/1.webp"),

    // Свет и декор
    new("decor", "DC-LAMP", "Настольная лампа с тканевым абажуром",
      "Лампа в классическом стиле: керамическое основание и абажур из ткани рассеивают мягкий тёплый свет.",
      "Цоколь: E27\nМакс. мощность лампы: 40 Вт\nВысота: 45 см\nОснование: керамика\nАбажур: ткань\nВыключатель: на проводе",
      3490m, 11, Cdn + "home-decoration/table-lamp/1.webp"),
    new("decor", "DC-POT", "Кашпо керамическое с растением",
      "Керамическое кашпо с декоративным растением — живой акцент для рабочего стола или полки.",
      "Материал: керамика\nДиаметр: 14 см\nВысота: 16 см\nДренаж: отверстие и поддон\nЦвет: белый\nРастение: в комплекте",
      1590m, 14, Cdn + "home-decoration/plant-pot/1.webp"),

    // Аксессуары
    new("accessories", "AC-USBC20", "Адаптер питания Apple USB-C 20 Вт",
      "Оригинальный адаптер для быстрой зарядки iPhone: до 50% заряда примерно за 30 минут.",
      "Мощность: 20 Вт\nРазъём: USB-C\nБыстрая зарядка: USB Power Delivery\nСовместимость: iPhone, iPad, AirPods\nВилка: евро\nКабель: не входит",
      2490m, 60, Cdn + "mobile-accessories/apple-iphone-charger/1.webp"),
    new("accessories", "AC-MSBAT", "Apple MagSafe Battery Pack",
      "Внешний аккумулятор, который магнитом крепится к iPhone и заряжает его на ходу.",
      "Крепление: MagSafe\nЁмкость: 1460 мА·ч\nЗарядка iPhone: до 7,5 Вт\nВход: Lightning, до 20 Вт\nСовместимость: iPhone 12 и новее\nВес: 115 г",
      9990m, 13, Cdn + "mobile-accessories/apple-magsafe-battery-pack/1.webp"),
    new("accessories", "AC-CASE12", "Силиконовый чехол MagSafe для iPhone 12, сливовый",
      "Оригинальный чехол с мягкой подкладкой из микрофибры и встроенными магнитами MagSafe.",
      "Совместимость: iPhone 12 / 12 Pro\nМатериал: силикон, микрофибра\nКрепление: MagSafe\nЦвет: сливовый\nЗащита: бортик над экраном и камерой\nБеспроводная зарядка: да",
      3990m, 35, Cdn + "mobile-accessories/iphone-12-silicone-case-with-magsafe-plum/1.webp"),
    new("accessories", "AC-SELFIE", "Селфи-палка монопод с Bluetooth-пультом",
      "Складной монопод с беспроводным пультом и держателем для смартфонов шириной до 8,5 см.",
      "Длина: 20–100 см\nПульт: Bluetooth 5.0\nДержатель: смартфоны 5,5–8,5 см\nВращение: 360°\nМатериал: алюминий\nВес: 160 г",
      1290m, 40, Cdn + "mobile-accessories/selfie-stick-monopod/1.webp"),
  ];

  /// <summary>Несколько отзывов на товар от разных покупателей (и несколько от одного).</summary>
  public static readonly ReviewData[] Reviews = [
    new("buyer", "SM-IP13P", 5, "Экран 120 Гц — после него на другие смотреть не хочется. Камера отличная.", 12),
    new("buyer", "SM-IP13P", 4, "Через месяц: держит день активного использования, но греется в играх.", 2),
    new("owner", "SM-IP13P", 5, "Брали в офис для сотрудников, все довольны.", 20),
    new("buyer", "AU-APMAX", 4, "Шумоподавление отличное, но кейс странный и наушники тяжеловаты.", 5),
    new("manager", "AU-APMAX", 5, "Лучший звук, который я слышала в беспроводных наушниках.", 9),
    new("buyer", "NB-MBP14", 5, "Тихий, быстрый, экран потрясающий. Батареи хватает на весь день.", 15),
    new("buyer", "KT-BLEND", 4, "Лёд колет без проблем, но довольно шумный.", 7),
    new("logist", "AC-USBC20", 5, "Заряжает быстро, не греется.", 3),
  ];
}
