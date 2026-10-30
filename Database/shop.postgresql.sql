-- =====================================================================
--  ИС «Интернет-магазин» (вариант 5) — база данных
--  СУБД: PostgreSQL 13+
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
--  Запуск: createdb shop && psql -d shop -f shop.postgresql.sql
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

-- Типы: Guid → UUID, перечисления → SMALLINT (те же коды, что в C#),
-- bool → BOOLEAN, дата → TIMESTAMPTZ, деньги → NUMERIC(12, 2),
-- строки → VARCHAR с длиной из конфигурации EF Core (HasMaxLength).
-- Имена столбцов без кавычек: PostgreSQL хранит их в нижнем регистре,
-- запросы можно писать как в ER-диаграмме (UserId, PlacedAt ...).

SET client_encoding = 'UTF8';
SET client_min_messages = warning;

BEGIN;

-- =====================================================================
--  1. СОЗДАНИЕ ТАБЛИЦ
-- =====================================================================

DROP TABLE IF EXISTS cache, reviews, deliveries, payments, order_items, orders,
    favorite_items, favorites, cart_items, carts, products, categories,
    confirmation_codes, devices, users, roles CASCADE;

CREATE TABLE roles (
    Id          UUID         NOT NULL,
    Kind        SMALLINT     NOT NULL,
    Name        VARCHAR(64)  NOT NULL,
    Description VARCHAR(512) NULL,
    CONSTRAINT PK_roles PRIMARY KEY (Id),
    CONSTRAINT AK_roles_Kind UNIQUE (Kind),
    CONSTRAINT CK_roles_Kind CHECK (Kind BETWEEN 0 AND 4)
);
COMMENT ON TABLE roles IS 'Роль (справочник). Kind — альтернативный ключ, на него ссылается users.Role';

CREATE TABLE users (
    Id           UUID          NOT NULL,
    Role         SMALLINT      NOT NULL,
    Login        VARCHAR(32)   NOT NULL,
    Email        VARCHAR(254)  NULL,
    Phone        VARCHAR(16)   NULL,
    PasswordHash VARCHAR(1024) NOT NULL,
    FirstName    VARCHAR(64)   NOT NULL,
    LastName     VARCHAR(64)   NULL,
    RegisteredAt TIMESTAMPTZ   NOT NULL,
    IsConfirmed  BOOLEAN       NOT NULL DEFAULT FALSE,
    IsBlocked    BOOLEAN       NOT NULL DEFAULT FALSE,
    BlockReason  VARCHAR(512)  NULL,
    BlockedAt    TIMESTAMPTZ   NULL,
    DeletedAt    TIMESTAMPTZ   NULL,
    CONSTRAINT PK_users PRIMARY KEY (Id),
    CONSTRAINT FK_users_roles_Role FOREIGN KEY (Role) REFERENCES roles (Kind) ON DELETE RESTRICT,
    CONSTRAINT CK_users_Login CHECK (Login ~ '^[a-z0-9_.-]{3,32}$'),
    CONSTRAINT CK_users_Email CHECK (Email ~ '^[^@[:space:]]+@[^@[:space:]]+[.][^@[:space:]]+$'),
    CONSTRAINT CK_users_Phone CHECK (Phone ~ '^[+][0-9]{10,15}$'),
    CONSTRAINT CK_users_Contact CHECK (Email IS NOT NULL OR Phone IS NOT NULL OR DeletedAt IS NOT NULL),
    CONSTRAINT CK_users_Block CHECK (NOT IsBlocked OR BlockReason IS NOT NULL)
);
CREATE UNIQUE INDEX IX_users_Login ON users (Login);
CREATE UNIQUE INDEX IX_users_Email ON users (Email);
CREATE UNIQUE INDEX IX_users_Phone ON users (Phone);
CREATE INDEX IX_users_Role ON users (Role);
COMMENT ON TABLE users IS 'Пользователь: покупатель или сотрудник. Регистрация по email или телефону; удалённый аккаунт обезличен (DeletedAt)';

CREATE TABLE devices (
    Id           UUID          NOT NULL,
    UserId       UUID          NOT NULL,
    Name         VARCHAR(128)  NOT NULL,
    IpAddress    VARCHAR(64)   NULL,
    RefreshToken VARCHAR(1024) NULL,
    LastSignInAt TIMESTAMPTZ   NOT NULL,
    IsActive     BOOLEAN       NOT NULL,
    CONSTRAINT PK_devices PRIMARY KEY (Id),
    CONSTRAINT FK_devices_users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE
);
CREATE INDEX IX_devices_UserId ON devices (UserId);
COMMENT ON TABLE devices IS 'Устройство, с которого входил пользователь';

CREATE TABLE confirmation_codes (
    Id        UUID        NOT NULL,
    UserId    UUID        NOT NULL,
    Code      VARCHAR(32) NOT NULL,
    Purpose   SMALLINT    NOT NULL,
    CreatedAt TIMESTAMPTZ NOT NULL,
    ExpiresAt TIMESTAMPTZ NOT NULL,
    IsUsed    BOOLEAN     NOT NULL,
    CONSTRAINT PK_confirmation_codes PRIMARY KEY (Id),
    CONSTRAINT FK_confirmation_codes_users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE,
    CONSTRAINT CK_confirmation_codes_Purpose CHECK (Purpose IN (0, 1)),
    CONSTRAINT CK_confirmation_codes_Expires CHECK (ExpiresAt > CreatedAt)
);
CREATE INDEX IX_confirmation_codes_UserId ON confirmation_codes (UserId);
COMMENT ON TABLE confirmation_codes IS 'Код подтверждения (регистрация, восстановление пароля), живёт 15 минут';

CREATE TABLE categories (
    Id          UUID          NOT NULL,
    ParentId    UUID          NULL,
    Name        VARCHAR(128)  NOT NULL,
    Description VARCHAR(2048) NULL,
    Photo       VARCHAR(1024) NULL,
    CONSTRAINT PK_categories PRIMARY KEY (Id),
    CONSTRAINT FK_categories_categories_ParentId FOREIGN KEY (ParentId) REFERENCES categories (Id) ON DELETE RESTRICT
);
CREATE INDEX IX_categories_ParentId ON categories (ParentId);
COMMENT ON TABLE categories IS 'Категория каталога; ParentId — родительская категория (дерево)';

CREATE TABLE products (
    Id              UUID          NOT NULL,
    CategoryId      UUID          NOT NULL,
    Sku             VARCHAR(64)   NOT NULL,
    Name            VARCHAR(256)  NOT NULL,
    Description     VARCHAR(4096) NULL,
    Characteristics VARCHAR(4096) NULL,
    price_amount    NUMERIC(12,2) NOT NULL,
    price_currency  CHAR(3)       NOT NULL DEFAULT 'RUB',
    Stock           INTEGER       NOT NULL,
    Photo           VARCHAR(1024) NULL,
    IsActive        BOOLEAN       NOT NULL DEFAULT TRUE,
    CONSTRAINT PK_products PRIMARY KEY (Id),
    CONSTRAINT FK_products_categories_CategoryId FOREIGN KEY (CategoryId) REFERENCES categories (Id) ON DELETE RESTRICT,
    CONSTRAINT CK_products_Price CHECK (price_amount >= 0),
    CONSTRAINT CK_products_Stock CHECK (Stock >= 0)
);
CREATE UNIQUE INDEX IX_products_Sku ON products (Sku);
CREATE INDEX IX_products_Name ON products (Name);
CREATE INDEX IX_products_CategoryId ON products (CategoryId);
COMMENT ON TABLE products IS 'Товар; цена — сумма и валюта';

CREATE TABLE carts (
    Id     UUID NOT NULL,
    UserId UUID NOT NULL,
    CONSTRAINT PK_carts PRIMARY KEY (Id),
    CONSTRAINT FK_carts_users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_carts_UserId ON carts (UserId);
COMMENT ON TABLE carts IS 'Корзина: одна на пользователя';

CREATE TABLE cart_items (
    Id        UUID        NOT NULL,
    CartId    UUID        NOT NULL,
    ProductId UUID        NOT NULL,
    Quantity  INTEGER     NOT NULL,
    AddedAt   TIMESTAMPTZ NOT NULL,
    CONSTRAINT PK_cart_items PRIMARY KEY (Id),
    CONSTRAINT FK_cart_items_carts_CartId FOREIGN KEY (CartId) REFERENCES carts (Id) ON DELETE CASCADE,
    CONSTRAINT FK_cart_items_products_ProductId FOREIGN KEY (ProductId) REFERENCES products (Id) ON DELETE CASCADE,
    CONSTRAINT CK_cart_items_Quantity CHECK (Quantity BETWEEN 1 AND 99)
);
CREATE UNIQUE INDEX IX_cart_items_CartId_ProductId ON cart_items (CartId, ProductId);
CREATE INDEX IX_cart_items_ProductId ON cart_items (ProductId);
COMMENT ON TABLE cart_items IS 'Позиция корзины: товар встречается один раз, количество 1..99';

CREATE TABLE favorites (
    Id     UUID NOT NULL,
    UserId UUID NOT NULL,
    CONSTRAINT PK_favorites PRIMARY KEY (Id),
    CONSTRAINT FK_favorites_users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_favorites_UserId ON favorites (UserId);
COMMENT ON TABLE favorites IS 'Избранное: один список на пользователя';

CREATE TABLE favorite_items (
    Id          UUID        NOT NULL,
    FavoritesId UUID        NOT NULL,
    ProductId   UUID        NOT NULL,
    AddedAt     TIMESTAMPTZ NOT NULL,
    CONSTRAINT PK_favorite_items PRIMARY KEY (Id),
    CONSTRAINT FK_favorite_items_favorites_FavoritesId FOREIGN KEY (FavoritesId) REFERENCES favorites (Id) ON DELETE CASCADE,
    CONSTRAINT FK_favorite_items_products_ProductId FOREIGN KEY (ProductId) REFERENCES products (Id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_favorite_items_FavoritesId_ProductId ON favorite_items (FavoritesId, ProductId);
CREATE INDEX IX_favorite_items_ProductId ON favorite_items (ProductId);
COMMENT ON TABLE favorite_items IS 'Позиция избранного';

CREATE TABLE orders (
    Id                  UUID          NOT NULL,
    UserId              UUID          NOT NULL,
    PlacedAt            TIMESTAMPTZ   NOT NULL,
    Status              SMALLINT      NOT NULL DEFAULT 0,
    DeliveryType        SMALLINT      NULL,
    PaymentMethod       SMALLINT      NULL,
    total_amount        NUMERIC(12,2) NOT NULL,
    total_currency      CHAR(3)       NOT NULL DEFAULT 'RUB',
    address_city        VARCHAR(128)  NULL,
    address_street      VARCHAR(256)  NULL,
    address_house       VARCHAR(32)   NULL,
    address_apartment   VARCHAR(32)   NULL,
    address_postal_code VARCHAR(16)   NULL,
    address_comment     VARCHAR(512)  NULL,
    SubmittedAt         TIMESTAMPTZ   NULL,
    ConfirmedAt         TIMESTAMPTZ   NULL,
    CompletedAt         TIMESTAMPTZ   NULL,
    CancelledAt         TIMESTAMPTZ   NULL,
    CancellationReason  SMALLINT      NULL,
    CONSTRAINT PK_orders PRIMARY KEY (Id),
    CONSTRAINT FK_orders_users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE RESTRICT,
    CONSTRAINT CK_orders_Status CHECK (Status BETWEEN 0 AND 6),
    CONSTRAINT CK_orders_DeliveryType CHECK (DeliveryType BETWEEN 0 AND 3),
    CONSTRAINT CK_orders_PaymentMethod CHECK (PaymentMethod IN (0, 1)),
    CONSTRAINT CK_orders_CancellationReason CHECK (CancellationReason BETWEEN 0 AND 2),
    CONSTRAINT CK_orders_Total CHECK (total_amount >= 0),
    CONSTRAINT CK_orders_Cancelled CHECK ((Status = 6) = (CancelledAt IS NOT NULL))
);
CREATE INDEX IX_orders_UserId ON orders (UserId);
CREATE INDEX IX_orders_Status ON orders (Status);
COMMENT ON TABLE orders IS 'Заказ; адрес доставки — столбцы address_*';

CREATE TABLE order_items (
    Id                  UUID          NOT NULL,
    OrderId             UUID          NOT NULL,
    ProductId           UUID          NOT NULL,
    Quantity            INTEGER       NOT NULL,
    unit_price_amount   NUMERIC(12,2) NOT NULL,
    unit_price_currency CHAR(3)       NOT NULL DEFAULT 'RUB',
    CONSTRAINT PK_order_items PRIMARY KEY (Id),
    CONSTRAINT FK_order_items_orders_OrderId FOREIGN KEY (OrderId) REFERENCES orders (Id) ON DELETE CASCADE,
    CONSTRAINT FK_order_items_products_ProductId FOREIGN KEY (ProductId) REFERENCES products (Id) ON DELETE RESTRICT,
    CONSTRAINT CK_order_items_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_order_items_Price CHECK (unit_price_amount >= 0)
);
CREATE INDEX IX_order_items_OrderId ON order_items (OrderId);
CREATE INDEX IX_order_items_ProductId ON order_items (ProductId);
COMMENT ON TABLE order_items IS 'Позиция заказа; цена за единицу фиксируется на момент оформления';

CREATE TABLE payments (
    Id              UUID          NOT NULL,
    OrderId         UUID          NOT NULL,
    Method          SMALLINT      NOT NULL,
    Status          SMALLINT      NOT NULL,
    amount_amount   NUMERIC(12,2) NOT NULL,
    amount_currency CHAR(3)       NOT NULL DEFAULT 'RUB',
    TransactionId   VARCHAR(128)  NULL,
    PaidAt          TIMESTAMPTZ   NOT NULL,
    FailureReason   VARCHAR(512)  NULL,
    CONSTRAINT PK_payments PRIMARY KEY (Id),
    CONSTRAINT FK_payments_orders_OrderId FOREIGN KEY (OrderId) REFERENCES orders (Id) ON DELETE CASCADE,
    CONSTRAINT CK_payments_Method CHECK (Method IN (0, 1)),
    CONSTRAINT CK_payments_Status CHECK (Status BETWEEN 0 AND 4),
    CONSTRAINT CK_payments_Amount CHECK (amount_amount >= 0)
);
CREATE INDEX IX_payments_OrderId ON payments (OrderId);
COMMENT ON TABLE payments IS 'Платёж по заказу (в том числе отклонённые и возвращённые)';

CREATE TABLE deliveries (
    Id            UUID         NOT NULL,
    CourierId     UUID         NOT NULL,
    Status        SMALLINT     NOT NULL DEFAULT 0,
    AssignedAt    TIMESTAMPTZ  NOT NULL,
    AcceptedAt    TIMESTAMPTZ  NULL,
    DeliveredAt   TIMESTAMPTZ  NULL,
    FailureReason VARCHAR(512) NULL,
    CONSTRAINT PK_deliveries PRIMARY KEY (Id),
    CONSTRAINT FK_deliveries_orders_Id FOREIGN KEY (Id) REFERENCES orders (Id) ON DELETE CASCADE,
    CONSTRAINT FK_deliveries_users_CourierId FOREIGN KEY (CourierId) REFERENCES users (Id) ON DELETE RESTRICT,
    CONSTRAINT CK_deliveries_Status CHECK (Status BETWEEN 0 AND 4)
);
CREATE INDEX IX_deliveries_CourierId ON deliveries (CourierId);
COMMENT ON TABLE deliveries IS 'Доставка: 0..1 на заказ, Id совпадает с Id заказа; CourierId — пользователь-курьер';

CREATE TABLE reviews (
    Id        UUID          NOT NULL,
    UserId    UUID          NOT NULL,
    ProductId UUID          NOT NULL,
    Rating    SMALLINT      NOT NULL,
    Text      VARCHAR(4000) NULL,
    CreatedAt TIMESTAMPTZ   NOT NULL,
    EditedAt  TIMESTAMPTZ   NULL,
    CONSTRAINT PK_reviews PRIMARY KEY (Id),
    CONSTRAINT FK_reviews_users_UserId FOREIGN KEY (UserId) REFERENCES users (Id) ON DELETE CASCADE,
    CONSTRAINT FK_reviews_products_ProductId FOREIGN KEY (ProductId) REFERENCES products (Id) ON DELETE CASCADE,
    CONSTRAINT CK_reviews_Rating CHECK (Rating BETWEEN 1 AND 5)
);
CREATE INDEX IX_reviews_UserId_ProductId ON reviews (UserId, ProductId);
CREATE INDEX IX_reviews_ProductId ON reviews (ProductId);
COMMENT ON TABLE reviews IS 'Отзыв; у пользователя может быть несколько отзывов на один товар';

CREATE TABLE cache (
    Key       VARCHAR(512) NOT NULL,
    Payload   TEXT         NOT NULL,
    StoredAt  TIMESTAMPTZ  NOT NULL,
    ExpiresAt TIMESTAMPTZ  NULL,
    CONSTRAINT PK_cache PRIMARY KEY (Key)
);
COMMENT ON TABLE cache IS 'Служебная таблица: локальный кэш ответов приложения (JSON)';

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
INSERT INTO users (Id, Role, Login, Email, Phone, PasswordHash, FirstName, LastName, RegisteredAt, IsConfirmed, IsBlocked, BlockReason, BlockedAt, DeletedAt) VALUES
    ('00000002-0000-0000-0000-000000000001', 0, 'buyer', 'buyer@nonamebtww.shop', '+79000000001', 'pbkdf2-sha256$100000$/19Z0LNdDZiAbPT6gNi+qQ==$f9zmOmKDxCyiL3ZJNstxlBGLZd8Hhu0GeVanvbGhsPA=', 'Иван', 'Покупателев', '2026-09-01 09:00:00+00', TRUE, FALSE, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000002', 1, 'manager', 'manager@nonamebtww.shop', '+79000000002', 'pbkdf2-sha256$100000$/5Zp2Fy+3rxZ6RiEkkjl6g==$i0AHAYO5EtFRkmU7bT9N/n77HxWHjo1uCJTsNIpUhxA=', 'Мария', 'Менеджерова', '2026-08-15 08:00:00+00', TRUE, FALSE, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000003', 2, 'owner', 'owner@nonamebtww.shop', '+79000000003', 'pbkdf2-sha256$100000$mpsAI5890XgeGBjXLvPdEA==$Ni5yqG5GxDNYvgG58u/A3ACNXeYHt4u1mlGvuaEb2RQ=', 'Олег', 'Владельцев', '2026-08-01 08:00:00+00', TRUE, FALSE, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000004', 3, 'logist', 'logist@nonamebtww.shop', '+79000000004', 'pbkdf2-sha256$100000$55Q+Woi0mftXTaHpMQ747g==$l78XORf9rc2y5sNzIjYWCzvc+vqZ5oEE92nYtNZYmk4=', 'Лидия', 'Логистова', '2026-08-20 08:00:00+00', TRUE, FALSE, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000005', 4, 'courier', 'courier@nonamebtww.shop', '+79000000005', 'pbkdf2-sha256$100000$FfAAXzZ/zF0Dei577CAYeA==$+IHtNVXn8qkVcgu5woeuHooV55cRBXARZbs2iohgYfA=', 'Кирилл', 'Курьеров', '2026-08-20 08:30:00+00', TRUE, FALSE, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000006', 4, 'courier2', 'courier2@nonamebtww.shop', '+79000000006', 'pbkdf2-sha256$100000$hQfC/UlQWcVQcBROC7QM+A==$r53vrbwsnX6O8GQre4WBUxNE2wqUoBRn5mKmEhyfHxQ=', 'Семён', 'Доставкин', '2026-09-05 08:00:00+00', TRUE, FALSE, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000007', 0, 'anna', NULL, '+79161234567', 'pbkdf2-sha256$100000$IjaRkhXl5wcR3Ol92xhE6Q==$40JN3at/G0cRic/vxVsPa2eeNP9iy39Z2JHtLAFRa2A=', 'Анна', 'Смирнова', '2026-09-20 17:45:00+00', TRUE, FALSE, NULL, NULL, NULL),
    ('00000002-0000-0000-0000-000000000008', 0, 'petr', 'petr.ivanov@yandex.ru', NULL, 'pbkdf2-sha256$100000$LKlRjMwbGLFNiNz06Q07ew==$DlrJhKJTb6c/5ox6MiO82WwSCG46wpVdHkhAS/2Cu+c=', 'Пётр', 'Иванов', '2026-09-12 21:10:00+00', TRUE, TRUE, 'Оскорбительные отзывы и спам', '2026-10-25 10:00:00+00', NULL),
    ('00000002-0000-0000-0000-000000000009', 0, 'deleted_000000020000000000000000', NULL, NULL, 'deleted', 'Удалённый', 'пользователь', '2026-09-10 12:00:00+00', TRUE, FALSE, NULL, NULL, '2026-10-20 18:00:00+00');

-- Устройства, с которых входил пользователь (9)
INSERT INTO devices (Id, UserId, Name, IpAddress, RefreshToken, LastSignInAt, IsActive) VALUES
    ('0000000C-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000001', 'Ivan-iPhone (Unix)', '192.168.1.10', 'gYr7I6DKq-9ltxHBbgPUfKK8GaAEfBUShcEBb2AbRR8', '2026-10-30 11:38:00+00', TRUE),
    ('0000000C-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000001', 'Ivan-iPad (Unix)', '192.168.1.11', NULL, '2026-10-12 20:15:00+00', FALSE),
    ('0000000C-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000007', 'Pixel-8 (Unix)', '10.0.0.24', 'UKj62fChNCtV8JfJByYg4k14myz_pXK8cUNvtpQcJvc', '2026-10-30 11:48:00+00', TRUE),
    ('0000000C-0000-0000-0000-000000000004', '00000002-0000-0000-0000-000000000002', 'Maria-iPhone (Unix)', '192.168.0.31', 'obuIuZ37cMz2jhULxfH5JGCPuhzF7eGshbXd8dTAC7I', '2026-10-30 09:05:00+00', TRUE),
    ('0000000C-0000-0000-0000-000000000005', '00000002-0000-0000-0000-000000000003', 'Oleg-MacBook-Pro (Unix)', '192.168.0.2', 'xe4oIlz-dvEXOFbyXyJcnUWHGzYLaZoSfTclBUm5bg4', '2026-10-29 19:30:00+00', TRUE),
    ('0000000C-0000-0000-0000-000000000006', '00000002-0000-0000-0000-000000000004', 'Galaxy-S23 (Unix)', '192.168.0.44', 'uwwWY3UVmIeiW1DvfEw0Bqikfi3cTiVV8VkmYSEMyyE', '2026-10-30 08:20:00+00', TRUE),
    ('0000000C-0000-0000-0000-000000000007', '00000002-0000-0000-0000-000000000005', 'Redmi-Note-12 (Unix)', '100.64.12.7', '7-vchDa3ebfRD5yx4ctPRhCFkdtFfJNByntY1dk0zsM', '2026-10-30 08:25:00+00', TRUE),
    ('0000000C-0000-0000-0000-000000000008', '00000002-0000-0000-0000-000000000006', 'Semyon-iPhone (Unix)', '100.64.12.9', 'piRqoYMPxCkMqbI73j90Qp1rydRFO2BXrewGxtpCo38', '2026-10-30 09:35:00+00', TRUE),
    ('0000000C-0000-0000-0000-000000000009', '00000002-0000-0000-0000-000000000008', 'Galaxy-A54 (Unix)', '85.143.10.5', NULL, '2026-10-24 22:00:00+00', FALSE);

-- Коды подтверждения (регистрация, восстановление пароля) (6)
INSERT INTO confirmation_codes (Id, UserId, Code, Purpose, CreatedAt, ExpiresAt, IsUsed) VALUES
    ('0000000D-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000001', '482913', 0, '2026-09-01 09:00:00+00', '2026-09-01 09:15:00+00', TRUE),
    ('0000000D-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000007', '305771', 0, '2026-09-20 17:45:00+00', '2026-09-20 18:00:00+00', TRUE),
    ('0000000D-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000008', '918204', 0, '2026-09-12 21:10:00+00', '2026-09-12 21:25:00+00', TRUE),
    ('0000000D-0000-0000-0000-000000000004', '00000002-0000-0000-0000-000000000001', '660145', 1, '2026-10-12 20:05:00+00', '2026-10-12 20:20:00+00', TRUE),
    ('0000000D-0000-0000-0000-000000000005', '00000002-0000-0000-0000-000000000007', '127530', 1, '2026-10-29 21:00:00+00', '2026-10-29 21:15:00+00', FALSE),
    ('0000000D-0000-0000-0000-000000000006', '00000002-0000-0000-0000-000000000009', '774019', 0, '2026-09-10 12:00:00+00', '2026-09-10 12:15:00+00', TRUE);

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
    ('00000004-0000-0000-0000-000000000001', '00000003-0000-0000-0000-000000000002', 'SM-IP13P', 'Apple iPhone 13 Pro 128 ГБ', 'Флагман Apple с дисплеем ProMotion 120 Гц, тройной камерой 12 Мп с телеобъективом и процессором A15 Bionic.', E'Экран: 6,1" Super Retina XDR, 120 Гц\nПроцессор: Apple A15 Bionic\nПамять: 128 ГБ\nКамера: 3 × 12 Мп, оптический зум 3×\nАккумулятор: 3095 мА·ч, MagSafe\nЗащита: IP68\nВес: 204 г', 79990.00, 'RUB', 14, 'https://cdn.dummyjson.com/product-images/smartphones/iphone-13-pro/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000002', '00000003-0000-0000-0000-000000000002', 'SM-IPX', 'Apple iPhone X 64 ГБ', 'Первый iPhone с OLED-экраном во всю переднюю панель и Face ID. Подходит тем, кто хочет iPhone по доступной цене.', E'Экран: 5,8" OLED Super Retina\nПроцессор: Apple A11 Bionic\nПамять: 64 ГБ\nКамера: 2 × 12 Мп\nРазблокировка: Face ID\nЗащита: IP67\nВес: 174 г', 24990.00, 'RUB', 6, 'https://cdn.dummyjson.com/product-images/smartphones/iphone-x/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000003', '00000003-0000-0000-0000-000000000002', 'SM-GS10', 'Samsung Galaxy S10 8/128 ГБ', 'Смартфон с изогнутым Dynamic AMOLED-экраном, ультразвуковым сканером отпечатков в экране и тройной камерой.', E'Экран: 6,1" Dynamic AMOLED, 3040×1440\nПроцессор: Exynos 9820\nОЗУ: 8 ГБ\nПамять: 128 ГБ, microSD\nКамера: 12 + 12 + 16 Мп\nАккумулятор: 3400 мА·ч, беспроводная зарядка\nЗащита: IP68', 29990.00, 'RUB', 9, 'https://cdn.dummyjson.com/product-images/smartphones/samsung-galaxy-s10/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000004', '00000003-0000-0000-0000-000000000002', 'SM-GS8', 'Samsung Galaxy S8 4/64 ГБ', 'Компактный смартфон с безрамочным Infinity-экраном, защитой от воды и быстрой зарядкой.', E'Экран: 5,8" Super AMOLED, 2960×1440\nПроцессор: Exynos 8895\nОЗУ: 4 ГБ\nПамять: 64 ГБ, microSD\nКамера: 12 Мп, f/1.7\nАккумулятор: 3000 мА·ч\nЗащита: IP68', 15990.00, 'RUB', 4, 'https://cdn.dummyjson.com/product-images/smartphones/samsung-galaxy-s8/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000005', '00000003-0000-0000-0000-000000000002', 'SM-OF19', 'OPPO F19 Pro+ 5G 8/128 ГБ', 'Тонкий смартфон с поддержкой 5G, AMOLED-экраном и зарядкой 50 Вт: от 0 до 100% примерно за 48 минут.', E'Экран: 6,43" AMOLED, 2400×1080\nПроцессор: MediaTek Dimensity 800U\nОЗУ: 8 ГБ\nПамять: 128 ГБ\nКамера: 48 + 8 + 2 + 2 Мп\nАккумулятор: 4310 мА·ч, 50 Вт\nСеть: 5G', 23990.00, 'RUB', 18, 'https://cdn.dummyjson.com/product-images/smartphones/oppo-f19-pro-plus/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000006', '00000003-0000-0000-0000-000000000002', 'SM-RMXT', 'realme XT 8/128 ГБ', 'Смартфон с основной камерой 64 Мп и экраном Super AMOLED — отличный выбор для любителей мобильной фотографии.', E'Экран: 6,4" Super AMOLED\nПроцессор: Snapdragon 712\nОЗУ: 8 ГБ\nПамять: 128 ГБ\nКамера: 64 + 8 + 2 + 2 Мп\nАккумулятор: 4000 мА·ч, 20 Вт\nСканер отпечатков: в экране', 17990.00, 'RUB', 22, 'https://cdn.dummyjson.com/product-images/smartphones/realme-xt/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000007', '00000003-0000-0000-0000-000000000002', 'SM-VX21', 'vivo X21 6/128 ГБ', 'Один из первых смартфонов со сканером отпечатков пальцев в экране и двойной камерой с AI-обработкой.', E'Экран: 6,28" Super AMOLED\nПроцессор: Snapdragon 660\nОЗУ: 6 ГБ\nПамять: 128 ГБ\nКамера: 12 + 5 Мп\nФронтальная камера: 12 Мп\nАккумулятор: 3200 мА·ч', 14990.00, 'RUB', 0, 'https://cdn.dummyjson.com/product-images/smartphones/vivo-x21/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000008', '00000003-0000-0000-0000-000000000003', 'TB-IPMINI', 'Apple iPad mini (2021) Wi-Fi 64 ГБ, сияющая звезда', 'Компактный планшет с экраном Liquid Retina 8,3", процессором A15 Bionic, USB-C и поддержкой Apple Pencil 2.', E'Экран: 8,3" Liquid Retina, 2266×1488\nПроцессор: Apple A15 Bionic\nПамять: 64 ГБ\nКамера: 12 Мп, фронтальная 12 Мп Center Stage\nРазъём: USB-C\nСтилус: Apple Pencil (2-го поколения)\nВес: 293 г', 49990.00, 'RUB', 7, 'https://cdn.dummyjson.com/product-images/tablets/ipad-mini-2021-starlight/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000009', '00000003-0000-0000-0000-000000000003', 'TB-GTS8P', 'Samsung Galaxy Tab S8+ 8/128 ГБ, графит', 'Большой Super AMOLED-экран 12,4", стилус S Pen в комплекте и режим DeX для работы как на компьютере.', E'Экран: 12,4" Super AMOLED, 120 Гц\nПроцессор: Snapdragon 8 Gen 1\nОЗУ: 8 ГБ\nПамять: 128 ГБ, microSD\nАккумулятор: 10090 мА·ч, 45 Вт\nСтилус: S Pen в комплекте\nВес: 567 г', 69990.00, 'RUB', 5, 'https://cdn.dummyjson.com/product-images/tablets/samsung-galaxy-tab-s8-plus-grey/1.webp', TRUE),
    ('00000004-0000-0000-0000-00000000000A', '00000003-0000-0000-0000-000000000004', 'NB-MBP14', 'Apple MacBook Pro 14" M1 Pro 16/512 ГБ, серый космос', 'Профессиональный ноутбук с экраном Liquid Retina XDR, чипом M1 Pro и автономностью до 17 часов.', E'Экран: 14,2" Liquid Retina XDR, 120 Гц\nПроцессор: Apple M1 Pro, 8 CPU / 14 GPU\nОЗУ: 16 ГБ\nSSD: 512 ГБ\nПорты: 3 × Thunderbolt 4, HDMI, SDXC, MagSafe 3\nАвтономность: до 17 ч\nВес: 1,6 кг', 189990.00, 'RUB', 3, 'https://cdn.dummyjson.com/product-images/laptops/apple-macbook-pro-14-inch-space-grey/1.webp', TRUE),
    ('00000004-0000-0000-0000-00000000000B', '00000003-0000-0000-0000-000000000004', 'NB-ZENDUO', 'ASUS ZenBook Pro Duo UX581', 'Ноутбук с двумя экранами: основной 4K OLED и сенсорный ScreenPad Plus над клавиатурой для монтажа и многозадачности.', E'Экран: 15,6" 4K OLED, сенсорный\nДоп. экран: ScreenPad Plus 14"\nПроцессор: Intel Core i7-10750H\nОЗУ: 16 ГБ\nSSD: 1 ТБ\nВидеокарта: NVIDIA GeForce RTX 2060\nВес: 2,5 кг', 159990.00, 'RUB', 2, 'https://cdn.dummyjson.com/product-images/laptops/asus-zenbook-pro-dual-screen-laptop/1.webp', TRUE),
    ('00000004-0000-0000-0000-00000000000C', '00000003-0000-0000-0000-000000000004', 'NB-MATEX', 'HUAWEI MateBook X Pro 16/1024 ГБ', 'Тонкий металлический ноутбук с сенсорным 3K-экраном почти без рамок и быстрой зарядкой USB-C.', E'Экран: 13,9" 3K LTPS, сенсорный\nПроцессор: Intel Core i7\nОЗУ: 16 ГБ\nSSD: 1 ТБ\nЗвук: 4 динамика\nЗарядка: USB-C, 65 Вт\nВес: 1,33 кг', 129990.00, 'RUB', 6, 'https://cdn.dummyjson.com/product-images/laptops/huawei-matebook-x-pro/1.webp', TRUE),
    ('00000004-0000-0000-0000-00000000000D', '00000003-0000-0000-0000-000000000004', 'NB-YOGA920', 'Lenovo Yoga 920 13,9"', 'Ноутбук-трансформер с поворотным на 360° экраном, поддержкой стилуса и корпусом из алюминия.', E'Экран: 13,9" Full HD IPS, сенсорный\nПроцессор: Intel Core i7-8550U\nОЗУ: 8 ГБ\nSSD: 256 ГБ\nКорпус: трансформер 360°\nПорты: 2 × Thunderbolt 3, USB-A\nВес: 1,37 кг', 79990.00, 'RUB', 4, 'https://cdn.dummyjson.com/product-images/laptops/lenovo-yoga-920/1.webp', TRUE),
    ('00000004-0000-0000-0000-00000000000E', '00000003-0000-0000-0000-000000000004', 'NB-XPS13', 'Dell XPS 13 9300', 'Компактный ноутбук с экраном InfinityEdge 16:10 — 13,4 дюйма в корпусе размером с 11-дюймовый.', E'Экран: 13,4" FHD+ InfinityEdge, 16:10\nПроцессор: Intel Core i7-1065G7\nОЗУ: 16 ГБ LPDDR4x\nSSD: 512 ГБ\nПорты: 2 × Thunderbolt 3\nАвтономность: до 14 ч\nВес: 1,2 кг', 109990.00, 'RUB', 5, 'https://cdn.dummyjson.com/product-images/laptops/new-dell-xps-13-9300-laptop/1.webp', TRUE),
    ('00000004-0000-0000-0000-00000000000F', '00000003-0000-0000-0000-000000000005', 'AU-APODS', 'Apple AirPods (2-го поколения)', 'Беспроводные наушники с автоматическим подключением к устройствам Apple и голосовым вызовом Siri.', E'Тип: вкладыши, TWS\nЧип: Apple H1\nРабота: до 5 ч, с кейсом до 24 ч\nКейс: зарядный, Lightning\nМикрофоны: 2, с подавлением шума\nУправление: двойное касание', 12990.00, 'RUB', 30, 'https://cdn.dummyjson.com/product-images/mobile-accessories/apple-airpods/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000010', '00000003-0000-0000-0000-000000000005', 'AU-APMAX', 'Apple AirPods Max, серебристый', 'Полноразмерные наушники с активным шумоподавлением, режимом прозрачности и пространственным звуком.', E'Тип: полноразмерные, закрытые\nЧип: Apple H1 в каждом наушнике\nШумоподавление: активное\nПространственный звук: да, с отслеживанием головы\nРабота: до 20 ч\nВес: 385 г', 54990.00, 'RUB', 8, 'https://cdn.dummyjson.com/product-images/mobile-accessories/apple-airpods-max-silver/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000011', '00000003-0000-0000-0000-000000000005', 'AU-BFLEX', 'Beats Flex', 'Беспроводные наушники на шнурке с магнитными амбушюрами: при соединении музыка ставится на паузу.', E'Тип: внутриканальные, с шейным шнурком\nЧип: Apple W1\nРабота: до 12 ч\nЗарядка: USB-C, Fast Fuel\nМикрофон: да\nАмбушюры: 4 размера в комплекте', 5990.00, 'RUB', 45, 'https://cdn.dummyjson.com/product-images/mobile-accessories/beats-flex-wireless-earphones/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000012', '00000003-0000-0000-0000-000000000005', 'AU-HPMINI', 'Apple HomePod mini, серый космос', 'Компактная умная колонка с объёмным звуком на 360°, Siri и управлением умным домом.', E'Звук: 360°, вычислительная обработка\nАссистент: Siri\nУмный дом: Apple Home, Thread\nПодключение: Wi-Fi, Bluetooth 5.0\nПитание: USB-C, 20 Вт\nВысота: 84 мм', 11990.00, 'RUB', 12, 'https://cdn.dummyjson.com/product-images/mobile-accessories/apple-homepod-mini-cosmic-grey/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000013', '00000003-0000-0000-0000-000000000005', 'AU-ECHOP', 'Amazon Echo Plus (2-го поколения)', 'Умная колонка с голосовым ассистентом Alexa, встроенным хабом Zigbee и датчиком температуры.', E'Ассистент: Amazon Alexa\nДинамики: 3" вуфер + 0,8" твитер\nУмный дом: хаб Zigbee\nДатчики: температуры\nПодключение: Wi-Fi, Bluetooth\nВыход: 3,5 мм', 8990.00, 'RUB', 0, 'https://cdn.dummyjson.com/product-images/mobile-accessories/amazon-echo-plus/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000014', '00000003-0000-0000-0000-000000000006', 'WR-AW4G', 'Apple Watch Series 4 GPS 40 мм, золотой алюминий', 'Смарт-часы с ЭКГ, распознаванием падений и экраном, который на 30% больше, чем у предыдущей серии.', E'Корпус: 40 мм, алюминий\nЭкран: LTPO OLED Retina\nДатчики: ЭКГ, пульс, акселерометр\nФункции: распознавание падений\nЗащита: водонепроницаемость 50 м\nРабота: до 18 ч', 17990.00, 'RUB', 10, 'https://cdn.dummyjson.com/product-images/mobile-accessories/apple-watch-series-4-gold/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000015', '00000003-0000-0000-0000-000000000008', 'KT-BLEND', 'Стационарный блендер 1,5 л', 'Мощный блендер для смузи, коктейлей, супов-пюре и колки льда; чаша из прочного стекла.', E'Мощность: 1000 Вт\nЧаша: 1,5 л, стекло\nСкоростей: 5 + импульсный режим\nНожи: 6 лезвий, нержавеющая сталь\nФункции: колка льда\nЗащита: блокировка без крышки', 6490.00, 'RUB', 15, 'https://cdn.dummyjson.com/product-images/kitchen-accessories/boxed-blender/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000016', '00000003-0000-0000-0000-000000000008', 'KT-HBLEND', 'Погружной блендер с венчиком', 'Компактный погружной блендер с насадкой-венчиком для взбивания и измельчения.', E'Мощность: 800 Вт\nСкоростей: 2 + турбо\nНасадки: блендерная, венчик\nПогружная часть: нержавеющая сталь\nДлина шнура: 1,2 м\nВес: 0,9 кг', 3290.00, 'RUB', 25, 'https://cdn.dummyjson.com/product-images/kitchen-accessories/hand-blender/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000017', '00000003-0000-0000-0000-000000000008', 'KT-MICRO', 'Микроволновая печь 20 л', 'Микроволновка с электронным управлением, грилем и автоматическими программами разморозки.', E'Объём: 20 л\nМощность СВЧ: 700 Вт\nГриль: 1000 Вт\nУправление: электронное\nАвтопрограмм: 8\nПокрытие камеры: эмаль', 8990.00, 'RUB', 9, 'https://cdn.dummyjson.com/product-images/kitchen-accessories/microwave-oven/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000018', '00000003-0000-0000-0000-000000000008', 'KT-STOVE', 'Электроплитка настольная', 'Настольная плитка с чугунной конфоркой — для дачи, общежития или как дополнительная конфорка.', E'Конфорок: 1\nМощность: 1500 Вт\nТип конфорки: чугунная\nРегулировка: плавная, 5 режимов\nКорпус: металл\nДиаметр конфорки: 18,5 см', 2490.00, 'RUB', 20, 'https://cdn.dummyjson.com/product-images/kitchen-accessories/electric-stove/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000019', '00000003-0000-0000-0000-000000000008', 'KT-WOK', 'Сковорода-вок из углеродистой стали 30 см', 'Классический вок для быстрой обжарки на сильном огне; подходит для газовых и индукционных плит.', E'Диаметр: 30 см\nМатериал: углеродистая сталь\nТолщина стенок: 1,5 мм\nПлиты: газ, электро, индукция\nРучка: дерево\nВес: 1,4 кг', 2190.00, 'RUB', 0, 'https://cdn.dummyjson.com/product-images/kitchen-accessories/carbon-steel-wok/1.webp', TRUE),
    ('00000004-0000-0000-0000-00000000001A', '00000003-0000-0000-0000-000000000008', 'KT-POT', 'Кастрюля 3 л со стеклянной крышкой', 'Кастрюля из нержавеющей стали с капсульным дном для равномерного нагрева.', E'Объём: 3 л\nМатериал: нержавеющая сталь 18/10\nДно: капсульное\nКрышка: жаропрочное стекло\nПлиты: все, включая индукцию\nПосудомойка: можно', 2790.00, 'RUB', 18, 'https://cdn.dummyjson.com/product-images/kitchen-accessories/silver-pot-with-glass-cap/1.webp', TRUE),
    ('00000004-0000-0000-0000-00000000001B', '00000003-0000-0000-0000-000000000009', 'DC-LAMP', 'Настольная лампа с тканевым абажуром', 'Лампа в классическом стиле: керамическое основание и абажур из ткани рассеивают мягкий тёплый свет.', E'Цоколь: E27\nМакс. мощность лампы: 40 Вт\nВысота: 45 см\nОснование: керамика\nАбажур: ткань\nВыключатель: на проводе', 3490.00, 'RUB', 11, 'https://cdn.dummyjson.com/product-images/home-decoration/table-lamp/1.webp', TRUE),
    ('00000004-0000-0000-0000-00000000001C', '00000003-0000-0000-0000-000000000009', 'DC-POT', 'Кашпо керамическое с растением', 'Керамическое кашпо с декоративным растением — живой акцент для рабочего стола или полки.', E'Материал: керамика\nДиаметр: 14 см\nВысота: 16 см\nДренаж: отверстие и поддон\nЦвет: белый\nРастение: в комплекте', 1590.00, 'RUB', 14, 'https://cdn.dummyjson.com/product-images/home-decoration/plant-pot/1.webp', TRUE),
    ('00000004-0000-0000-0000-00000000001D', '00000003-0000-0000-0000-00000000000A', 'AC-USBC20', 'Адаптер питания Apple USB-C 20 Вт', 'Оригинальный адаптер для быстрой зарядки iPhone: до 50% заряда примерно за 30 минут.', E'Мощность: 20 Вт\nРазъём: USB-C\nБыстрая зарядка: USB Power Delivery\nСовместимость: iPhone, iPad, AirPods\nВилка: евро\nКабель: не входит', 2490.00, 'RUB', 60, 'https://cdn.dummyjson.com/product-images/mobile-accessories/apple-iphone-charger/1.webp', TRUE),
    ('00000004-0000-0000-0000-00000000001E', '00000003-0000-0000-0000-00000000000A', 'AC-MSBAT', 'Apple MagSafe Battery Pack', 'Внешний аккумулятор, который магнитом крепится к iPhone и заряжает его на ходу.', E'Крепление: MagSafe\nЁмкость: 1460 мА·ч\nЗарядка iPhone: до 7,5 Вт\nВход: Lightning, до 20 Вт\nСовместимость: iPhone 12 и новее\nВес: 115 г', 9990.00, 'RUB', 13, 'https://cdn.dummyjson.com/product-images/mobile-accessories/apple-magsafe-battery-pack/1.webp', TRUE),
    ('00000004-0000-0000-0000-00000000001F', '00000003-0000-0000-0000-00000000000A', 'AC-CASE12', 'Силиконовый чехол MagSafe для iPhone 12, сливовый', 'Оригинальный чехол с мягкой подкладкой из микрофибры и встроенными магнитами MagSafe.', E'Совместимость: iPhone 12 / 12 Pro\nМатериал: силикон, микрофибра\nКрепление: MagSafe\nЦвет: сливовый\nЗащита: бортик над экраном и камерой\nБеспроводная зарядка: да', 3990.00, 'RUB', 35, 'https://cdn.dummyjson.com/product-images/mobile-accessories/iphone-12-silicone-case-with-magsafe-plum/1.webp', TRUE),
    ('00000004-0000-0000-0000-000000000020', '00000003-0000-0000-0000-00000000000A', 'AC-SELFIE', 'Селфи-палка монопод с Bluetooth-пультом', 'Складной монопод с беспроводным пультом и держателем для смартфонов шириной до 8,5 см.', E'Длина: 20–100 см\nПульт: Bluetooth 5.0\nДержатель: смартфоны 5,5–8,5 см\nВращение: 360°\nМатериал: алюминий\nВес: 160 г', 1290.00, 'RUB', 40, 'https://cdn.dummyjson.com/product-images/mobile-accessories/selfie-stick-monopod/1.webp', TRUE);

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
INSERT INTO cart_items (Id, CartId, ProductId, Quantity, AddedAt) VALUES
    ('00000006-0000-0000-0000-000000000001', '00000005-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000003', 1, '2026-10-29 20:10:00+00'),
    ('00000006-0000-0000-0000-000000000002', '00000005-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000011', 2, '2026-10-29 20:12:00+00'),
    ('00000006-0000-0000-0000-000000000003', '00000005-0000-0000-0000-000000000007', '00000004-0000-0000-0000-000000000014', 1, '2026-10-28 13:00:00+00'),
    ('00000006-0000-0000-0000-000000000004', '00000005-0000-0000-0000-000000000007', '00000004-0000-0000-0000-00000000001E', 1, '2026-10-28 13:02:00+00'),
    ('00000006-0000-0000-0000-000000000005', '00000005-0000-0000-0000-000000000002', '00000004-0000-0000-0000-000000000017', 1, '2026-10-27 12:30:00+00'),
    ('00000006-0000-0000-0000-000000000006', '00000005-0000-0000-0000-000000000008', '00000004-0000-0000-0000-00000000000F', 1, '2026-10-24 21:40:00+00'),
    ('00000006-0000-0000-0000-000000000007', '00000005-0000-0000-0000-000000000003', '00000004-0000-0000-0000-00000000000E', 1, '2026-10-26 10:00:00+00');

-- Избранное: по одному списку на пользователя (5)
INSERT INTO favorites (Id, UserId) VALUES
    ('00000007-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000001'),
    ('00000007-0000-0000-0000-000000000007', '00000002-0000-0000-0000-000000000007'),
    ('00000007-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000002'),
    ('00000007-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000003'),
    ('00000007-0000-0000-0000-000000000005', '00000002-0000-0000-0000-000000000005');

-- Позиции избранного (7)
INSERT INTO favorite_items (Id, FavoritesId, ProductId, AddedAt) VALUES
    ('00000008-0000-0000-0000-000000000001', '00000007-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000010', '2026-10-05 19:00:00+00'),
    ('00000008-0000-0000-0000-000000000002', '00000007-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000008', '2026-10-05 19:01:00+00'),
    ('00000008-0000-0000-0000-000000000003', '00000007-0000-0000-0000-000000000007', '00000004-0000-0000-0000-000000000001', '2026-10-21 08:15:00+00'),
    ('00000008-0000-0000-0000-000000000004', '00000007-0000-0000-0000-000000000007', '00000004-0000-0000-0000-000000000012', '2026-10-21 08:16:00+00'),
    ('00000008-0000-0000-0000-000000000005', '00000007-0000-0000-0000-000000000002', '00000004-0000-0000-0000-00000000000C', '2026-10-18 13:20:00+00'),
    ('00000008-0000-0000-0000-000000000006', '00000007-0000-0000-0000-000000000003', '00000004-0000-0000-0000-000000000009', '2026-10-11 09:00:00+00'),
    ('00000008-0000-0000-0000-000000000007', '00000007-0000-0000-0000-000000000005', '00000004-0000-0000-0000-000000000020', '2026-10-19 18:40:00+00');

-- Заказы во всех статусах (10)
INSERT INTO orders (Id, UserId, PlacedAt, Status, DeliveryType, PaymentMethod, total_amount, total_currency, address_city, address_street, address_house, address_apartment, address_postal_code, address_comment, SubmittedAt, ConfirmedAt, CompletedAt, CancelledAt, CancellationReason) VALUES
    ('00000009-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000001', '2026-10-02 10:15:00+00', 5, 0, 0, 82480.00, 'RUB', 'Москва', 'ул. Тверская', '7', '15', '125009', 'Домофон 15, третий этаж', '2026-10-02 10:20:00+00', '2026-10-02 10:21:00+00', '2026-10-03 12:10:00+00', NULL, NULL),
    ('00000009-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000001', '2026-10-29 18:00:00+00', 4, 1, 1, 189990.00, 'RUB', 'Москва', 'ул. Тверская', '7', '15', '125009', 'Домофон 15, третий этаж', '2026-10-29 18:05:00+00', '2026-10-29 18:06:00+00', NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000007', '2026-10-29 12:00:00+00', 3, 3, 0, 62970.00, 'RUB', 'Москва', 'ул. Профсоюзная', '56', NULL, '117393', 'Постамат у входа в ТЦ', '2026-10-29 12:04:00+00', '2026-10-29 12:05:00+00', NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000004', '00000002-0000-0000-0000-000000000007', '2026-10-25 15:00:00+00', 2, 0, 0, 9280.00, 'RUB', 'Москва', 'Ленинский проспект', '32', '48', '119334', NULL, '2026-10-25 15:03:00+00', '2026-10-25 15:04:00+00', NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000005', '00000002-0000-0000-0000-000000000001', '2026-10-30 11:40:00+00', 1, 2, 0, 49990.00, 'RUB', 'Москва', 'ул. Новый Арбат', '15', NULL, '119019', 'Пункт самовывоза магазина', '2026-10-30 11:45:00+00', NULL, NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000006', '00000002-0000-0000-0000-000000000002', '2026-10-20 09:00:00+00', 6, 0, 1, 6980.00, 'RUB', 'Москва', 'ул. Земляной Вал', '9', '112', '105064', NULL, '2026-10-20 09:02:00+00', '2026-10-20 09:03:00+00', NULL, '2026-10-20 16:30:00+00', 1),
    ('00000009-0000-0000-0000-000000000007', '00000002-0000-0000-0000-000000000008', '2026-10-22 20:00:00+00', 6, 0, 0, 7280.00, 'RUB', 'Подольск', 'ул. Кирова', '11', '5', '142110', NULL, '2026-10-22 20:02:00+00', NULL, NULL, '2026-10-22 20:32:00+00', 2),
    ('00000009-0000-0000-0000-000000000008', '00000002-0000-0000-0000-000000000007', '2026-10-30 11:50:00+00', 0, 0, NULL, 1590.00, 'RUB', 'Москва', 'Ленинский проспект', '32', '48', '119334', NULL, NULL, NULL, NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000009', '00000002-0000-0000-0000-000000000003', '2026-10-10 12:00:00+00', 5, 2, 1, 11480.00, 'RUB', 'Москва', 'ул. Новый Арбат', '15', NULL, '119019', 'Пункт самовывоза магазина', '2026-10-10 12:01:00+00', '2026-10-10 12:02:00+00', '2026-10-11 13:00:00+00', NULL, NULL),
    ('00000009-0000-0000-0000-00000000000A', '00000002-0000-0000-0000-000000000001', '2026-10-15 19:00:00+00', 6, NULL, NULL, 15990.00, 'RUB', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, '2026-10-15 19:10:00+00', 0);

-- Позиции заказа (цена фиксируется на момент заказа) (15)
INSERT INTO order_items (Id, OrderId, ProductId, Quantity, unit_price_amount, unit_price_currency) VALUES
    ('0000000A-0000-0000-0000-000000000001', '00000009-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000001', 1, 79990.00, 'RUB'),
    ('0000000A-0000-0000-0000-000000000002', '00000009-0000-0000-0000-000000000001', '00000004-0000-0000-0000-00000000001D', 1, 2490.00, 'RUB'),
    ('0000000A-0000-0000-0000-000000000003', '00000009-0000-0000-0000-000000000002', '00000004-0000-0000-0000-00000000000A', 1, 189990.00, 'RUB'),
    ('0000000A-0000-0000-0000-000000000004', '00000009-0000-0000-0000-000000000003', '00000004-0000-0000-0000-000000000010', 1, 54990.00, 'RUB'),
    ('0000000A-0000-0000-0000-000000000005', '00000009-0000-0000-0000-000000000003', '00000004-0000-0000-0000-00000000001F', 2, 3990.00, 'RUB'),
    ('0000000A-0000-0000-0000-000000000006', '00000009-0000-0000-0000-000000000004', '00000004-0000-0000-0000-000000000015', 1, 6490.00, 'RUB'),
    ('0000000A-0000-0000-0000-000000000007', '00000009-0000-0000-0000-000000000004', '00000004-0000-0000-0000-00000000001A', 1, 2790.00, 'RUB'),
    ('0000000A-0000-0000-0000-000000000008', '00000009-0000-0000-0000-000000000005', '00000004-0000-0000-0000-000000000008', 1, 49990.00, 'RUB'),
    ('0000000A-0000-0000-0000-000000000009', '00000009-0000-0000-0000-000000000006', '00000004-0000-0000-0000-00000000001B', 2, 3490.00, 'RUB'),
    ('0000000A-0000-0000-0000-00000000000A', '00000009-0000-0000-0000-000000000007', '00000004-0000-0000-0000-000000000020', 1, 1290.00, 'RUB'),
    ('0000000A-0000-0000-0000-00000000000B', '00000009-0000-0000-0000-000000000007', '00000004-0000-0000-0000-000000000011', 1, 5990.00, 'RUB'),
    ('0000000A-0000-0000-0000-00000000000C', '00000009-0000-0000-0000-000000000008', '00000004-0000-0000-0000-00000000001C', 1, 1590.00, 'RUB'),
    ('0000000A-0000-0000-0000-00000000000D', '00000009-0000-0000-0000-000000000009', '00000004-0000-0000-0000-000000000017', 1, 8990.00, 'RUB'),
    ('0000000A-0000-0000-0000-00000000000E', '00000009-0000-0000-0000-000000000009', '00000004-0000-0000-0000-000000000018', 1, 2490.00, 'RUB'),
    ('0000000A-0000-0000-0000-00000000000F', '00000009-0000-0000-0000-00000000000A', '00000004-0000-0000-0000-000000000004', 1, 15990.00, 'RUB');

-- Платежи (7)
INSERT INTO payments (Id, OrderId, Method, Status, amount_amount, amount_currency, TransactionId, PaidAt, FailureReason) VALUES
    ('0000000B-0000-0000-0000-000000000001', '00000009-0000-0000-0000-000000000001', 0, 1, 82480.00, 'RUB', 'FAKE-FFEC4F8B898E001B', '2026-10-02 10:21:00+00', NULL),
    ('0000000B-0000-0000-0000-000000000002', '00000009-0000-0000-0000-000000000002', 1, 1, 189990.00, 'RUB', 'FAKE-D8DAAF40C5F782B0', '2026-10-29 18:06:00+00', NULL),
    ('0000000B-0000-0000-0000-000000000003', '00000009-0000-0000-0000-000000000003', 0, 1, 62970.00, 'RUB', 'FAKE-69630178DD2D8544', '2026-10-29 12:05:00+00', NULL),
    ('0000000B-0000-0000-0000-000000000004', '00000009-0000-0000-0000-000000000004', 0, 1, 9280.00, 'RUB', 'FAKE-AA4DE1D4AEDF7011', '2026-10-25 15:04:00+00', NULL),
    ('0000000B-0000-0000-0000-000000000005', '00000009-0000-0000-0000-000000000005', 0, 2, 49990.00, 'RUB', 'FAKE-72AF42081A1A1565', '2026-10-30 11:46:00+00', 'Недостаточно средств на карте'),
    ('0000000B-0000-0000-0000-000000000006', '00000009-0000-0000-0000-000000000006', 1, 4, 6980.00, 'RUB', 'FAKE-1A1E4BFDDA656245', '2026-10-20 09:03:00+00', NULL),
    ('0000000B-0000-0000-0000-000000000007', '00000009-0000-0000-0000-000000000009', 1, 1, 11480.00, 'RUB', 'FAKE-BFD06B3FA0A5CBD1', '2026-10-10 12:02:00+00', NULL);

-- Доставки (Id = Id заказа) (6)
INSERT INTO deliveries (Id, CourierId, Status, AssignedAt, AcceptedAt, DeliveredAt, FailureReason) VALUES
    ('00000009-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000005', 2, '2026-10-02 14:00:00+00', '2026-10-03 09:30:00+00', '2026-10-03 12:10:00+00', NULL),
    ('00000009-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000006', 1, '2026-10-30 09:00:00+00', '2026-10-30 09:40:00+00', NULL, NULL),
    ('00000009-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000005', 0, '2026-10-30 08:30:00+00', NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000004', '00000002-0000-0000-0000-000000000006', 3, '2026-10-26 10:00:00+00', '2026-10-27 11:00:00+00', NULL, 'Покупатель не отвечал на звонки, дверь не открыли'),
    ('00000009-0000-0000-0000-000000000006', '00000002-0000-0000-0000-000000000005', 4, '2026-10-20 13:00:00+00', NULL, NULL, NULL),
    ('00000009-0000-0000-0000-000000000009', '00000002-0000-0000-0000-000000000006', 2, '2026-10-10 15:00:00+00', '2026-10-11 10:00:00+00', '2026-10-11 13:00:00+00', NULL);

-- Отзывы (9)
INSERT INTO reviews (Id, UserId, ProductId, Rating, Text, CreatedAt, EditedAt) VALUES
    ('0000000E-0000-0000-0000-000000000001', '00000002-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000001', 5, 'Экран 120 Гц — после него на другие смотреть не хочется. Камера отличная.', '2026-10-18 12:00:00+00', NULL),
    ('0000000E-0000-0000-0000-000000000002', '00000002-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000001', 4, 'Через месяц: держит день активного использования, но греется в играх.', '2026-10-28 12:00:00+00', NULL),
    ('0000000E-0000-0000-0000-000000000003', '00000002-0000-0000-0000-000000000003', '00000004-0000-0000-0000-000000000001', 5, 'Брали в офис для сотрудников, все довольны.', '2026-10-10 12:00:00+00', NULL),
    ('0000000E-0000-0000-0000-000000000004', '00000002-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000010', 4, 'Шумоподавление отличное, но кейс странный и наушники тяжеловаты.', '2026-10-25 12:00:00+00', '2026-10-26 12:00:00+00'),
    ('0000000E-0000-0000-0000-000000000005', '00000002-0000-0000-0000-000000000002', '00000004-0000-0000-0000-000000000010', 5, 'Лучший звук, который я слышала в беспроводных наушниках.', '2026-10-21 12:00:00+00', NULL),
    ('0000000E-0000-0000-0000-000000000006', '00000002-0000-0000-0000-000000000001', '00000004-0000-0000-0000-00000000000A', 5, 'Тихий, быстрый, экран потрясающий. Батареи хватает на весь день.', '2026-10-15 12:00:00+00', NULL),
    ('0000000E-0000-0000-0000-000000000007', '00000002-0000-0000-0000-000000000001', '00000004-0000-0000-0000-000000000015', 4, 'Лёд колет без проблем, но довольно шумный.', '2026-10-23 12:00:00+00', NULL),
    ('0000000E-0000-0000-0000-000000000008', '00000002-0000-0000-0000-000000000004', '00000004-0000-0000-0000-00000000001D', 5, 'Заряжает быстро, не греется.', '2026-10-27 12:00:00+00', NULL),
    ('0000000E-0000-0000-0000-000000000009', '00000002-0000-0000-0000-000000000007', '00000004-0000-0000-0000-000000000012', 5, 'Маленькая, но звук для своего размера отличный.', '2026-10-26 12:00:00+00', NULL);

-- Локальный кэш ответов приложения (5)
INSERT INTO cache (Key, Payload, StoredAt, ExpiresAt) VALUES
    ('categories', '[{"id": "00000003-0000-0000-0000-000000000001", "name": "Электроника"}, {"id": "00000003-0000-0000-0000-000000000007", "name": "Дом"}, {"id": "00000003-0000-0000-0000-00000000000a", "name": "Аксессуары"}]', '2026-10-30 11:38:30+00', '2026-10-30 12:38:30+00'),
    ('product:00000004-0000-0000-0000-000000000001', '{"id": "00000004-0000-0000-0000-000000000001", "name": "Apple iPhone 13 Pro 128 ГБ", "price": 79990, "stock": 14}', '2026-10-30 11:39:10+00', '2026-10-30 11:49:10+00'),
    ('catalog:smartphones:price-asc', '{"page": 1, "total": 7}', '2026-10-30 11:39:00+00', '2026-10-30 11:44:00+00'),
    ('orders:active', '{"count": 2}', '2026-10-30 11:46:30+00', '2026-10-30 11:51:30+00'),
    ('profile', '{"login": "buyer", "firstName": "Иван", "lastName": "Покупателев"}', '2026-10-30 11:38:20+00', NULL);

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
  AND p.IsActive AND p.Stock > 0
  AND p.price_amount < 50000
ORDER BY p.price_amount;

-- 3.2 (JOIN, WHERE, ORDER BY) Заказы покупателя buyer: статус, состав и сумма строки.
SELECT to_char(o.PlacedAt AT TIME ZONE 'UTC', 'YYYY-MM-DD HH24:MI') AS PlacedAt,
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
  AND o.PlacedAt >= '2026-10-01 00:00+00'
  AND o.PlacedAt <  '2026-11-01 00:00+00'
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
WHERE r.Name = 'Курьер' AND u.DeletedAt IS NULL AND NOT (u.IsBlocked)
GROUP BY u.Id, u.Login, u.FirstName, u.LastName
ORDER BY Total DESC, u.Login;
