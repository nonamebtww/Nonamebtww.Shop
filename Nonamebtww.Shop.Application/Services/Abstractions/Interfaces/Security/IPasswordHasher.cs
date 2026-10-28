namespace Nonamebtww.Shop.Application.Services.Abstractions.Interfaces.Security;

public interface IPasswordHasher {
  string Hash(string password);
  bool Verify(string password, string hash);
}
