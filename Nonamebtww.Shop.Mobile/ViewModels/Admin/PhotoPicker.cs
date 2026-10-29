namespace Nonamebtww.Shop.Mobile.ViewModels.Admin;

/// <summary>Picks a photo from the device gallery and copies it into app storage (no network, local only).</summary>
internal static class PhotoPicker {
  public static async Task<string?> PickAsync() {
    var results = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { Title = "Фото", SelectionLimit = 1 });
    var file = results?.FirstOrDefault();
    if (file is null) {
      return null;
    }

    var folder = Path.Combine(FileSystem.AppDataDirectory, "photos");
    Directory.CreateDirectory(folder);
    var target = Path.Combine(folder, $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}");

    await using var source = await file.OpenReadAsync();
    await using var destination = File.Create(target);
    await source.CopyToAsync(destination);
    return target;
  }
}
