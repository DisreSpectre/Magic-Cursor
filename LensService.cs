using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MagicCursor;

public record LensResult(bool Success, string Reason = "")
{
    public static LensResult Launched() => new(true);
    public static LensResult Failed(string reason) => new(false, reason);
}

public static class LensService
{
    private const int MaxSizeBytes = 8 * 1024 * 1024; // 8 MB

    public static async Task<LensResult> SearchAsync(byte[]? pngBytes)
    {
        if (pngBytes == null || pngBytes.Length == 0)
        {
            return LensResult.Failed("No image data captured.");
        }

        if (pngBytes.Length > MaxSizeBytes)
        {
            return LensResult.Failed("Selection too large. Image must be under 8 MB.");
        }

        string? generatedFilePath = null;
        try
        {
            string tempDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MagicCursor",
                "temp"
            );

            Directory.CreateDirectory(tempDir);

            string fileName = $"lens_{Guid.NewGuid():N}.html";
            generatedFilePath = Path.Combine(tempDir, fileName);

            string b64 = Convert.ToBase64String(pngBytes);
            long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string html = $$"""
<!doctype html><html><body style="background:#202124;color:#e8eaed;font-family:Segoe UI,sans-serif">
<p>Opening Google Lens…</p>
<form id="f" method="POST" enctype="multipart/form-data"
      action="https://lens.google.com/v3/upload?hl=en&re=df&st={{ts}}&ep=gsbubb">
  <input type="file" name="encoded_image" id="i" hidden>
</form>
<script>
  const b = atob("{{b64}}"), a = new Uint8Array(b.length);
  for (let n = 0; n < b.length; n++) a[n] = b.charCodeAt(n);
  const dt = new DataTransfer();
  dt.items.add(new File([a], "image.png", { type: "image/png" }));
  document.getElementById("i").files = dt.files;
  document.getElementById("f").submit();
</script></body></html>
""";

            await File.WriteAllTextAsync(generatedFilePath, html, System.Text.Encoding.UTF8);

            Log.Info($"Lens: method=HtmlPost file={fileName}");

            var startInfo = new ProcessStartInfo
            {
                FileName = generatedFilePath,
                UseShellExecute = true
            };

            Process.Start(startInfo);
            Log.Info("Lens: launched OK method=HtmlPost");

            // Schedule deletion after ~60 seconds
            string fileToDelete = generatedFilePath;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(60));
                    if (File.Exists(fileToDelete))
                    {
                        File.Delete(fileToDelete);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[LensService] Scheduled deletion failed for {fileToDelete}", ex);
                }
            });

            return LensResult.Launched();
        }
        catch (Exception ex)
        {
            Log.Error($"Lens: HtmlPost failed ({ex.GetType().Name}: {ex.Message}), falling back", ex);
            return LensResult.Failed(ex.Message);
        }
    }

    public static void CleanupTemp()
    {
        try
        {
            string tempDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MagicCursor",
                "temp"
            );

            if (!Directory.Exists(tempDir))
            {
                return;
            }

            var cutoff = DateTime.UtcNow.AddMinutes(-10);
            var dirInfo = new DirectoryInfo(tempDir);
            foreach (var file in dirInfo.GetFiles("lens_*.html"))
            {
                try
                {
                    if (file.CreationTimeUtc < cutoff || file.LastWriteTimeUtc < cutoff)
                    {
                        file.Delete();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[LensService] Cleanup failed for {file.Name}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("[LensService] CleanupTemp directory scan failed", ex);
        }
    }
}
