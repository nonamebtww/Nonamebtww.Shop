namespace Nonamebtww.Shop.Mobile.Services.Navigation;

/// <summary>Navigation abstraction so ViewModels do not depend on Shell directly.</summary>
public interface INavigationService {
  Task GoToAsync(string route, IDictionary<string, object>? parameters = null);
  Task GoBackAsync();
  Task GoToRootAsync(string root);
}
