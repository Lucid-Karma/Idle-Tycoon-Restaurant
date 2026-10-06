#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Beyzosh gömülme koruması: her WebGL build'inden sonra index.html'in <head>'inin
// en başına küçük bir betik ekler.
//
// Koruma YALNIZCA oyun ProtectedHosts'taki bir sunucudan (games.beyzosh.com) açıldığında
// çalışır. O durumda oyun sadece AllowedEmbedders'taki sitelerde iframe'e gömülebilir
// (ya da doğrudan açılabilir); başka bir site gömerse Unity yüklenmez, beyzosh.com
// bağlantısı gösterilir.
//
// Aynı build itch.io, CrazyGames, Poki vb. platformlara yüklendiğinde oyun onların
// kendi sunucusundan açıldığı için koruma hiç devreye girmez — orada normal çalışır.
// Bir platform oyunu doğrudan games.beyzosh.com adresinden gömecekse, o platformun
// adresini AllowedEmbedders'a ekle.
public class BeyzoshEmbedGuardPostBuild : IPostprocessBuildWithReport
{
    public int callbackOrder => 1000;

    private static readonly string[] ProtectedHosts = { "games.beyzosh.com" };

    private static readonly string[] AllowedEmbedders =
    {
        "https://beyzosh.com",
        "https://www.beyzosh.com",
        // "https://www.crazygames.com",
    };

    private const string Marker = "beyzosh-embed-guard";

    private const string GuardScript = @"<script>/* beyzosh-embed-guard */
(function () {
  if (window.top === window.self) return;
  var protectedHosts = __HOSTS__;
  if (protectedHosts.indexOf(location.hostname) === -1) return;
  var allowedList = __ALLOWED__;
  var allowed = { test: function (o) {
    return allowedList.indexOf(o) !== -1 || /^https?:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/.test(o);
  } };
  var origins = [];
  try {
    var ao = window.location.ancestorOrigins;
    if (ao && ao.length) for (var i = 0; i < ao.length; i++) origins.push(ao[i]);
  } catch (e) {}
  if (!origins.length && document.referrer) {
    try { origins.push(new URL(document.referrer).origin); } catch (e) {}
  }
  var ok = origins.length > 0;
  for (var j = 0; j < origins.length; j++) if (!allowed.test(origins[j])) ok = false;
  if (ok) return;
  var id = (location.pathname.split('/')[1] || '').replace(/[^\w-]/g, '');
  var link = 'https://beyzosh.com/' + (id ? '#oyna=' + id : '');
  // Mesajı yaz, ardından açılan <plaintext> sayfanın geri kalanını (Unity betikleri
  // dahil) çalışmayan gizli metne çevirir — oyun hiç yüklenmez.
  document.write('<div style=""position:fixed;inset:0;z-index:2147483647;display:flex;align-items:center;' +
    'justify-content:center;background:#1d0c24;color:#fdf4ff;font:700 16px/1.5 system-ui,sans-serif;' +
    'text-align:center;padding:24px""><div><p style=""margin:0 0 14px"">Bu oyunu beyzosh.com&#39;da oyna<br>' +
    'Play this game on beyzosh.com</p><a href=""' + link + '"" target=""_blank"" rel=""noopener"" ' +
    'style=""display:inline-block;padding:12px 22px;border-radius:999px;background:#ff66c4;color:#1e0c25;' +
    'text-decoration:none;font-weight:800"">beyzosh.com</a></div></div><plaintext style=""display:none"">');
})();
</script>";

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
            return;

        string indexPath = Path.Combine(report.summary.outputPath, "index.html");
        if (!File.Exists(indexPath))
        {
            Debug.LogWarning("[BeyzoshEmbedGuard] index.html bulunamadı: " + indexPath);
            return;
        }

        string html = File.ReadAllText(indexPath);
        if (html.Contains(Marker))
            return;

        int head = html.IndexOf("<head", System.StringComparison.OrdinalIgnoreCase);
        int insertAt = head >= 0 ? html.IndexOf('>', head) + 1 : 0;
        string script = GuardScript
            .Replace("__HOSTS__", JsArray(ProtectedHosts))
            .Replace("__ALLOWED__", JsArray(AllowedEmbedders));
        html = html.Insert(insertAt, "\n    " + script);
        File.WriteAllText(indexPath, html);
        Debug.Log("[BeyzoshEmbedGuard] gömülme koruması eklendi: " + indexPath);
    }

    private static string JsArray(string[] items)
    {
        var parts = new System.Collections.Generic.List<string>();
        foreach (var s in items)
            parts.Add("\"" + s.Trim().TrimEnd('/').Replace("\\", "").Replace("\"", "") + "\"");
        return "[" + string.Join(", ", parts) + "]";
    }
}
#endif
