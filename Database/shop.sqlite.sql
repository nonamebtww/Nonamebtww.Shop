-- =====================================================================
--  ИС «Интернет-магазин» (вариант 5) — база данных
--  СУБД: SQLite 3 (файл shop.db3 мобильного приложения)
-- ---------------------------------------------------------------------
--  Схема повторяет модель приложения Nonamebtww.Shop (ShopDbContext,
--  EF Core) и ER-диаграмму: 16 таблиц, те же имена таблиц и столбцов.
--  Сущности IDEF1X: Роль, Пользователь, Устройство, Код подтверждения,
--  Категория, Товар, Корзина, Позиция корзины, Избранное, Позиция
--  избранного, Заказ, Позиция заказа, Платёж, Доставка, Отзыв
--  (+ служебная таблица cache).
--
--  Разделы скрипта:
--    1. Создание таблиц (DDL): типы, PK, FK, UNIQUE, CHECK, индексы
--    2. Тестовые данные (DML): не меньше 5 записей в каждой таблице
--    3. Проверочные SELECT-запросы (WHERE, ORDER BY, JOIN, GROUP BY)
--
--  Запуск: sqlite3 shop.db3 < shop.sqlite.sql
--  Скрипт можно запускать повторно: таблицы пересоздаются.
--
--  Коды перечислений (хранятся числом, как в коде C#):
--    Роль (roles.Kind, users.Role): 0 покупатель, 1 менеджер по продажам,
--      2 владелец, 3 логист, 4 курьер
--    Статус заказа: 0 оформление, 1 ожидает оплаты, 2 подтверждён,
--      3 курьер назначен, 4 в доставке, 5 завершён, 6 отменён
--    Тип доставки: 0 курьер, 1 срочно курьером, 2 самовывоз, 3 постамат
--    Способ оплаты: 0 банковская карта, 1 СБП
--    Статус платежа: 0 в обработке, 1 успешно, 2 отклонён, 3 аннулирован,
--      4 возвращён
--    Статус доставки: 0 назначена, 1 принята курьером, 2 вручена,
--      3 не состоялась, 4 отменена
--    Причина отмены заказа: 0 отказ от оформления, 1 отменён покупателем,
--      2 не оплачен за 30 минут
--    Назначение кода: 0 регистрация, 1 восстановление пароля
--
--  Демо-логины и пароли: buyer / Buyer123!, manager / Manager123!,
--    owner / Owner123!, logist / Logist123!, courier / Courier123!,
--    courier2 / Courier123!, anna / Anna123! (вход по телефону),
--    petr / Petr123! (заблокирован)
-- =====================================================================

-- Даты хранятся так же, как их записывает EF Core для SQLite
-- (DateTimeOffsetToBinaryConverter): ((ticks / 1000) << 11) | смещение в минутах.
-- В INSERT ниже даты указаны строкой (UTC) и переводятся в это число
-- выражением ((strftime('%s', дата) + 62135596800) * 10000) << 11.
-- Обратно в дату: datetime((столбец >> 11) / 10000 - 62135596800, 'unixepoch').
-- Guid хранится строкой в верхнем регистре, bool — 0/1, деньги — REAL.

-- =====================================================================
--  1. СОЗДАНИЕ ТАБЛИЦ
-- =====================================================================

-- Удаление старых таблиц (проверка FK на время удаления выключена:
-- PRAGMA foreign_keys не действует внутри транзакции).
PRAGMA foreign_keys = OFF;

DROP TABLE IF EXISTS cache;
DROP TABLE IF EXISTS reviews;
DROP TABLE IF EXISTS deliveries;
DROP TABLE IF EXISTS payments;
DROP TABLE IF EXISTS order_items;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS favorite_items;
DROP TABLE IF EXISTS favorites;
DROP TABLE IF EXISTS cart_items;
DROP TABLE IF EXISTS carts;
DROP TABLE IF EXISTS products;
DROP TABLE IF EXISTS categories;
DROP TABLE IF EXISTS confirmation_codes;
DROP TABLE IF EXISTS devices;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS roles;

PRAGMA foreign_keys = ON;

BEGIN TRANSACTION;

-- Роль (справочник). Kind — альтернативный ключ, на него ссылается users.Role.
CREATE TABLE roles (
    Id          TEXT    NOT NULL CONSTRAINT PK_roles PRIMARY KEY,
    Kind        INTEGER NOT NULL CONSTRAINT CK_roles_Kind CHECK (Kind BETWEEN 0 AND 4),
    Name        TEXT    NOT NULL,
    Description TEXT    NULL,
    CONSTRAINT AK_roles_Kind UNIQUE (Kind)
);

-- Пользователь. Регистрация по email ИЛИ телефону; удалённый аккаунт обезличен (DeletedAt).
CREATE TABLE users (
    Id           TEXT    NOT NULL CONSTRAINT PK_users PRIMARY KEY,
    Role         INTEGER NOT NULL,
    Login        TEXT    NOT NULL CONSTRAINT CK_users_Login CHECK (length(Login) BETWEEN 3 AND 32),
    Email        TEXT    NULL     CONSTRAINT CK_users_Email CHECK (Email LIKE '_%@_%._%'),
    Phone        TEXT    NULL     CONSTRAINT CK_users_Phone CHECK (Phone GLOB '+[0-9]*' AND length(Phone) BETWEEN 11 AND 16),
    PasswordHash TEXT    NOT NULL,
    FirstName    TEXT    NOT NULL,
    LastName     TEXT    NULL,
    RegisteredAt INTEGER NOT NULL,
    IsConfirmed  INTEGER NOT NULL DEFAULT 0 CONSTRAINT CK_users_IsConfirmed CHECK (IsConfirmed IN (0, 1)),
    IsBlocked    INTEGER NOT NULL DEFAULT 0 CONSTRAINT CK_users_IsBlocked CHECK (IsBlocked IN (0, 1)),
    BlockReason  TEXT    NULL,
    BlockedAt    INTEGER NULL,
    DeletedAt    INTEGER NULL,
    CONSTRAINT FK_users_roles_Role FOREIGN KEY (Role) REFERENCES roles (Kind) ON DELETE RESTRICT,
    CONSTRAINT CK_users_Contact CHECK (Email IS NOT NULL OR Phone IS NOT NULL OR DeletedAt IS NOT NULL),
    CONSTRAINT CK_users_Block CHECK (IsBlocked = 0 OR BlockReason IS NOT NULL)
);
CREATE UNIQUE INDEX IX_users_Login ON users (Login);
CREATE UNIQUE INDEX IX_users_Email ON users (Email);
CREATE UNIQUE INDEX IX_users_Phone ON users (Phone);
CREATE INDEX IX_users_Role ON users (Role);

-- Устройство, с которого входил пользователь.
CREATE TABLE devices (
    Id           TEXT    NOT NULL CONSTRAINT PK_devices PRIMARY KEY,
    UserId       TEXT    NOT NULL,
    Name         TEXT    NOT NULL,
    IpAddress    TEXT    NULL,
    RefreshToken TEXT    NULL,
    LastSignInAt INTEGER NOT NULL,
    IsActive     INTEGER NOT NULL CONSTRAINT CK_devices_IsActive CHECK (IsActive IN (0, 1)),
    CONSTRAINT FK_devices_users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE
);
CREATE INDEX IX_devices_UserId ON devices (UserId);

-- Код подтверждения (регистрация, восстановление пароля), живёт 15 минут.
CREATE TABLE confirmation_codes (
    Id        TEXT    NOT NULL CONSTRAINT PK_confirmation_codes PRIMARY KEY,
    UserId    TEXT    NOT NULL,
    Code      TEXT    NOT NULL,
    Purpose   INTEGER NOT NULL CONSTRAINT CK_confirmation_codes_Purpose CHECK (Purpose IN (0, 1)),
    CreatedAt INTEGER NOT NULL,
    ExpiresAt INTEGER NOT NULL,
    IsUsed    INTEGER NOT NULL CONSTRAINT CK_confirmation_codes_IsUsed CHECK (IsUsed IN (0, 1)),
    CONSTRAINT FK_confirmation_codes_users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE,
    CONSTRAINT CK_confirmation_codes_Expires CHECK (ExpiresAt > CreatedAt)
);
CREATE INDEX IX_confirmation_codes_UserId ON confirmation_codes (UserId);

-- Категория каталога. ParentId — родительская категория (дерево).
CREATE TABLE categories (
    Id          TEXT NOT NULL CONSTRAINT PK_categories PRIMARY KEY,
    ParentId    TEXT NULL,
    Name        TEXT NOT NULL,
    Description TEXT NULL,
    Photo       TEXT NULL,
    CONSTRAINT FK_categories_categories_ParentId FOREIGN KEY (ParentId) REFERENCES categories (Id) ON DELETE RESTRICT
);
CREATE INDEX IX_categories_ParentId ON categories (ParentId);

-- Товар. Цена — денежная сумма из двух столбцов (сумма и валюта).
CREATE TABLE products (
    Id              TEXT    NOT NULL CONSTRAINT PK_products PRIMARY KEY,
    CategoryId      TEXT    NOT NULL,
    Sku             TEXT    NOT NULL,
    Name            TEXT    NOT NULL,
    Description     TEXT    NULL,
    Characteristics TEXT    NULL,
    price_amount    REAL    NOT NULL CONSTRAINT CK_products_Price CHECK (price_amount >= 0),
    price_currency  TEXT    NOT NULL CONSTRAINT CK_products_Currency CHECK (length(price_currency) = 3),
    Stock           INTEGER NOT NULL CONSTRAINT CK_products_Stock CHECK (Stock >= 0),
    Photo           TEXT    NULL,
    IsActive        INTEGER NOT NULL CONSTRAINT CK_products_IsActive CHECK (IsActive IN (0, 1)),
    CONSTRAINT FK_products_categories_CategoryId FOREIGN KEY (CategoryId) REFERENCES categories (Id) ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IX_products_Sku ON products (Sku);
CREATE INDEX IX_products_Name ON products (Name);
CREATE INDEX IX_products_CategoryId ON products (CategoryId);

-- Корзина: одна на пользователя.
CREATE TABLE carts (
    Id     TEXT NOT NULL CONSTRAINT PK_carts PRIMARY KEY,
    UserId TEXT NOT NULL,
    CONSTRAINT FK_carts_users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_carts_UserId ON carts (UserId);

-- Позиция корзины: товар встречается в корзине один раз, количество 1..99.
CREATE TABLE cart_items (
    Id        TEXT    NOT NULL CONSTRAINT PK_cart_items PRIMARY KEY,
    CartId    TEXT    NOT NULL,
    ProductId TEXT    NOT NULL,
    Quantity  INTEGER NOT NULL CONSTRAINT CK_cart_items_Quantity CHECK (Quantity BETWEEN 1 AND 99),
    AddedAt   INTEGER NOT NULL,
    CONSTRAINT FK_cart_items_carts_CartId FOREIGN KEY (CartId) REFERENCES carts (Id) ON DELETE CASCADE,
    CONSTRAINT FK_cart_items_products_ProductId FOREIGN KEY (ProductId) REFERENCES products (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_cart_items_CartId_ProductId ON cart_items (CartId, ProductId);
CREATE INDEX IX_cart_items_ProductId ON cart_items (ProductId);

-- Избранное: один список на пользователя.
CREATE TABLE favorites (
    Id     TEXT NOT NULL CONSTRAINT PK_favorites PRIMARY KEY,
    UserId TEXT NOT NULL,
    CONSTRAINT FK_favorites_users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_favorites_UserId ON favorites (UserId);

-- Позиция избранного.
CREATE TABLE favorite_items (
    Id          TEXT    NOT NULL CONSTRAINT PK_favorite_items PRIMARY KEY,
    FavoritesId TEXT    NOT NULL,
    ProductId   TEXT    NOT NULL,
    AddedAt     INTEGER NOT NULL,
    CONSTRAINT FK_favorite_items_favorites_FavoritesId FOREIGN KEY (FavoritesId) REFERENCES favorites (Id) ON DELETE CASCADE,
    CONSTRAINT FK_favorite_items_products_ProductId FOREIGN KEY (ProductId) REFERENCES products (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_favorite_items_FavoritesId_ProductId ON favorite_items (FavoritesId, ProductId);
CREATE INDEX IX_favorite_items_ProductId ON favorite_items (ProductId);

-- Заказ. Адрес доставки хранится в столбцах address_*.
CREATE TABLE orders (
    Id                  TEXT    NOT NULL CONSTRAINT PK_orders PRIMARY KEY,
    UserId              TEXT    NOT NULL,
    PlacedAt            INTEGER NOT NULL,
    Status              INTEGER NOT NULL CONSTRAINT CK_orders_Status CHECK (Status BETWEEN 0 AND 6),
    DeliveryType        INTEGER NULL     CONSTRAINT CK_orders_DeliveryType CHECK (DeliveryType BETWEEN 0 AND 3),
    PaymentMethod       INTEGER NULL     CONSTRAINT CK_orders_PaymentMethod CHECK (PaymentMethod IN (0, 1)),
    total_amount        REAL    NOT NULL CONSTRAINT CK_orders_Total CHECK (total_amount >= 0),
    total_currency      TEXT    NOT NULL CONSTRAINT CK_orders_Currency CHECK (length(total_currency) = 3),
    address_city        TEXT    NULL,
    address_street      TEXT    NULL,
    address_house       TEXT    NULL,
    address_apartment   TEXT    NULL,
    address_postal_code TEXT    NULL,
    address_comment     TEXT    NULL,
    SubmittedAt         INTEGER NULL,
    ConfirmedAt         INTEGER NULL,
    CompletedAt         INTEGER NULL,
    CancelledAt         INTEGER NULL,
    CancellationReason  INTEGER NULL     CONSTRAINT CK_orders_CancellationReason CHECK (CancellationReason BETWEEN 0 AND 2),
    CONSTRAINT FK_orders_users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE RESTRICT,
    CONSTRAINT CK_orders_Cancelled CHECK ((Status = 6) = (CancelledAt IS NOT NULL))
);
CREATE INDEX IX_orders_UserId ON orders (UserId);
CREATE INDEX IX_orders_Status ON orders (Status);

-- Позиция заказа: цена за единицу фиксируется на момент оформления.
CREATE TABLE order_items (
    Id                  TEXT    NOT NULL CONSTRAINT PK_order_items PRIMARY KEY,
    OrderId             TEXT    NOT NULL,
    ProductId           TEXT    NOT NULL,
    Quantity            INTEGER NOT NULL CONSTRAINT CK_order_items_Quantity CHECK (Quantity > 0),
    unit_price_amount   REAL    NOT NULL CONSTRAINT CK_order_items_Price CHECK (unit_price_amount >= 0),
    unit_price_currency TEXT    NOT NULL CONSTRAINT CK_order_items_Currency CHECK (length(unit_price_currency) = 3),
    CONSTRAINT FK_order_items_orders_OrderId FOREIGN KEY (OrderId) REFERENCES orders (Id) ON DELETE CASCADE,
    CONSTRAINT FK_order_items_products_ProductId FOREIGN KEY (ProductId) REFERENCES products (Id) ON DELETE RESTRICT
);
CREATE INDEX IX_order_items_OrderId ON order_items (OrderId);
CREATE INDEX IX_order_items_ProductId ON order_items (ProductId);

-- Платёж по заказу (в том числе отклонённые и возвращённые).
CREATE TABLE payments (
    Id              TEXT    NOT NULL CONSTRAINT PK_payments PRIMARY KEY,
    OrderId         TEXT    NOT NULL,
    Method          INTEGER NOT NULL CONSTRAINT CK_payments_Method CHECK (Method IN (0, 1)),
    Status          INTEGER NOT NULL CONSTRAINT CK_payments_Status CHECK (Status BETWEEN 0 AND 4),
    amount_amount   REAL    NOT NULL CONSTRAINT CK_payments_Amount CHECK (amount_amount >= 0),
    amount_currency TEXT    NOT NULL CONSTRAINT CK_payments_Currency CHECK (length(amount_currency) = 3),
    TransactionId   TEXT    NULL,
    PaidAt          INTEGER NOT NULL,
    FailureReason   TEXT    NULL,
    CONSTRAINT FK_payments_orders_OrderId FOREIGN KEY (OrderId) REFERENCES orders (Id) ON DELETE CASCADE
);
CREATE INDEX IX_payments_OrderId ON payments (OrderId);

-- Доставка: 0..1 на заказ, Id совпадает с Id заказа. CourierId — пользователь-курьер.
CREATE TABLE deliveries (
    Id            TEXT    NOT NULL CONSTRAINT PK_deliveries PRIMARY KEY,
    CourierId     TEXT    NOT NULL,
    Status        INTEGER NOT NULL CONSTRAINT CK_deliveries_Status CHECK (Status BETWEEN 0 AND 4),
    AssignedAt    INTEGER NOT NULL,
    AcceptedAt    INTEGER NULL,
    DeliveredAt   INTEGER NULL,
    FailureReason TEXT    NULL,
    CONSTRAINT FK_deliveries_orders_Id FOREIGN KEY (Id) REFERENCES orders (Id) ON DELETE CASCADE,
    CONSTRAINT FK_deliveries_users_CourierId FOREIGN KEY (CourierId) REFERENCES users (Id) ON DELETE RESTRICT
);
CREATE INDEX IX_deliveries_CourierId ON deliveries (CourierId);

-- Отзыв. У пользователя может быть несколько отзывов на один товар.
CREATE TABLE reviews (
    Id        TEXT    NOT NULL CONSTRAINT PK_reviews PRIMARY KEY,
    UserId    TEXT    NOT NULL,
    ProductId TEXT    NOT NULL,
    Rating    INTEGER NOT NULL CONSTRAINT CK_reviews_Rating CHECK (Rating BETWEEN 1 AND 5),
    Text      TEXT    NULL     CONSTRAINT CK_reviews_Text CHECK (length(Text) <= 4000),
    CreatedAt INTEGER NOT NULL,
    EditedAt  INTEGER NULL,
    CONSTRAINT FK_reviews_users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE,
    CONSTRAINT FK_reviews_products_ProductId FOREIGN KEY (ProductId) REFERENCES products (Id) ON DELETE CASCADE
);
CREATE INDEX IX_reviews_UserId_ProductId ON reviews (UserId, ProductId);
CREATE INDEX IX_reviews_ProductId ON reviews (ProductId);

-- Служебная таблица: локальный кэш ответов (сериализованный JSON).
CREATE TABLE cache (
    Key       TEXT    NOT NULL CONSTRAINT PK_cache PRIMARY KEY,
    Payload   TEXT    NOT NULL,
    StoredAt  INTEGER NOT NULL,
    ExpiresAt INTEGER NULL
);

-- =====================================================================
--  2. ТЕСТОВЫЕ ДАННЫЕ
--  Идентификаторы читаемые: первые 8 цифр — номер таблицы, последние —
--  номер записи (00000002-…-000000000001 — первый пользователь).
--  Точка отсчёта: «сейчас» = 2026-10-30 12:00 UTC.
-- =====================================================================


-- Роли (справочник прав) (5)
INSERT INTO roles (Id, Kind, Name, Description) VALUES
    ('00000001-0000-0000-0000-000000000001', 0, 'Покупатель', 'Просматривает каталог, оформляет и оплачивает заказы'),
    ('00000001-0000-0000-0000-000000000002', 1, 'Менеджер по продажам', 'Управляет каталогом товаров и категорий'),
    ('00000001-0000-0000-0000-000000000003', 2, 'Владелец', 'Полный доступ, управление правами пользователей'),
    ('00000001-0000-0000-0000-000000000004', 3, 'Логист', 'Распределяет заказы по курьерам'),
    ('00000001-0000-0000-0000-000000000005', 4, 'Курьер', 'Доставляет заказы покупателям');

-- Пользователи: покупатели и сотрудники (9)
WITH v (Id, Role, Login, Email, Phone, PasswordHash, FirstName, LastName, RegisteredAt, IsConfirmed, IsBlocked, BlockReason, BlockedAt, DeletedAt) AS (VALUES
    ('00000002-0000-0000-0000-000000000001', 0, 'buyer', 'buyer@nonamebtww.shop', '+79000000001', 'pbkdf2-sha256$100000$/19Z0LNdDZiAbPT6gNi+qQ==$f9zmOmKDxCyiL3ZJNstxlBGLZd8Hhu0GeVanvbGhsPA=', 'Иван', 'Покупателев', '2026-09-01 09:00:00', 1, 0, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000002', 1, 'manager', 'manager@nonamebtww.shop', '+79000000002', 'pbkdf2-sha256$100000$/5Zp2Fy+3rxZ6RiEkkjl6g==$i0AHAYO5EtFRkmU7bT9N/n77HxWHjo1uCJTsNIpUhxA=', 'Мария', 'Менеджерова', '2026-08-15 08:00:00', 1, 0, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000003', 2, 'owner', 'owner@nonamebtww.shop', '+79000000003', 'pbkdf2-sha256$100000$mpsAI5890XgeGBjXLvPdEA==$Ni5yqG5GxDNYvgG58u/A3ACNXeYHt4u1mlGvuaEb2RQ=', 'Олег', 'Владельцев', '2026-08-01 08:00:00', 1, 0, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000004', 3, 'logist', 'logist@nonamebtww.shop', '+79000000004', 'pbkdf2-sha256$100000$55Q+Woi0mftXTaHpMQ747g==$l78XORf9rc2y5sNzIjYWCzvc+vqZ5oEE92nYtNZYmk4=', 'Лидия', 'Логистова', '2026-08-20 08:00:00', 1, 0, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000005', 4, 'courier', 'courier@nonamebtww.shop', '+79000000005', 'pbkdf2-sha256$100000$FfAAXzZ/zF0Dei577CAYeA==$+IHtNVXn8qkVcgu5woeuHooV55cRBXARZbs2iohgYfA=', 'Кирилл', 'Курьеров', '2026-08-20 08:30:00', 1, 0, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000006', 4, 'courier2', 'courier2@nonamebtww.shop', '+79000000006', 'pbkdf2-sha256$100000$hQfC/UlQWcVQcBROC7QM+A==$r53vrbwsnX6O8GQre4WBUxNE2wqUoBRn5mKmEhyfHxQ=', 'Семён', 'Доставкин', '2026-09-05 08:00:00', 1, 0, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000007', 0, 'anna', NULL, '+79161234567', 'pbkdf2-sha256$100000$IjaRkhXl5wcR3Ol92xhE6Q==$40JN3at/G0cRic/vxVsPa2eeNP9iy39Z2JHtLAFRa2A=', 'Анна', 'Смирнова', '2026-09-20 17:45:00', 1, 0, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000008', 0, 'petr', 'petr.ivanov@yandex.ru', NULL, 'pbkdf2-sha256$100000$LKlRjMwbGLFNiNz06Q07ew==$DlrJhKJTb6c/5ox6MiO82WwSCG46wpVdHkhAS/2Cu+c=', 'Пётр', 'Иванов', '2026-09-12 21:10:00', 1, 1, 'Оскорбительные отзывы и спам', '2026-10-25 10:00:00', NULL),
    ('00000002-0000-0000-0000-000000000009', 0, 'deleted_000000020000000000000000', NULL, NULL, 'deleted', 'Удалённый', 'пользователь', '2026-09-10 12:00:00', 1, 0, NULL, NULL, '2026-10-20 18:00:00')
)
INSERT INTO users (Id, Role, Login, Email, Phone, PasswordHash, FirstName, LastName, RegisteredAt, IsConfirmed, IsBlocked, BlockReason, BlockedAt, DeletedAt)
SELECT Id, Role, Login, Email, Phone, PasswordHash, FirstName, LastName,
       (((CAST(strftime('%s', RegisteredAt) AS INTEGER) + 62135596800) * 10000) << 11),
       IsConfirmed, IsBlocked, BlockReason,
       (((CAST(strftime('%s', BlockedAt) AS INTEGER) + 62135596800) * 10000) << 11),
       (((CAST(strftime('%s', DeletedAt) AS INTEGER) + 62135596800) * 10000) << 11)
FROM v;

-- Устройства, с которых входил пользователь (9)
WITH v (Id, UserId, Name, IpAddress, RefreshToken, LastSignInAt, IsActive) AS (VALUES
    ('0000000C-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000001', 'Ivan-iPhone (Unix)', '192.168.1.10', 'gYr7I6DKq-9ltxHBbgPUfKK8GaAEfBUShcEBb2AbRR8', '2026-10-30 11:38:00', 1),
    ('0000000C-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000001', 'Ivan-iPad (Unix)', '192.168.1.11', NULL, '2026-10-12 20:15:00', 0),
    ('0000000C-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000007', 'Pixel-8 (Unix)', '10.0.0.24', 'UKj62fChNCtV8JfJByYg4k14myz_pXK8cUNvtpQcJvc', '2026-10-30 11:48:00', 1),
    ('0000000C-0000-0000-0000-000000000004', '00000002-0000-0000-0000-000000000002', 'Maria-iPhone (Unix)', '192.168.0.31', 'obuIuZ37cMz2jhULxfH5JGCPuhzF7eGshbXd8dTAC7I', '2026-10-30 09:05:00', 1),
    ('0000000C-0000-0000-0000-000000000005', '00000002-0000-0000-0000-000000000003', 'Oleg-MacBook-Pro (Unix)', '192.168.0.2', 'xe4oIlz-dvEXOFbyXyJcnUWHGzYLaZoSfTclBUm5bg4', '2026-10-29 19:30:00', 1),
    ('0000000C-0000-0000-0000-000000000006', '00000002-0000-0000-0000-000000000004', 'Galaxy-S23 (Unix)', '192.168.0.44', 'uwwWY3UVmIeiW1DvfEw0Bqikfi3cTiVV8VkmYSEMyyE', '2026-10-30 08:20:00', 1),
    ('0000000C-0000-0000-0000-000000000007', '00000002-0000-0000-0000-000000000005', 'Redmi-Note-12 (Unix)', '100.64.12.7', '7-vchDa3ebfRD5yx4ctPRhCFkdtFfJNByntY1dk0zsM', '2026-10-30 08:25:00', 1),
    ('0000000C-0000-0000-0000-000000000008', '00000002-0000-0000-0000-000000000006', 'Semyon-iPhone (Unix)', '100.64.12.9', 'piRqoYMPxCkMqbI73j90Qp1rydRFO2BXrewGxtpCo38', '2026-10-30 09:35:00', 1),
    ('0000000C-0000-0000-0000-000000000009', '00000002-0000-0000-0000-000000000008', 'Galaxy-A54 (Unix)', '85.143.10.5', NULL, '2026-10-24 22:00:00', 0)
)
INSERT INTO devices (Id, UserId, Name, IpAddress, RefreshToken, LastSignInAt, IsActive)
SELECT Id, UserId, Name, IpAddress, RefreshToken,
       (((CAST(strftime('%s', LastSignInAt) AS INTEGER) + 62135596800) * 10000) << 11),
       IsActive
FROM v;

-- Коды подтверждения (регистрация, восстановление пароля) (6)
WITH v (Id, UserId, Code, Purpose, CreatedAt, ExpiresAt, IsUsed) AS (VALUES
    ('0000000D-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000001', '482913', 0, '2026-09-01 09:00:00', '2026-09-01 09:15:00', 1),
    ('0000000D-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000007', '305771', 0, '2026-09-20 17:45:00', '2026-09-20 18:00:00', 1),
    ('0000000D-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000008', '918204', 0, '2026-09-12 21:10:00', '2026-09-12 21:25:00', 1),
    ('0000000D-0000-0000-0000-000000000004', '00000002-0000-0000-0000-000000000001', '660145', 1, '2026-10-12 20:05:00', '2026-10-12 20:20:00', 1),
    ('0000000D-0000-0000-0000-000000000005', '00000002-0000-0000-0000-000000000007', '127530', 1, '2026-10-29 21:00:00', '2026-10-29 21:15:00', 0),
    ('0000000D-0000-0000-0000-000000000006', '00000002-0000-0000-0000-000000000009', '774019', 0, '2026-09-10 12:00:00', '2026-09-10 12:15:00', 1)
)
INSERT INTO confirmation_codes (Id, UserId, Code, Purpose, CreatedAt, ExpiresAt, IsUsed)
SELECT Id, UserId, Code, Purpose,
       (((CAST(strftime('%s', CreatedAt) AS INTEGER) + 62135596800) * 10000) << 11),
       (((CAST(strftime('%s', ExpiresAt) AS INTEGER) + 62135596800) * 10000) << 11),
       IsUsed
FROM v;

-- Категории каталога (дерево) (10)
INSERT INTO categories (Id, ParentId, Name, Description, Photo) VALUES
    ('00000003-0000-0000-0000-000000000001', NULL, 'Электроника', 'Смартфоны, планшеты, ноутбуки и аудиотехника', NULL),
    ('00000003-0000-0000-0000-000000000002', '00000003-0000-0000-0000-000000000001', 'Смартфоны', 'Смартфоны Apple, Samsung, OPPO, realme и vivo', NULL),
    ('00000003-0000-0000-0000-000000000003', '00000003-0000-0000-0000-000000000001', 'Планшеты', 'Планшеты для работы, учёбы и развлечений', NULL),
    ('00000003-0000-0000-0000-000000000004', '00000003-0000-0000-0000-000000000001', 'Ноутбуки', 'Ноутбуки для работы, учёбы и творчества', NULL),
    ('00000003-0000-0000-0000-000000000005', '00000003-0000-0000-0000-000000000001', 'Аудио', 'Наушники и умные колонки', NULL),
    ('00000003-0000-0000-0000-000000000006', '00000003-0000-0000-0000-000000000001', 'Умные часы', 'Смарт-часы и фитнес-гаджеты', NULL),
    ('00000003-0000-0000-0000-000000000007', NULL, 'Дом', 'Товары для дома', NULL),
    ('00000003-0000-0000-0000-000000000008', '00000003-0000-0000-0000-000000000007', 'Кухня', 'Техника и посуда для кухни', NULL),
    ('00000003-0000-0000-0000-000000000009', '00000003-0000-0000-0000-000000000007', 'Свет и декор', 'Светильники и предметы интерьера', NULL),
    ('00000003-0000-0000-0000-00000000000A', NULL, 'Аксессуары', 'Чехлы, зарядные устройства и аксессуары для съёмки', NULL);

-- Товары (как в DemoCatalog.cs) (32)
INSERT INTO products (Id, CategoryId, Sku, Name, Description, Characteristics, price_amount, price_currency, Stock, Photo, IsActive) VALUES
    ('00000004-0000-0000-0000-000000000001', '00000003-0000-0000-0000-000000000002', 'SM-IP13P', 'Apple iPhone 13 Pro 128 ГБ', 'Флагман Apple с дисплеем ProMotion 120 Гц, тройной камерой 12 Мп с телеобъективом и процессором A15 Bionic.', replace('Экран: 6,1" Super Retina XDR, 120 Гц\nПроцессор: Apple A15 Bionic\nПамять: 128 ГБ\nКамера: 3 × 12 Мп, оптический зум 3×\nАккумулятор: 3095 мА·ч, MagSafe\nЗащита: IP68\nВес: 204 г', '\n', char(10)), 79990.0, 'RUB', 14, 'https://cdn.dummyjson.com/product-images/smartphones/iphone-13-pro/1.webp', 1),
    ('00000004-0000-0000-0000-000000000002', '00000003-0000-0000-0000-000000000002', 'SM-IPX', 'Apple iPhone X 64 ГБ', 'Первый iPhone с OLED-экраном во всю переднюю панель и Face ID. Подходит тем, кто хочет iPhone по доступной цене.', replace('Экран: 5,8" OLED Super Retina\nПроцессор: Apple A11 Bionic\nПамять: 64 ГБ\nКамера: 2 × 12 Мп\nРазблокировка: Face ID\nЗащита: IP67\nВес: 174 г', '\n', char(10)), 24990.0, 'RUB', 6, 'https://cdn.dummyjson.com/product-images/smartphones/iphone-x/1.webp', 1),
    ('00000004-0000-0000-0000-000000000003', '00000003-0000-0000-0000-000000000002', 'SM-GS10', 'Samsung Galaxy S10 8/128 ГБ', 'Смартфон с изогнутым Dynamic AMOLED-экраном, ультразвуковым сканером отпечатков в экране и тройной камерой.', replace('Экран: 6,1" Dynamic AMOLED, 3040×1440\nПроцессор: Exynos 9820\nОЗУ: 8 ГБ\nПамять: 128 ГБ, microSD\nКамера: 12 + 12 + 16 Мп\nАккумулятор: 3400 мА·ч, беспроводная зарядка\nЗащита: IP68', '\n', char(10)), 29990.0, 'RUB', 9, 'https://cdn.dummyjson.com/product-images/smartphones/samsung-galaxy-s10/1.webp', 1),
    ('00000004-0000-0000-0000-000000000004', '00000003-0000-0000-0000-000000000002', 'SM-GS8', 'Samsung Galaxy S8 4/64 ГБ', 'Компактный смартфон с безрамочным Infinity-экраном, защитой от воды и быстрой зарядкой.', replace('Экран: 5,8" Super AMOLED, 2960×1440\nПроцессор: Exynos 8895\nОЗУ: 4 ГБ\nПамять: 64 ГБ, microSD\nКамера: 12 Мп, f/1.7\nАккумулятор: 3000 мА·ч\nЗащита: IP68', '\n', char(10)), 15990.0, 'RUB', 4, 'https://cdn.dummyjson.com/product-images/smartphones/samsung-galaxy-s8/1.webp', 1),
    ('00000004-0000-0000-0000-000000000005', '00000003-0000-0000-0000-000000000002', 'SM-OF19', 'OPPO F19 Pro+ 5G 8/128 ГБ', 'Тонкий смартфон с поддержкой 5G, AMOLED-экраном и зарядкой 50 Вт: от 0 до 100% примерно за 48 минут.', replace('Экран: 6,43" AMOLED, 2400×1080\nПроцессор: MediaTek Dimensity 800U\nОЗУ: 8 ГБ\nПамять: 128 ГБ\nКамера: 48 + 8 + 2 + 2 Мп\nАккумулятор: 4310 мА·ч, 50 Вт\nСеть: 5G', '\n', char(10)), 23990.0, 'RUB', 18, 'https://cdn.dummyjson.com/product-images/smartphones/oppo-f19-pro-plus/1.webp', 1),
    ('00000004-0000-0000-0000-000000000006', '00000003-0000-0000-0000-000000000002', 'SM-RMXT', 'realme XT 8/128 ГБ', 'Смартфон с основной камерой 64 Мп и экраном Super AMOLED — отличный выбор для любителей мобильной фотографии.', replace('Экран: 6,4" Super AMOLED\nПроцессор: Snapdragon 712\nОЗУ: 8 ГБ\nПамять: 128 ГБ\nКамера: 64 + 8 + 2 + 2 Мп\nАккумулятор: 4000 мА·ч, 20 Вт\nСканер отпечатков: в экране', '\n', char(10)), 17990.0, 'RUB', 22, 'https://cdn.dummyjson.com/product-images/smartphones/realme-xt/1.webp', 1),
    ('00000004-0000-0000-0000-000000000007', '00000003-0000-0000-0000-000000000002', 'SM-VX21', 'vivo X21 6/128 ГБ', 'Один из первых смартфонов со сканером отпечатков пальцев в экране и двойной камерой с AI-обработкой.', replace('Экран: 6,28" Super AMOLED\nПроцессор: Snapdragon 660\nОЗУ: 6 ГБ\nПамять: 128 ГБ\nКамера: 12 + 5 Мп\nФронтальная камера: 12 Мп\nАккумулятор: 3200 мА·ч', '\n', char(10)), 14990.0, 'RUB', 0, 'https://cdn.dummyjson.com/product-images/smartphones/vivo-x21/1.webp', 1),
    ('00000004-0000-0000-0000-000000000008', '00000003-0000-0000-0000-000000000003', 'TB-IPMINI', 'Apple iPad mini (2021) Wi-Fi 64 ГБ, сияющая звезда', 'Компактный планшет с экраном Liquid Retina 8,3", процессором A15 Bionic, USB-C и поддержкой Apple Pencil 2.', replace('Экран: 8,3" Liquid Retina, 2266×1488\nПроцессор: Apple A15 Bionic\nПамять: 64 ГБ\nКамера: 12 Мп, фронтальная 12 Мп Center Stage\nРазъём: USB-C\nСтилус: Apple Pencil (2-го поколения)\nВес: 293 г', '\n', char(10)), 49990.0, 'RUB', 7, 'https://cdn.dummyjson.com/product-images/tablets/ipad-mini-2021-starlight/1.webp', 1),
    ('00000004-0000-0000-0000-000000000009', '00000003-0000-0000-0000-000000000003', 'TB-GTS8P', 'Samsung Galaxy Tab S8+ 8/128 ГБ, графит', 'Большой Super AMOLED-экран 12,4", стилус S Pen в комплекте и режим DeX для работы как на компьютере.', replace('Экран: 12,4" Super AMOLED, 120 Гц\nПроцессор: Snapdragon 8 Gen 1\nОЗУ: 8 ГБ\nПамять: 128 ГБ, microSD\nАккумулятор: 10090 мА·ч, 45 Вт\nСтилус: S Pen в комплекте\nВес: 567 г', '\n', char(10)), 69990.0, 'RUB', 5, 'https://cdn.dummyjson.com/product-images/tablets/samsung-galaxy-tab-s8-plus-grey/1.webp', 1),
    ('00000004-0000-0000-0000-00000000000A', '00000003-0000-0000-0000-000000000004', 'NB-MBP14', 'Apple MacBook Pro 14" M1 Pro 16/512 ГБ, серый космос', 'Профессиональный ноутбук с экраном Liquid Retina XDR, чипом M1 Pro и автономностью до 17 часов.', replace('Экран: 14,2" Liquid Retina XDR, 120 Гц\nПроцессор: Apple M1 Pro, 8 CPU / 14 GPU\nОЗУ: 16 ГБ\nSSD: 512 ГБ\nПорты: 3 × Thunderbolt 4, HDMI, SDXC, MagSafe 3\nАвтономность: до 17 ч\nВес: 1,6 кг', '\n', char(10)), 189990.0, 'RUB', 3, 'https://cdn.dummyjson.com/product-images/laptops/apple-macbook-pro-14-inch-space-grey/1.webp', 1),
    ('00000004-0000-0000-0000-00000000000B', '00000003-0000-0000-0000-000000000004', 'NB-ZENDUO', 'ASUS ZenBook Pro Duo UX581', 'Ноутбук с двумя экранами: основной 4K OLED и сенсорный ScreenPad Plus над клавиатурой для монтажа и многозадачности.', replace('Экран: 15,6" 4K OLED, сенсорный\nДоп. экран: ScreenPad Plus 14"\nПроцессор: Intel Core i7-10750H\nОЗУ: 16 ГБ\nSSD: 1 ТБ\nВидеокарта: NVIDIA GeForce RTX 2060\nВес: 2,5 кг', '\n', char(10)), 159990.0, 'RUB', 2, 'https://cdn.dummyjson.com/product-images/laptops/asus-zenbook-pro-dual-screen-laptop/1.webp', 1),
    ('00000004-0000-0000-0000-00000000000C', '00000003-0000-0000-0000-000000000004', 'NB-MATEX', 'HUAWEI MateBook X Pro 16/1024 ГБ', 'Тонкий металлический ноутбук с сенсорным 3K-экраном почти без рамок и быстрой зарядкой USB-C.', replace('Экран: 13,9" 3K LTPS, сенсорный\nПроцессор: Intel Core i7\nОЗУ: 16 ГБ\nSSD: 1 ТБ\nЗвук: 4 динамика\nЗарядка: USB-C, 65 Вт\nВес: 1,33 кг', '\n', char(10)), 129990.0, 'RUB', 6, 'https://cdn.dummyjson.com/product-images/laptops/huawei-matebook-x-pro/1.webp', 1),
    ('00000004-0000-0000-0000-00000000000D', '00000003-0000-0000-0000-000000000004', 'NB-YOGA920', 'Lenovo Yoga 920 13,9"', 'Ноутбук-трансформер с поворотным на 360° экраном, поддержкой стилуса и корпусом из алюминия.', replace('Экран: 13,9" Full HD IPS, сенсорный\nПроцессор: Intel Core i7-8550U\nОЗУ: 8 ГБ\nSSD: 256 ГБ\nКорпус: трансформер 360°\nПорты: 2 × Thunderbolt 3, USB-A\nВес: 1,37 кг', '\n', char(10)), 79990.0, 'RUB', 4, 'https://cdn.dummyjson.com/product-images/laptops/lenovo-yoga-920/1.webp', 1),
    ('00000004-0000-0000-0000-00000000000E', '00000003-0000-0000-0000-000000000004', 'NB-XPS13', 'Dell XPS 13 9300', 'Компактный ноутбук с экраном InfinityEdge 16:10 — 13,4 дюйма в корпусе размером с 11-дюймовый.', replace('Экран: 13,4" FHD+ InfinityEdge, 16:10\nПроцессор: Intel Core i7-1065G7\nОЗУ: 16 ГБ LPDDR4x\nSSD: 512 ГБ\nПорты: 2 × Thunderbolt 3\nАвтономность: до 14 ч\nВес: 1,2 кг', '\n', char(10)), 109990.0, 'RUB', 5, 'https://cdn.dummyjson.com/product-images/laptops/new-dell-xps-13-9300-laptop/1.webp', 1),
    ('00000004-0000-0000-0000-00000000000F', '00000003-0000-0000-0000-000000000005', 'AU-APODS', 'Apple AirPods (2-го поколения)', 'Беспроводные наушники с автоматическим подключением к устройствам Apple и голосовым вызовом Siri.', replace('Тип: вкладыши, TWS\nЧип: Apple H1\nРабота: до 5 ч, с кейсом до 24 ч\nКейс: зарядный, Lightning\nМикрофоны: 2, с подавлением шума\nУправление: двойное касание', '\n', char(10)), 12990.0, 'RUB', 30, 'https://cdn.dummyjson.com/product-images/mobile-accessories/apple-airpods/1.webp', 1),
    ('00000004-0000-0000-0000-000000000010', '00000003-0000-0000-0000-000000000005', 'AU-APMAX', 'Apple AirPods Max, серебристый', 'Полноразмерные наушники с активным шумоподавлением, режимом прозрачности и пространственным звуком.', replace('Тип: полноразмерные, закрытые\nЧип: Apple H1 в каждом наушнике\nШумоподавление: активное\nПространственный звук: да, с отслеживанием головы\nРабота: до 20 ч\nВес: 385 г', '\n', char(10)), 54990.0, 'RUB', 8, 'https://cdn.dummyjson.com/product-images/mobile-accessories/apple-airpods-max-silver/1.webp', 1),
    ('00000004-0000-0000-0000-000000000011', '00000003-0000-0000-0000-000000000005', 'AU-BFLEX', 'Beats Flex', 'Беспроводные наушники на шнурке с магнитными амбушюрами: при соединении музыка ставится на паузу.', replace('Тип: внутриканальные, с шейным шнурком\nЧип: Apple W1\nРабота: до 12 ч\nЗарядка: USB-C, Fast Fuel\nМикрофон: да\nАмбушюры: 4 размера в комплекте', '\n', char(10)), 5990.0, 'RUB', 45, 'https://cdn.dummyjson.com/product-images/mobile-accessories/beats-flex-wireless-earphones/1.webp', 1),
    ('00000004-0000-0000-0000-000000000012', '00000003-0000-0000-0000-000000000005', 'AU-HPMINI', 'Apple HomePod mini, серый космос', 'Компактная умная колонка с объёмным звуком на 360°, Siri и управлением умным домом.', replace('Звук: 360°, вычислительная обработка\nАссистент: Siri\nУмный дом: Apple Home, Thread\nПодключение: Wi-Fi, Bluetooth 5.0\nПитание: USB-C, 20 Вт\nВысота: 84 мм', '\n', char(10)), 11990.0, 'RUB', 12, 'https://cdn.dummyjson.com/product-images/mobile-accessories/apple-homepod-mini-cosmic-grey/1.webp', 1),
    ('00000004-0000-0000-0000-000000000013', '00000003-0000-0000-0000-000000000005', 'AU-ECHOP', 'Amazon Echo Plus (2-го поколения)', 'Умная колонка с голосовым ассистентом Alexa, встроенным хабом Zigbee и датчиком температуры.', replace('Ассистент: Amazon Alexa\nДинамики: 3" вуфер + 0,8" твитер\nУмный дом: хаб Zigbee\nДатчики: температуры\nПодключение: Wi-Fi, Bluetooth\nВыход: 3,5 мм', '\n', char(10)), 8990.0, 'RUB', 0, 'https://cdn.dummyjson.com/product-images/mobile-accessories/amazon-echo-plus/1.webp', 1),
    ('00000004-0000-0000-0000-000000000014', '00000003-0000-0000-0000-000000000006', 'WR-AW4G', 'Apple Watch Series 4 GPS 40 мм, золотой алюминий', 'Смарт-часы с ЭКГ, распознаванием падений и экраном, который на 30% больше, чем у предыдущей серии.', replace('Корпус: 40 мм, алюминий\nЭкран: LTPO OLED Retina\nДатчики: ЭКГ, пульс, акселерометр\nФункции: распознавание падений\nЗащита: водонепроницаемость 50 м\nРабота: до 18 ч', '\n', char(10)), 17990.0, 'RUB', 10, 'https://cdn.dummyjson.com/product-images/mobile-accessories/apple-watch-series-4-gold/1.webp', 1),
    ('00000004-0000-0000-0000-000000000015', '00000003-0000-0000-0000-000000000008', 'KT-BLEND', 'Стационарный блендер 1,5 л', 'Мощный блендер для смузи, коктейлей, супов-пюре и колки льда; чаша из прочного стекла.', replace('Мощность: 1000 Вт\nЧаша: 1,5 л, стекло\nСкоростей: 5 + импульсный режим\nНожи: 6 лезвий, нержавеющая сталь\nФункции: колка льда\nЗащита: блокировка без крышки', '\n', char(10)), 6490.0, 'RUB', 15, 'https://cdn.dummyjson.com/product-images/kitchen-accessories/boxed-blender/1.webp', 1),
    ('00000004-0000-0000-0000-000000000016', '00000003-0000-0000-0000-000000000008', 'KT-HBLEND', 'Погружной блендер с венчиком', 'Компактный погружной блендер с насадкой-венчиком для взбивания и измельчения.', replace('Мощность: 800 Вт\nСкоростей: 2 + турбо\nНасадки: блендерная, венчик\nПогружная часть: нержавеющая сталь\nДлина шнура: 1,2 м\nВес: 0,9 кг', '\n', char(10)), 3290.0, 'RUB', 25, 'https://cdn.dummyjson.com/product-images/kitchen-accessories/hand-blender/1.webp', 1),
    ('00000004-0000-0000-0000-000000000017', '00000003-0000-0000-0000-000000000008', 'KT-MICRO', 'Микроволновая печь 20 л', 'Микроволновка с электронным управлением, грилем и автоматическими программами разморозки.', replace('Объём: 20 л\nМощность СВЧ: 700 Вт\nГриль: 1000 Вт\nУправление: электронное\nАвтопрограмм: 8\nПокрытие камеры: эмаль', '\n', char(10)), 8990.0, 'RUB', 9, 'https://cdn.dummyjson.com/product-images/kitchen-accessories/microwave-oven/1.webp', 1),
    ('00000004-0000-0000-0000-000000000018', '00000003-0000-0000-0000-000000000008', 'KT-STOVE', 'Электроплитка настольная', 'Настольная плитка с чугунной конфоркой — для дачи, общежития или как дополнительная конфорка.', replace('Конфорок: 1\nМощность: 1500 Вт\nТип конфорки: чугунная\nРегулировка: плавная, 5 режимов\nКорпус: металл\nДиаметр конфорки: 18,5 см', '\n', char(10)), 2490.0, 'RUB', 20, 'https://cdn.dummyjson.com/product-images/kitchen-accessories/electric-stove/1.webp', 1),
    ('00000004-0000-0000-0000-000000000019', '00000003-0000-0000-0000-000000000008', 'KT-WOK', 'Сковорода-вок из углеродистой стали 30 см', 'Классический вок для быстрой обжарки на сильном огне; подходит для газовых и индукционных плит.', replace('Диаметр: 30 см\nМатериал: углеродистая сталь\nТолщина стенок: 1,5 мм\nПлиты: газ, электро, индукция\nРучка: дерево\nВес: 1,4 кг', '\n', char(10)), 2190.0, 'RUB', 0, 'https://cdn.dummyjson.com/product-images/kitchen-accessories/carbon-steel-wok/1.webp', 1),
    ('00000004-0000-0000-0000-00000000001A', '00000003-0000-0000-0000-000000000008', 'KT-POT', 'Кастрюля 3 л со стеклянной крышкой', 'Кастрюля из нержавеющей стали с капсульным дном для равномерного нагрева.', replace('Объём: 3 л\nМатериал: нержавеющая сталь 18/10\nДно: капсульное\nКрышка: жаропрочное стекло\nПлиты: все, включая индукцию\nПосудомойка: можно', '\n', char(10)), 2790.0, 'RUB', 18, 'https://cdn.dummyjson.com/product-images/kitchen-accessories/silver-pot-with-glass-cap/1.webp', 1),
    ('00000004-0000-0000-0000-00000000001B', '00000003-0000-0000-0000-000000000009', 'DC-LAMP', 'Настольная лампа с тканевым абажуром', 'Лампа в классическом стиле: керамическое основание и абажур из ткани рассеивают мягкий тёплый свет.', replace('Цоколь: E27\nМакс. мощность лампы: 40 Вт\nВысота: 45 см\nОснование: керамика\nАбажур: ткань\nВыключатель: на проводе', '\n', char(10)), 3490.0, 'RUB', 11, 'https://cdn.dummyjson.com/product-images/home-decoration/table-lamp/1.webp', 1),
    ('00000004-0000-0000-0000-00000000001C', '00000003-0000-0000-0000-000000000009', 'DC-POT', 'Кашпо керамическое с растением', 'Керамическое кашпо с декоративным растением — живой акцент для рабочего стола или полки.', replace('Материал: керамика\nДиаметр: 14 см\nВысота: 16 см\nДренаж: отверстие и поддон\nЦвет: белый\nРастение: в комплекте', '\n', char(10)), 1590.0, 'RUB', 14, 'https://cdn.dummyjson.com/product-images/home-decoration/plant-pot/1.webp', 1),
    ('00000004-0000-0000-0000-00000000001D', '00000003-0000-0000-0000-00000000000A', 'AC-USBC20', 'Адаптер питания Apple USB-C 20 Вт', 'Оригинальный адаптер для быстрой зарядки iPhone: до 50% заряда примерно за 30 минут.', replace('Мощность: 20 Вт\nРазъём: USB-C\nБыстрая зарядка: USB Power Delivery\nСовместимость: iPhone, iPad, AirPods\nВилка: евро\nКабель: не входит', '\n', char(10)), 2490.0, 'RUB', 60, 'https://cdn.dummyjson.com/product-images/mobile-accessories/apple-iphone-charger/1.webp', 1),
    ('00000004-0000-0000-0000-00000000001E', '00000003-0000-0000-0000-00000000000A', 'AC-MSBAT', 'Apple MagSafe Battery Pack', 'Внешний аккумулятор, который магнитом крепится к iPhone и заряжает его на ходу.', replace('Крепление: MagSafe\nЁмкость: 1460 мА·ч\nЗарядка iPhone: до 7,5 Вт\nВход: Lightning, до 20 Вт\nСовместимость: iPhone 12 и новее\nВес: 115 г', '\n', char(10)), 9990.0, 'RUB', 13, 'https://cdn.dummyjson.com/product-images/mobile-accessories/apple-magsafe-battery-pack/1.webp', 1),
    ('00000004-0000-0000-0000-00000000001F', '00000003-0000-0000-0000-00000000000A', 'AC-CASE12', 'Силиконовый чехол MagSafe для iPhone 12, сливовый', 'Оригинальный чехол с мягкой подкладкой из микрофибры и встроенными магнитами MagSafe.', replace('Совместимость: iPhone 12 / 12 Pro\nМатериал: силикон, микрофибра\nКрепление: MagSafe\nЦвет: сливовый\nЗащита: бортик над экраном и камерой\nБеспроводная зарядка: да', '\n', char(10)), 3990.0, 'RUB', 35, 'https://cdn.dummyjson.com/product-images/mobile-accessories/iphone-12-silicone-case-with-magsafe-plum/1.webp', 1),
    ('00000004-0000-0000-0000-000000000020', '00000003-0000-0000-0000-00000000000A', 'AC-SELFIE', 'Селфи-палка монопод с Bluetooth-пультом', 'Складной монопод с беспроводным пультом и держателем для смартфонов шириной до 8,5 см.', replace('Длина: 20–100 см\nПульт: Bluetooth 5.0\nДержатель: смартфоны 5,5–8,5 см\nВращение: 360°\nМатериал: алюминий\nВес: 160 г', '\n', char(10)), 1290.0, 'RUB', 40, 'https://cdn.dummyjson.com/product-images/mobile-accessories/selfie-stick-monopod/1.webp', 1);

-- Корзины: по одной на пользователя (9)
INSERT INTO carts (Id, UserId) VALUES
    ('00000005-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000001'),
    ('00000005-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000002'),
    ('00000005-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000003'),
    ('00000005-0000-0000-0000-000000000004', '00000002-0000-0000-0000-000000000004'),
    ('00000005-0000-0000-0000-000000000005', '00000002-0000-0000-0000-000000000005'),
    ('00000005-0000-0000-0000-000000000006', '00000002-0000-0000-0000-000000000006'),
    ('00000005-0000-0000-0000-000000000007', '00000002-0000-0000-0000-000000000007'),
    ('00000005-0000-0000-0000-000000000008', '00000002-0000-0000-0000-000000000008'),
    ('00000005-0000-0000-0000-000000000009', '00000002-0000-0000-0000-000000000009');

-- Позиции корзины (7)
WITH v (Id, CartId, ProductId, Quantity, AddedAt) AS (VALUES
    ('00000006-0000-0000-0000-000000000001', '00000005-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000003', 1, '2026-10-29 20:10:00'),
    ('00000006-0000-0000-0000-000000000002', '00000005-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000011', 2, '2026-10-29 20:12:00'),
    ('00000006-0000-0000-0000-000000000003', '00000005-0000-0000-0000-000000000007', '00000004-0000-0000-0000-000000000014', 1, '2026-10-28 13:00:00'),
    ('00000006-0000-0000-0000-000000000004', '00000005-0000-0000-0000-000000000007', '00000004-0000-0000-0000-00000000001E', 1, '2026-10-28 13:02:00'),
    ('00000006-0000-0000-0000-000000000005', '00000005-0000-0000-0000-000000000002', '00000004-0000-0000-0000-000000000017', 1, '2026-10-27 12:30:00'),
    ('00000006-0000-0000-0000-000000000006', '00000005-0000-0000-0000-000000000008', '00000004-0000-0000-0000-00000000000F', 1, '2026-10-24 21:40:00'),
    ('00000006-0000-0000-0000-000000000007', '00000005-0000-0000-0000-000000000003', '00000004-0000-0000-0000-00000000000E', 1, '2026-10-26 10:00:00')
)
INSERT INTO cart_items (Id, CartId, ProductId, Quantity, AddedAt)
SELECT Id, CartId, ProductId, Quantity,
       (((CAST(strftime('%s', AddedAt) AS INTEGER) + 62135596800) * 10000) << 11)
FROM v;

-- Избранное: по одному списку на пользователя (5)
INSERT INTO favorites (Id, UserId) VALUES
    ('00000007-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000001'),
    ('00000007-0000-0000-0000-000000000007', '00000002-0000-0000-0000-000000000007'),
    ('00000007-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000002'),
    ('00000007-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000003'),
    ('00000007-0000-0000-0000-000000000005', '00000002-0000-0000-0000-000000000005');

-- Позиции избранного (7)
WITH v (Id, FavoritesId, ProductId, AddedAt) AS (VALUES
    ('00000008-0000-0000-0000-000000000001', '00000007-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000010', '2026-10-05 19:00:00'),
    ('00000008-0000-0000-0000-000000000002', '00000007-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000008', '2026-10-05 19:01:00'),
    ('00000008-0000-0000-0000-000000000003', '00000007-0000-0000-0000-000000000007', '00000004-0000-0000-0000-000000000001', '2026-10-21 08:15:00'),
    ('00000008-0000-0000-0000-000000000004', '00000007-0000-0000-0000-000000000007', '00000004-0000-0000-0000-000000000012', '2026-10-21 08:16:00'),
    ('00000008-0000-0000-0000-000000000005', '00000007-0000-0000-0000-000000000002', '00000004-0000-0000-0000-00000000000C', '2026-10-18 13:20:00'),
    ('00000008-0000-0000-0000-000000000006', '00000007-0000-0000-0000-000000000003', '00000004-0000-0000-0000-000000000009', '2026-10-11 09:00:00'),
    ('00000008-0000-0000-0000-000000000007', '00000007-0000-0000-0000-000000000005', '00000004-0000-0000-0000-000000000020', '2026-10-19 18:40:00')
)
INSERT INTO favorite_items (Id, FavoritesId, ProductId, AddedAt)
SELECT Id, FavoritesId, ProductId,
       (((CAST(strftime('%s', AddedAt) AS INTEGER) + 62135596800) * 10000) << 11)
FROM v;

-- Заказы во всех статусах (10)
WITH v (Id, UserId, PlacedAt, Status, DeliveryType, PaymentMethod, total_amount, total_currency, address_city, address_street, address_house, address_apartment, address_postal_code, address_comment, SubmittedAt, ConfirmedAt, CompletedAt, CancelledAt, CancellationReason) AS (VALUES
    ('00000009-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000001', '2026-10-02 10:15:00', 5, 0, 0, 82480.0, 'RUB', 'Москва', 'ул. Тверская', '7', '15', '125009', 'Домофон 15, третий этаж', '2026-10-02 10:20:00', '2026-10-02 10:21:00', '2026-10-03 12:10:00', NULL, NULL),
    ('00000009-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000001', '2026-10-29 18:00:00', 4, 1, 1, 189990.0, 'RUB', 'Москва', 'ул. Тверская', '7', '15', '125009', 'Домофон 15, третий этаж', '2026-10-29 18:05:00', '2026-10-29 18:06:00', NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000007', '2026-10-29 12:00:00', 3, 3, 0, 62970.0, 'RUB', 'Москва', 'ул. Профсоюзная', '56', NULL, '117393', 'Постамат у входа в ТЦ', '2026-10-29 12:04:00', '2026-10-29 12:05:00', NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000004', '00000002-0000-0000-0000-000000000007', '2026-10-25 15:00:00', 2, 0, 0, 9280.0, 'RUB', 'Москва', 'Ленинский проспект', '32', '48', '119334', NULL, '2026-10-25 15:03:00', '2026-10-25 15:04:00', NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000005', '00000002-0000-0000-0000-000000000001', '2026-10-30 11:40:00', 1, 2, 0, 49990.0, 'RUB', 'Москва', 'ул. Новый Арбат', '15', NULL, '119019', 'Пункт самовывоза магазина', '2026-10-30 11:45:00', NULL, NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000006', '00000002-0000-0000-0000-000000000002', '2026-10-20 09:00:00', 6, 0, 1, 6980.0, 'RUB', 'Москва', 'ул. Земляной Вал', '9', '112', '105064', NULL, '2026-10-20 09:02:00', '2026-10-20 09:03:00', NULL, '2026-10-20 16:30:00', 1),
    ('00000009-0000-0000-0000-000000000007', '00000002-0000-0000-0000-000000000008', '2026-10-22 20:00:00', 6, 0, 0, 7280.0, 'RUB', 'Подольск', 'ул. Кирова', '11', '5', '142110', NULL, '2026-10-22 20:02:00', NULL, NULL, '2026-10-22 20:32:00', 2),
    ('00000009-0000-0000-0000-000000000008', '00000002-0000-0000-0000-000000000007', '2026-10-30 11:50:00', 0, 0, NULL, 1590.0, 'RUB', 'Москва', 'Ленинский проспект', '32', '48', '119334', NULL, NULL, NULL, NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000009', '00000002-0000-0000-0000-000000000003', '2026-10-10 12:00:00', 5, 2, 1, 11480.0, 'RUB', 'Москва', 'ул. Новый Арбат', '15', NULL, '119019', 'Пункт самовывоза магазина', '2026-10-10 12:01:00', '2026-10-10 12:02:00', '2026-10-11 13:00:00', NULL, NULL),
    ('00000009-0000-0000-0000-00000000000A', '00000002-0000-0000-0000-000000000001', '2026-10-15 19:00:00', 6, NULL, NULL, 15990.0, 'RUB', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, '2026-10-15 19:10:00', 0)
)
INSERT INTO orders (Id, UserId, PlacedAt, Status, DeliveryType, PaymentMethod, total_amount, total_currency, address_city, address_street, address_house, address_apartment, address_postal_code, address_comment, SubmittedAt, ConfirmedAt, CompletedAt, CancelledAt, CancellationReason)
SELECT Id, UserId,
       (((CAST(strftime('%s', PlacedAt) AS INTEGER) + 62135596800) * 10000) << 11),
       Status, DeliveryType, PaymentMethod, total_amount, total_currency, address_city, address_street, address_house, address_apartment, address_postal_code, address_comment,
       (((CAST(strftime('%s', SubmittedAt) AS INTEGER) + 62135596800) * 10000) << 11),
       (((CAST(strftime('%s', ConfirmedAt) AS INTEGER) + 62135596800) * 10000) << 11),
       (((CAST(strftime('%s', CompletedAt) AS INTEGER) + 62135596800) * 10000) << 11),
       (((CAST(strftime('%s', CancelledAt) AS INTEGER) + 62135596800) * 10000) << 11),
       CancellationReason
FROM v;

-- Позиции заказа (цена фиксируется на момент заказа) (15)
INSERT INTO order_items (Id, OrderId, ProductId, Quantity, unit_price_amount, unit_price_currency) VALUES
    ('0000000A-0000-0000-0000-000000000001', '00000009-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000001', 1, 79990.0, 'RUB'),
    ('0000000A-0000-0000-0000-000000000002', '00000009-0000-0000-0000-000000000001', '00000004-0000-0000-0000-00000000001D', 1, 2490.0, 'RUB'),
    ('0000000A-0000-0000-0000-000000000003', '00000009-0000-0000-0000-000000000002', '00000004-0000-0000-0000-00000000000A', 1, 189990.0, 'RUB'),
    ('0000000A-0000-0000-0000-000000000004', '00000009-0000-0000-0000-000000000003', '00000004-0000-0000-0000-000000000010', 1, 54990.0, 'RUB'),
    ('0000000A-0000-0000-0000-000000000005', '00000009-0000-0000-0000-000000000003', '00000004-0000-0000-0000-00000000001F', 2, 3990.0, 'RUB'),
    ('0000000A-0000-0000-0000-000000000006', '00000009-0000-0000-0000-000000000004', '00000004-0000-0000-0000-000000000015', 1, 6490.0, 'RUB'),
    ('0000000A-0000-0000-0000-000000000007', '00000009-0000-0000-0000-000000000004', '00000004-0000-0000-0000-00000000001A', 1, 2790.0, 'RUB'),
    ('0000000A-0000-0000-0000-000000000008', '00000009-0000-0000-0000-000000000005', '00000004-0000-0000-0000-000000000008', 1, 49990.0, 'RUB'),
    ('0000000A-0000-0000-0000-000000000009', '00000009-0000-0000-0000-000000000006', '00000004-0000-0000-0000-00000000001B', 2, 3490.0, 'RUB'),
    ('0000000A-0000-0000-0000-00000000000A', '00000009-0000-0000-0000-000000000007', '00000004-0000-0000-0000-000000000020', 1, 1290.0, 'RUB'),
    ('0000000A-0000-0000-0000-00000000000B', '00000009-0000-0000-0000-000000000007', '00000004-0000-0000-0000-000000000011', 1, 5990.0, 'RUB'),
    ('0000000A-0000-0000-0000-00000000000C', '00000009-0000-0000-0000-000000000008', '00000004-0000-0000-0000-00000000001C', 1, 1590.0, 'RUB'),
    ('0000000A-0000-0000-0000-00000000000D', '00000009-0000-0000-0000-000000000009', '00000004-0000-0000-0000-000000000017', 1, 8990.0, 'RUB'),
    ('0000000A-0000-0000-0000-00000000000E', '00000009-0000-0000-0000-000000000009', '00000004-0000-0000-0000-000000000018', 1, 2490.0, 'RUB'),
    ('0000000A-0000-0000-0000-00000000000F', '00000009-0000-0000-0000-00000000000A', '00000004-0000-0000-0000-000000000004', 1, 15990.0, 'RUB');

-- Платежи (7)
WITH v (Id, OrderId, Method, Status, amount_amount, amount_currency, TransactionId, PaidAt, FailureReason) AS (VALUES
    ('0000000B-0000-0000-0000-000000000001', '00000009-0000-0000-0000-000000000001', 0, 1, 82480.0, 'RUB', 'FAKE-FFEC4F8B898E001B', '2026-10-02 10:21:00', NULL),
    ('0000000B-0000-0000-0000-000000000002', '00000009-0000-0000-0000-000000000002', 1, 1, 189990.0, 'RUB', 'FAKE-D8DAAF40C5F782B0', '2026-10-29 18:06:00', NULL),
    ('0000000B-0000-0000-0000-000000000003', '00000009-0000-0000-0000-000000000003', 0, 1, 62970.0, 'RUB', 'FAKE-69630178DD2D8544', '2026-10-29 12:05:00', NULL),
    ('0000000B-0000-0000-0000-000000000004', '00000009-0000-0000-0000-000000000004', 0, 1, 9280.0, 'RUB', 'FAKE-AA4DE1D4AEDF7011', '2026-10-25 15:04:00', NULL),
    ('0000000B-0000-0000-0000-000000000005', '00000009-0000-0000-0000-000000000005', 0, 2, 49990.0, 'RUB', 'FAKE-72AF42081A1A1565', '2026-10-30 11:46:00', 'Недостаточно средств на карте'),
    ('0000000B-0000-0000-0000-000000000006', '00000009-0000-0000-0000-000000000006', 1, 4, 6980.0, 'RUB', 'FAKE-1A1E4BFDDA656245', '2026-10-20 09:03:00', NULL),
    ('0000000B-0000-0000-0000-000000000007', '00000009-0000-0000-0000-000000000009', 1, 1, 11480.0, 'RUB', 'FAKE-BFD06B3FA0A5CBD1', '2026-10-10 12:02:00', NULL)
)
INSERT INTO payments (Id, OrderId, Method, Status, amount_amount, amount_currency, TransactionId, PaidAt, FailureReason)
SELECT Id, OrderId, Method, Status, amount_amount, amount_currency, TransactionId,
       (((CAST(strftime('%s', PaidAt) AS INTEGER) + 62135596800) * 10000) << 11),
       FailureReason
FROM v;

-- Доставки (Id = Id заказа) (6)
WITH v (Id, CourierId, Status, AssignedAt, AcceptedAt, DeliveredAt, FailureReason) AS (VALUES
    ('00000009-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000005', 2, '2026-10-02 14:00:00', '2026-10-03 09:30:00', '2026-10-03 12:10:00', NULL),
    ('00000009-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000006', 1, '2026-10-30 09:00:00', '2026-10-30 09:40:00', NULL, NULL),
    ('00000009-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000005', 0, '2026-10-30 08:30:00', NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000004', '00000002-0000-0000-0000-000000000006', 3, '2026-10-26 10:00:00', '2026-10-27 11:00:00', NULL, 'Покупатель не отвечал на звонки, дверь не открыли'),
    ('00000009-0000-0000-0000-000000000006', '00000002-0000-0000-0000-000000000005', 4, '2026-10-20 13:00:00', NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000009', '00000002-0000-0000-0000-000000000006', 2, '2026-10-10 15:00:00', '2026-10-11 10:00:00', '2026-10-11 13:00:00', NULL)
)
INSERT INTO deliveries (Id, CourierId, Status, AssignedAt, AcceptedAt, DeliveredAt, FailureReason)
SELECT Id, CourierId, Status,
       (((CAST(strftime('%s', AssignedAt) AS INTEGER) + 62135596800) * 10000) << 11),
       (((CAST(strftime('%s', AcceptedAt) AS INTEGER) + 62135596800) * 10000) << 11),
       (((CAST(strftime('%s', DeliveredAt) AS INTEGER) + 62135596800) * 10000) << 11),
       FailureReason
FROM v;

-- Отзывы (9)
WITH v (Id, UserId, ProductId, Rating, Text, CreatedAt, EditedAt) AS (VALUES
    ('0000000E-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000001', 5, 'Экран 120 Гц — после него на другие смотреть не хочется. Камера отличная.', '2026-10-18 12:00:00', NULL),
    ('0000000E-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000001', 4, 'Через месяц: держит день активного использования, но греется в играх.', '2026-10-28 12:00:00', NULL),
    ('0000000E-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000003', '00000004-0000-0000-0000-000000000001', 5, 'Брали в офис для сотрудников, все довольны.', '2026-10-10 12:00:00', NULL),
    ('0000000E-0000-0000-0000-000000000004', '00000002-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000010', 4, 'Шумоподавление отличное, но кейс странный и наушники тяжеловаты.', '2026-10-25 12:00:00', '2026-10-26 12:00:00'),
    ('0000000E-0000-0000-0000-000000000005', '00000002-0000-0000-0000-000000000002', '00000004-0000-0000-0000-000000000010', 5, 'Лучший звук, который я слышала в беспроводных наушниках.', '2026-10-21 12:00:00', NULL),
    ('0000000E-0000-0000-0000-000000000006', '00000002-0000-0000-0000-000000000001', '00000004-0000-0000-0000-00000000000A', 5, 'Тихий, быстрый, экран потрясающий. Батареи хватает на весь день.', '2026-10-15 12:00:00', NULL),
    ('0000000E-0000-0000-0000-000000000007', '00000002-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000015', 4, 'Лёд колет без проблем, но довольно шумный.', '2026-10-23 12:00:00', NULL),
    ('0000000E-0000-0000-0000-000000000008', '00000002-0000-0000-0000-000000000004', '00000004-0000-0000-0000-00000000001D', 5, 'Заряжает быстро, не греется.', '2026-10-27 12:00:00', NULL),
    ('0000000E-0000-0000-0000-000000000009', '00000002-0000-0000-0000-000000000007', '00000004-0000-0000-0000-000000000012', 5, 'Маленькая, но звук для своего размера отличный.', '2026-10-26 12:00:00', NULL)
)
INSERT INTO reviews (Id, UserId, ProductId, Rating, Text, CreatedAt, EditedAt)
SELECT Id, UserId, ProductId, Rating, Text,
       (((CAST(strftime('%s', CreatedAt) AS INTEGER) + 62135596800) * 10000) << 11),
       (((CAST(strftime('%s', EditedAt) AS INTEGER) + 62135596800) * 10000) << 11)
FROM v;

-- Локальный кэш ответов приложения (5)
WITH v (Key, Payload, StoredAt, ExpiresAt) AS (VALUES
    ('categories', '[{"id": "00000003-0000-0000-0000-000000000001", "name": "Электроника"}, {"id": "00000003-0000-0000-0000-000000000007", "name": "Дом"}, {"id": "00000003-0000-0000-0000-00000000000a", "name": "Аксессуары"}]', '2026-10-30 11:38:30', '2026-10-30 12:38:30'),
    ('product:00000004-0000-0000-0000-000000000001', '{"id": "00000004-0000-0000-0000-000000000001", "name": "Apple iPhone 13 Pro 128 ГБ", "price": 79990, "stock": 14}', '2026-10-30 11:39:10', '2026-10-30 11:49:10'),
    ('catalog:smartphones:price-asc', '{"page": 1, "total": 7}', '2026-10-30 11:39:00', '2026-10-30 11:44:00'),
    ('orders:active', '{"count": 2}', '2026-10-30 11:46:30', '2026-10-30 11:51:30'),
    ('profile', '{"login": "buyer", "firstName": "Иван", "lastName": "Покупателев"}', '2026-10-30 11:38:20', NULL)
)
INSERT INTO cache (Key, Payload, StoredAt, ExpiresAt)
SELECT Key, Payload,
       (((CAST(strftime('%s', StoredAt) AS INTEGER) + 62135596800) * 10000) << 11),
       (((CAST(strftime('%s', ExpiresAt) AS INTEGER) + 62135596800) * 10000) << 11)
FROM v;

COMMIT;

-- =====================================================================
--  3. ПРОВЕРОЧНЫЕ ЗАПРОСЫ
-- =====================================================================

-- 3.1 (WHERE, ORDER BY, JOIN) Смартфоны и планшеты в наличии дешевле 50 000 ₽,
--     от дешёвых к дорогим.
SELECT p.Sku, p.Name, c.Name AS Category, p.price_amount AS Price, p.Stock
FROM products p
JOIN categories c ON c.Id = p.CategoryId
WHERE c.Name IN ('Смартфоны', 'Планшеты')
  AND p.IsActive = 1 AND p.Stock > 0
  AND p.price_amount < 50000
ORDER BY p.price_amount;

-- 3.2 (JOIN, WHERE, ORDER BY) Заказы покупателя buyer: статус, состав и сумма строки.
SELECT datetime((o.PlacedAt >> 11) / 10000 - 62135596800, 'unixepoch') AS PlacedAt,
       CASE o.Status
           WHEN 0 THEN 'Оформление'      WHEN 1 THEN 'Ожидает оплаты'
           WHEN 2 THEN 'Подтверждён'     WHEN 3 THEN 'Курьер назначен'
           WHEN 4 THEN 'В доставке'      WHEN 5 THEN 'Завершён'
           ELSE 'Отменён' END AS Status,
       p.Name AS Product, i.Quantity, i.unit_price_amount * i.Quantity AS LineTotal,
       o.total_amount AS OrderTotal
FROM orders o
JOIN users u        ON u.Id = o.UserId
JOIN order_items i  ON i.OrderId = o.Id
JOIN products p     ON p.Id = i.ProductId
WHERE u.Login = 'buyer'
ORDER BY o.PlacedAt DESC, p.Name;

-- 3.3 (JOIN, GROUP BY, HAVING, ORDER BY) Товары со средней оценкой от 4,5.
SELECT p.Name, COUNT(*) AS Reviews, ROUND(AVG(r.Rating), 2) AS AvgRating
FROM reviews r
JOIN products p ON p.Id = r.ProductId
GROUP BY p.Id, p.Name
HAVING AVG(r.Rating) >= 4.5
ORDER BY AvgRating DESC, Reviews DESC;

-- 3.4 (JOIN, WHERE, GROUP BY) Выручка по категориям за октябрь 2026:
--     только оплаченные и не отменённые заказы (статусы 2–5).
SELECT COALESCE(parent.Name, c.Name) AS Section, c.Name AS Category,
       SUM(i.Quantity) AS Items, SUM(i.Quantity * i.unit_price_amount) AS Revenue
FROM orders o
JOIN order_items i     ON i.OrderId = o.Id
JOIN products p        ON p.Id = i.ProductId
JOIN categories c      ON c.Id = p.CategoryId
LEFT JOIN categories parent ON parent.Id = c.ParentId
WHERE o.Status BETWEEN 2 AND 5
  AND o.PlacedAt >= (((CAST(strftime('%s', '2026-10-01 00:00:00') AS INTEGER) + 62135596800) * 10000) << 11)
  AND o.PlacedAt <  (((CAST(strftime('%s', '2026-11-01 00:00:00') AS INTEGER) + 62135596800) * 10000) << 11)
GROUP BY COALESCE(parent.Name, c.Name), c.Name
ORDER BY Revenue DESC;

-- 3.5 (LEFT JOIN, GROUP BY) Загрузка курьеров: доставки по статусам.
SELECT u.Login, u.FirstName || ' ' || u.LastName AS Courier,
       COUNT(d.Id) AS Total,
       SUM(CASE WHEN d.Status IN (0, 1) THEN 1 ELSE 0 END) AS Active,
       SUM(CASE WHEN d.Status = 2 THEN 1 ELSE 0 END) AS Delivered,
       SUM(CASE WHEN d.Status IN (3, 4) THEN 1 ELSE 0 END) AS FailedOrCancelled
FROM users u
JOIN roles r ON r.Kind = u.Role
LEFT JOIN deliveries d ON d.CourierId = u.Id
WHERE r.Name = 'Курьер' AND u.DeletedAt IS NULL AND NOT (u.IsBlocked = 1)
GROUP BY u.Id, u.Login, u.FirstName, u.LastName
ORDER BY Total DESC, u.Login;
