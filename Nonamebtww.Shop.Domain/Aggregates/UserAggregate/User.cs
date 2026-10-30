using Nonamebtww.Shop.Domain.Aggregates.RoleAggregate;
using Nonamebtww.Shop.Domain.Aggregates.UserAggregate.Events;
using Nonamebtww.Shop.Domain.Common;
using Nonamebtww.Shop.Domain.Entities.Abstractions;
using Nonamebtww.Shop.Domain.Exceptions;
using Nonamebtww.Shop.Domain.ValueObjects;

namespace Nonamebtww.Shop.Domain.Aggregates.UserAggregate;

/// <summary>
/// Пользователь (IDEF1X «Пользователь»). Агрегат включает устройства входа и коды подтверждения.
/// Хэширование пароля — забота приложения: сюда передаётся готовый хэш.
/// Для связи нужен хотя бы один контакт: email или телефон.
/// </summary>
public class User : AggregateRoot<Guid> {
  private readonly List<Device> _devices = [];
  private readonly List<ConfirmationCode> _confirmationCodes = [];

  private User() { }

  private User(
    Guid id,
    UserRole role,
    Login login,
    Email? email,
    PhoneNumber? phone,
    string passwordHash,
    string firstName,
    string? lastName,
    DateTimeOffset registeredAt) : base(id) {
    EnsureContact(email, phone);
    Role = role;
    Login = login;
    Email = email;
    Phone = phone;
    PasswordHash = Guard.NotEmpty(passwordHash, "Хэш пароля", 1024);
    FirstName = Guard.NotEmpty(firstName, "Имя", 64);
    LastName = Guard.Optional(lastName, "Фамилия", 64);
    RegisteredAt = registeredAt;
  }

  public UserRole Role { get; private set; }

  /// <summary>Логин (AK1).</summary>
  public Login Login { get; private set; } = null!;

  /// <summary>Email (AK2), необязателен, если указан телефон.</summary>
  public Email? Email { get; private set; }

  /// <summary>Телефон (AK3), необязателен, если указан email.</summary>
  public PhoneNumber? Phone { get; private set; }

  public string PasswordHash { get; private set; } = null!;
  public string FirstName { get; private set; } = null!;
  public string? LastName { get; private set; }
  public DateTimeOffset RegisteredAt { get; private set; }
  public bool IsConfirmed { get; private set; }
  public bool IsBlocked { get; private set; }
  public string? BlockReason { get; private set; }
  public DateTimeOffset? BlockedAt { get; private set; }

  /// <summary>Дата удаления аккаунта. Удалённый аккаунт обезличен, войти в него нельзя.</summary>
  public DateTimeOffset? DeletedAt { get; private set; }

  public bool IsDeleted => DeletedAt is not null;

  public IReadOnlyCollection<Device> Devices => _devices.AsReadOnly();
  public IReadOnlyCollection<ConfirmationCode> ConfirmationCodes => _confirmationCodes.AsReadOnly();

  public string FullName => LastName is null ? FirstName : $"{FirstName} {LastName}";

  /// <summary>
  /// Регистрация по email или по телефону. Уникальность логина, email и телефона проверяет приложение
  /// через <see cref="IUsersRepository"/>. Сотрудники создаются сразу подтверждёнными (<paramref name="confirmed"/>),
  /// покупатели подтверждают регистрацию кодом из письма или SMS.
  /// </summary>
  public static User Register(
    Login login,
    Email? email,
    PhoneNumber? phone,
    string passwordHash,
    string firstName,
    string? lastName,
    DateTimeOffset now,
    UserRole role = UserRole.Customer,
    bool confirmed = false) {
    var user = new User(Guid.NewGuid(), role, login, email, phone, passwordHash, firstName, lastName, now) {
      IsConfirmed = confirmed,
    };
    user.Raise(new UserRegistered(user.Id, login.Value, email?.Value, phone?.Value, role));
    return user;
  }

  public void Confirm() {
    if (IsConfirmed) return;
    IsConfirmed = true;
    Raise(new UserConfirmed(Id));
  }

  /// <summary>Проверка перед входом: аккаунт не удалён, подтверждён и не заблокирован.</summary>
  public void EnsureCanSignIn() {
    if (IsDeleted) throw new DomainException("Аккаунт удалён.");
    if (IsBlocked)
      throw new DomainException(BlockReason is null
        ? "Аккаунт заблокирован."
        : $"Аккаунт заблокирован: {BlockReason}");
    if (!IsConfirmed) throw new DomainException("Аккаунт не подтверждён.");
  }

  public void Block(string reason, DateTimeOffset now) {
    if (IsBlocked) throw new DomainException("Аккаунт уже заблокирован.");
    if (Role == UserRole.Owner) throw new DomainException("Нельзя заблокировать владельца магазина.");

    IsBlocked = true;
    BlockReason = Guard.NotEmpty(reason, "Причина блокировки", 512);
    BlockedAt = now;
    SignOutEverywhere();
    Raise(new UserBlocked(Id, BlockReason));
  }

  public void Unblock() {
    if (!IsBlocked) throw new DomainException("Аккаунт не заблокирован.");

    IsBlocked = false;
    BlockReason = null;
    BlockedAt = null;
    Raise(new UserUnblocked(Id));
  }

  public void ChangeRole(UserRole role) {
    if (Role == role) return;

    var old = Role;
    Role = role;
    SignOutEverywhere();
    Raise(new UserRoleChanged(Id, old, role));
  }

