using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Nonamebtww.Shop.Application.Common.Results;
using Nonamebtww.Shop.Mobile.Services.Dialogs;
using Nonamebtww.Shop.Mobile.Services.Navigation;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Nonamebtww.Shop.Mobile.ViewModels.Base;

/// <summary>
/// Common state for all screens (ReactiveUI): busy and skeleton-loading flags, title
/// and safe execution of use cases.
/// </summary>
public abstract partial class ViewModelBase : ReactiveObject {
  private readonly ObservableAsPropertyHelper<bool> _isNotBusy;

  protected ViewModelBase(INavigationService navigation, IDialogService dialogs) {
    Navigation = navigation;
    Dialogs = dialogs;
    _isNotBusy = this.WhenAnyValue(x => x.IsBusy)
      .Select(busy => !busy)
      .ToProperty(this, nameof(IsNotBusy), initialValue: true, scheduler: ImmediateScheduler.Instance);
  }

  protected INavigationService Navigation { get; }
  protected IDialogService Dialogs { get; }

  [Reactive]
  public partial bool IsBusy { get; set; }

  /// <summary>First load without cached data: the page shows skeleton placeholders with a shimmer.</summary>
  [Reactive]
  public partial bool IsLoading { get; set; }

  [Reactive]
  public partial string Title { get; set; } = string.Empty;

  public bool IsNotBusy => _isNotBusy.Value;

  /// <summary>
  /// Read-only property computed from other properties (ReactiveUI OAPH, created on first read):
  /// <c>public string X => Derived(ref _x, this.WhenAnyValue(v => v.Y).Select(...));</c>
  /// </summary>
  protected T Derived<T>(ref ObservableAsPropertyHelper<T>? helper, IObservable<T> source, [CallerMemberName] string property = "") {
    // Deferred: the helper is stored before it subscribes, so the change notification raised
    // for the first value (which makes bindings read this property again) does not re-create it.
    helper ??= source.ToProperty(this, property, deferSubscription: true, scheduler: ImmediateScheduler.Instance);
    return helper.Value;
  }

  /// <summary>Called by the page every time it appears.</summary>
  public virtual Task OnAppearingAsync() => Task.CompletedTask;

  /// <summary>
  /// Loads screen data: shows skeletons while <paramref name="load"/> runs, unless the data
  /// already came from the cache (then <paramref name="isCached"/> returns true and no skeleton is shown).
  /// </summary>
  protected async Task LoadWithSkeletonAsync(Func<Task> load, Func<bool>? isCached = null) {
    var showSkeleton = isCached?.Invoke() != true;
    if (showSkeleton) {
      IsLoading = true;
    }

    try {
      await load();
    } finally {
      IsLoading = false;
    }
  }

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
