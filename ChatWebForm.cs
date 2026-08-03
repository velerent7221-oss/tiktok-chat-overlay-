using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace tiktok_chat_levach
{
    public partial class ChatWebForm : Form
    {
        public WebView2 webViewChatWeb;
        private AppConfig currentConfig;

        public ChatWebForm(AppConfig config)
        {
            currentConfig = config;
            UpdateStyle(config);

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(config.ChatWebX, config.ChatWebY);
            this.Size = new Size(config.ChatWebWidth, config.ChatWebHeight);
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Text = "TikTok Live - Sohbet Web Penceresi";

            webViewChatWeb = new WebView2()
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(webViewChatWeb);

            if (!string.IsNullOrEmpty(config.ChatWebUrl))
            {
                InitializeWebView(config.ChatWebUrl);
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
                if (webViewChatWeb == null || webViewChatWeb.IsDisposed) return;

                string userDataFolder = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "WebView2_Cache_ChatWeb");
                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, null);

                await webViewChatWeb.EnsureCoreWebView2Async(environment);

                try { webViewChatWeb.DefaultBackgroundColor = Color.Transparent; } catch { }

                // Zoom faktörü doğrudan WebView2 üzerinden atanır
                webViewChatWeb.ZoomFactor = currentConfig.ChatWebZoom;

                webViewChatWeb.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    await webViewChatWeb.CoreWebView2.ExecuteScriptAsync(
                        "document.body.style.backgroundColor = 'transparent';" +
                        "document.documentElement.style.backgroundColor = 'transparent';" +
                        "document.body.style.overflow = 'hidden';"
                    );
                };

                if (webViewChatWeb.CoreWebView2 != null && !string.IsNullOrEmpty(url))
                {
                    webViewChatWeb.CoreWebView2.Navigate(url);
                }
            }
            catch { }
        }

        public void UpdateConfiguration(AppConfig config)
        {
            currentConfig = config;
            UpdateStyle(config);
            this.Location = new Point(config.ChatWebX, config.ChatWebY);
            this.Size = new Size(config.ChatWebWidth, config.ChatWebHeight);

            // Ayarlar panelinden anlık zoom değişimi
            if (webViewChatWeb != null)
            {
                webViewChatWeb.ZoomFactor = config.ChatWebZoom;
            }

            // URL değiştiyse veya güncellendiyse yenidennavigate et
            if (webViewChatWeb != null && webViewChatWeb.CoreWebView2 != null && !string.IsNullOrEmpty(config.ChatWebUrl))
            {
                string currentSource = webViewChatWeb.Source?.OriginalString;
                if (currentSource != config.ChatWebUrl)
                {
                    webViewChatWeb.CoreWebView2.Navigate(config.ChatWebUrl);
                }
            }
        }
    }
}