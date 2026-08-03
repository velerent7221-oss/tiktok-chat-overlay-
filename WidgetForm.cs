using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace tiktok_chat_levach
{
    public partial class WidgetForm : Form
    {
        public WebView2 webViewTikFinity;
        private AppConfig currentConfig;

        public WidgetForm(AppConfig config)
        {
            currentConfig = config;
            UpdateStyle(config);

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(config.WidgetX, config.WidgetY);
            this.Size = new Size(config.WidgetWidth, config.WidgetHeight);
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Text = "TikTok Live - Widget Penceresi";

            webViewTikFinity = new WebView2()
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(webViewTikFinity);

            if (!string.IsNullOrEmpty(config.TikFinityUrl))
            {
                InitializeWebView(config.TikFinityUrl);
            }
        }

        public void UpdateStyle(AppConfig config)
        {
            currentConfig = config;
            if (config.UseTransparentBackground)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.Magenta;
                this.TransparencyKey = Color.Magenta;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.BackColor = Color.FromArgb(32, 32, 32);
                this.TransparencyKey = Color.Empty;
            }
        }

        private async void InitializeWebView(string url)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(200);
                if (webViewTikFinity == null || webViewTikFinity.IsDisposed) return;

                string userDataFolder = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "WebView2_Cache_Widget");
                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, null);

                await webViewTikFinity.EnsureCoreWebView2Async(environment);

                try { webViewTikFinity.DefaultBackgroundColor = Color.Transparent; } catch { }

                // Doğru kullanım: ZoomFactor doğrudan WebView2 kontrolü üzerinden atanır
                webViewTikFinity.ZoomFactor = currentConfig.WidgetZoom;

                webViewTikFinity.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    await webViewTikFinity.CoreWebView2.ExecuteScriptAsync(
                        "document.body.style.backgroundColor = 'transparent';" +
                        "document.documentElement.style.backgroundColor = 'transparent';" +
                        "document.body.style.overflow = 'hidden';"
                    );
                };

                if (webViewTikFinity.CoreWebView2 != null && !string.IsNullOrEmpty(url))
                {
                    webViewTikFinity.CoreWebView2.Navigate(url);
                }
            }
            catch { }
        }

        public void UpdateConfiguration(AppConfig config)
        {
            currentConfig = config;
            UpdateStyle(config);
            this.Location = new Point(config.WidgetX, config.WidgetY);
            this.Size = new Size(config.WidgetWidth, config.WidgetHeight);

            // Ayarlar panelinden anlık zoom değişimi
            if (webViewTikFinity != null)
            {
                webViewTikFinity.ZoomFactor = config.WidgetZoom;
            }
        }
    }
}