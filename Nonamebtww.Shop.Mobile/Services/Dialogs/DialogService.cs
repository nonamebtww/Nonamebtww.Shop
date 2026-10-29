namespace Nonamebtww.Shop.Mobile.Services.Dialogs;

public sealed class DialogService : IDialogService {
  private static Page Page => Shell.Current ?? Microsoft.Maui.Controls.Application.Current!.Windows[0].Page!;

  public Task AlertAsync(string title, string message, string cancel = "OK") =>
    MainThread.InvokeOnMainThreadAsync(() => Page.DisplayAlertAsync(title, message, cancel));

  public Task<bool> ConfirmAsync(string title, string message, string accept = "Да", string cancel = "Нет") =>
    MainThread.InvokeOnMainThreadAsync(() => Page.DisplayAlertAsync(title, message, accept, cancel));

  public Task<string?> PromptAsync(string title, string message, string? initial = null, Keyboard? keyboard = null) =>
    MainThread.InvokeOnMainThreadAsync(() =>
      Page.DisplayPromptAsync(title, message, "OK", "Отмена", initialValue: initial ?? string.Empty, keyboard: keyboard ?? Keyboard.Default));

  public async Task<string?> ChooseAsync(string title, params string[] options) {
    var result = await MainThread.InvokeOnMainThreadAsync(() => Page.DisplayActionSheetAsync(title, "Отмена", null, options));
    return result is null || result == "Отмена" ? null : result;
  }
}
