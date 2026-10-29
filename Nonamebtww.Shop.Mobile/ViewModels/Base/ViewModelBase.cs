using CommunityToolkit.Mvvm.ComponentModel;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;

namespace Nonamebtww.Shop.Mobile.ViewModels.Base;

/// <summary>Common state for all screens: busy flag, title and safe execution of use cases.</summary>
public abstract partial class ViewModelBase(INavigationService navigation, IDialogService dialogs) : ObservableObject {
  protected INavigationService Navigation { get; } = navigation;
  protected IDialogService Dialogs { get; } = dialogs;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(IsNotBusy))]
  public partial bool IsBusy { get; set; }

  [ObservableProperty]
  public partial string Title { get; set; } = string.Empty;

  public bool IsNotBusy => !IsBusy;

  /// <summary>Called by the page every time it appears.</summary>
  public virtual Task OnAppearingAsync() => Task.CompletedTask;

  /// <summary>Runs a use case returning a value; shows its error and returns default on failure.</summary>
  protected async Task<T?> ExecuteAsync<T>(Func<Task<Result<T>>> useCase, bool showBusy = true) {
    T? value = default;
    await RunAsync(async () => {
      var result = await useCase();
      if (result.IsFailure) {
        await ShowErrorAsync(result.Error);
        return;
      }

      value = result.Value;
    }, showBusy);
    return value;
  }

  /// <summary>Same as <see cref="ExecuteAsync{T}"/> for value-type DTOs; returns null on failure.</summary>
  protected async Task<T?> ExecuteValueAsync<T>(Func<Task<Result<T>>> useCase, bool showBusy = true) where T : struct {
    T? value = null;
    await RunAsync(async () => {
      var result = await useCase();
      if (result.IsFailure) {
        await ShowErrorAsync(result.Error);
        return;
      }

      value = result.Value;
    }, showBusy);
    return value;
  }

  /// <summary>Runs a use case without a value; returns true on success.</summary>
  protected async Task<bool> ExecuteAsync(Func<Task<Result>> useCase, bool showBusy = true) {
    var success = false;
    await RunAsync(async () => {
      var result = await useCase();
      if (result.IsFailure) {
        await ShowErrorAsync(result.Error);
        return;
      }

      success = true;
    }, showBusy);
    return success;
  }

  /// <summary>Runs an action and shows unexpected exceptions instead of crashing.</summary>
  protected async Task RunAsync(Func<Task> action, bool showBusy = true) {
    if (showBusy && IsBusy) {
      return;
    }

    try {
      if (showBusy) {
        IsBusy = true;
      }

      await action();
    } catch (Exception ex) {
      await Dialogs.AlertAsync("Ошибка", ex.Message);
    } finally {
      if (showBusy) {
        IsBusy = false;
      }
    }
  }

  protected Task ShowErrorAsync(Error? error) {
    if (error is null) {
      return Task.CompletedTask;
    }

    var message = error.Fields.Count == 0
      ? error.Message
      : string.Join(Environment.NewLine, error.Fields.SelectMany(x => x.Value));

    return Dialogs.AlertAsync("Не получилось", message);
  }
}
