using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace tiktok_chat_levach
{
    public partial class GiftFeedForm : Form
    {
        public WebView2 webViewGiftFeed;
        private AppConfig currentConfig;

        public GiftFeedForm(AppConfig config)
        {
            currentConfig = config;
            UpdateStyle(config);

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(config.GiftFeedX, config.GiftFeedY);
            this.Size = new Size(config.GiftFeedWidth, config.GiftFeedHeight);
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Text = "TikTok Live - Hediye Beslemesi Penceresi";

            webViewGiftFeed = new WebView2()
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(webViewGiftFeed);

            if (!string.IsNullOrEmpty(config.GiftFeedUrl))
            {
                InitializeWebView(config.GiftFeedUrl);
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
                if (webViewGiftFeed == null || webViewGiftFeed.IsDisposed) return;

                string userDataFolder = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "WebView2_Cache_GiftFeed");
                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, null);

                await webViewGiftFeed.EnsureCoreWebView2Async(environment);

                try { webViewGiftFeed.DefaultBackgroundColor = Color.Transparent; } catch { }

                webViewGiftFeed.ZoomFactor = currentConfig.GiftFeedZoom;

                webViewGiftFeed.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    await webViewGiftFeed.CoreWebView2.ExecuteScriptAsync(
                        "document.body.style.backgroundColor = 'transparent';" +
                        "document.documentElement.style.backgroundColor = 'transparent';" +
                        "document.body.style.overflow = 'hidden';"
                    );
                };

                if (webViewGiftFeed.CoreWebView2 != null && !string.IsNullOrEmpty(url))
                {
                    webViewGiftFeed.CoreWebView2.Navigate(url);
                }
            }
            catch { }
        }

        /// <summary>
        /// Yeni bir hediye geldiğinde çağrılabilir. 
        /// HTML tarafında 'updateGiftFeed(giftHtml)' adında bir fonksiyon varsa, 
        /// bu fonksiyon eskisini silip sadece en son geleni ekleyecektir.
        /// </summary>
        public async void ShowLatestGift(string giftHtmlContent)
        {
            if (webViewGiftFeed?.CoreWebView2 != null)
            {
                // JavaScript tarafında önceki veriyi temizleyip yeni veriyi basan örnek mantık
                string safeContent = giftHtmlContent.Replace("'", "\\'").Replace("\r", "").Replace("\n", "");
                string script = $"if(typeof updateGiftFeed === 'function') {{ updateGiftFeed('{safeContent}'); }} " +
                                $"else {{ document.body.innerHTML = '{safeContent}'; }}";

                await webViewGiftFeed.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        public void UpdateConfiguration(AppConfig config)
        {
            currentConfig = config;
            UpdateStyle(config);
            this.Location = new Point(config.GiftFeedX, config.GiftFeedY);
            this.Size = new Size(config.GiftFeedWidth, config.GiftFeedHeight);

            if (webViewGiftFeed != null)
            {
                webViewGiftFeed.ZoomFactor = config.GiftFeedZoom;
            }
        }
    }
}