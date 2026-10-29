namespace Nonamebtww.Shop.Mobile.Services.Platform;

/// <summary>Removes native borders of entries: fields are framed by the shared FieldBorder style.</summary>
public static class FormHandlers {
  public static void Configure() {
    Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Borderless", (handler, _) => {
#if IOS
      handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif ANDROID
      handler.PlatformView.BackgroundTintList =
        Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
    });
  }
}
