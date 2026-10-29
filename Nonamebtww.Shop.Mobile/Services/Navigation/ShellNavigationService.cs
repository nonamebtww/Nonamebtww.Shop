namespace Nonamebtww.Shop.Mobile.Services.Navigation;

public sealed class ShellNavigationService : INavigationService {
  public Task GoToAsync(string route, IDictionary<string, object>? parameters = null) =>
    MainThread.InvokeOnMainThreadAsync(() => parameters is null
      ? Shell.Current.GoToAsync(route)
      : Shell.Current.GoToAsync(route, parameters));

  public Task GoBackAsync() =>
    MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(".."));

  public Task GoToRootAsync(string root) =>
    MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync($"//{root}"));
}