  /// <summary>Смена пароля: все устройства разлогиниваются.</summary>
  public void ChangePassword(string newPasswordHash) {
    PasswordHash = Guard.NotEmpty(newPasswordHash, "Хэш пароля", 1024);
    SignOutEverywhere();
    Raise(new UserPasswordChanged(Id));
  }

  public void UpdateProfile(string firstName, string? lastName) {
    FirstName = Guard.NotEmpty(firstName, "Имя", 64);
    LastName = Guard.Optional(lastName, "Фамилия", 64);
    Raise(new UserProfileUpdated(Id));
  }

  /// <summary>Смена контактов. Один из них можно убрать, оба сразу — нельзя.</summary>
  public void ChangeContacts(Email? email, PhoneNumber? phone) {
    EnsureContact(email, phone);

    if (Email != email) {
      var old = Email;
      Email = email;
      Raise(new UserEmailChanged(Id, old?.Value, email?.Value));
    }

    if (Phone != phone) {
      var old = Phone;
      Phone = phone;
      Raise(new UserPhoneChanged(Id, old?.Value, phone?.Value));
    }
  }

  /// <summary>
  /// Удаление аккаунта по просьбе пользователя. Запись остаётся (на неё ссылаются заказы и отзывы),
  /// но персональные данные стираются, вход закрывается на всех устройствах.
  /// Незавершённые заказы проверяет приложение.
  /// </summary>
  public void Delete(DateTimeOffset now) {
    if (IsDeleted) throw new DomainException("Аккаунт уже удалён.");
    if (Role == UserRole.Owner) throw new DomainException("Аккаунт владельца магазина удалить нельзя.");

    Login = new Login($"deleted_{Id:N}"[..Login.MaxLength]);
    Email = null;
    Phone = null;
    FirstName = "Удалённый";
    LastName = "пользователь";
    PasswordHash = "deleted";
    DeletedAt = now;

    SignOutEverywhere();
    foreach (var code in _confirmationCodes.Where(c => !c.IsUsed)) code.MarkUsed();
    Raise(new UserDeleted(Id));
  }

  /// <summary>Фиксирует вход: обновляет активное устройство с тем же названием или добавляет новое.</summary>
  public Device RecordSignIn(string deviceName, string? ipAddress, string refreshToken, DateTimeOffset now) {
    EnsureCanSignIn();

    var name = Guard.NotEmpty(deviceName, "Название устройства", 128);
    var device = _devices.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));
    if (device is null) {
      device = new Device(name, ipAddress, refreshToken, now);
      _devices.Add(device);
    } else {
      device.Touch(ipAddress, refreshToken, now);
    }

    Raise(new UserSignedIn(Id, device.Id, device.Name));
    return device;
  }

  public void SignOut(Guid deviceId) {
    var device = _devices.FirstOrDefault(d => d.Id == deviceId)
      ?? throw new DomainException("Устройство не найдено.");
    device.Deactivate();
  }

  public void SignOutEverywhere() {
    foreach (var device in _devices) device.Deactivate();
  }

  /// <summary>
  /// Выпускает код подтверждения. Прежние неиспользованные коды того же назначения гасятся.
  /// Сам код генерирует приложение.
  /// </summary>
  public ConfirmationCode IssueCode(ConfirmationCodePurpose purpose, string code, DateTimeOffset now, TimeSpan lifetime) {
    if (lifetime <= TimeSpan.Zero) throw new DomainException("Срок действия кода должен быть положительным.");
    if (IsDeleted) throw new DomainException("Аккаунт удалён.");
    if (purpose == ConfirmationCodePurpose.Registration && IsConfirmed)
      throw new DomainException("Аккаунт уже подтверждён.");
    if (purpose == ConfirmationCodePurpose.PasswordReset && IsBlocked)
      throw new DomainException("Аккаунт заблокирован.");

    foreach (var old in _confirmationCodes.Where(c => c.Purpose == purpose && !c.IsUsed)) old.MarkUsed();

    var confirmationCode = new ConfirmationCode(purpose, code, now, lifetime);
    _confirmationCodes.Add(confirmationCode);
    Raise(new ConfirmationCodeIssued(Id, Email?.Value, Phone?.Value, purpose, confirmationCode.Code, confirmationCode.ExpiresAt));
    return confirmationCode;
  }

  /// <summary>
  /// Проверяет и гасит код. Код регистрации сразу подтверждает аккаунт;
  /// после кода восстановления пароля приложение вызывает <see cref="ChangePassword"/>.
  /// </summary>
  public void UseCode(ConfirmationCodePurpose purpose, string code, DateTimeOffset now) {
    if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Введите код подтверждения.");

    var confirmationCode = _confirmationCodes
      .Where(c => c.Purpose == purpose && !c.IsUsed)
      .OrderByDescending(c => c.CreatedAt)
      .FirstOrDefault();

    if (confirmationCode is null || !confirmationCode.Matches(code))
      throw new DomainException("Неверный код подтверждения.");
    if (confirmationCode.IsExpired(now)) throw new DomainException("Срок действия кода истёк. Запросите новый.");

    confirmationCode.MarkUsed();
    if (purpose == ConfirmationCodePurpose.Registration) Confirm();
  }

  private static void EnsureContact(Email? email, PhoneNumber? phone) {
    if (email is null && phone is null) throw new DomainException("Укажите email или телефон.");
  }
}
