namespace Nonamebtww.Shop.Mobile.Services.Dialogs;

public interface IDialogService {
  Task AlertAsync(string title, string message, string cancel = "OK");
  Task<bool> ConfirmAsync(string title, string message, string accept = "Да", string cancel = "Нет");
  Task<string?> PromptAsync(string title, string message, string? initial = null, Keyboard? keyboard = null);
  Task<string?> ChooseAsync(string title, params string[] options);
}
