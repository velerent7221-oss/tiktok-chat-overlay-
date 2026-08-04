using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace tiktok_chat_levach
{
    public partial class LatestFollowerForm : Form
    {
        public WebView2 webViewLatestFollower;
        private AppConfig currentConfig;

        public LatestFollowerForm(AppConfig config)
        {
            currentConfig = config;
            UpdateStyle(config);

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(config.LatestFollowerX, config.LatestFollowerY);
            this.Size = new Size(config.LatestFollowerWidth, config.LatestFollowerHeight);
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Text = "TikTok Live - Son Takipçi Penceresi";

            webViewLatestFollower = new WebView2()
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(webViewLatestFollower);

            if (!string.IsNullOrEmpty(config.LatestFollowerUrl))
            {
                InitializeWebView(config.LatestFollowerUrl);
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
                if (webViewLatestFollower == null || webViewLatestFollower.IsDisposed) return;

                string userDataFolder = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "WebView2_Cache_LatestFollower");
                
                // CPU yükünü düşürmek için optimize edilmiş Chromium argümanları
                var options = new CoreWebView2EnvironmentOptions();
                options.AdditionalBrowserArguments = "--disable-extensions --disable-background-networking --disable-sync --disable-component-extensions-with-background-pages";

                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);

                await webViewLatestFollower.EnsureCoreWebView2Async(environment);

                try { webViewLatestFollower.DefaultBackgroundColor = Color.Transparent; } catch { }

                webViewLatestFollower.ZoomFactor = currentConfig.LatestFollowerZoom;

                webViewLatestFollower.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    if (webViewLatestFollower?.CoreWebView2 != null)
                    {
                        await webViewLatestFollower.CoreWebView2.ExecuteScriptAsync(
                            "document.body.style.backgroundColor = 'transparent';" +
                            "document.documentElement.style.backgroundColor = 'transparent';" +
                            "document.body.style.overflow = 'hidden';" +
                            "if (!document.getElementById('customLatestFollowerLabel')) {" +
                            "   var container = document.createElement('div');" +
                            "   container.id = 'customLatestFollowerLabel';" +
                            "   container.style.display = 'flex';" +
                            "   container.style.alignItems = 'center';" +
                            "   container.style.gap = '4px';" +
                            "   container.style.marginBottom = '2px';" +
                            "   " +
                            "   var icon = document.createElement('span');" +
                            "   icon.innerText = '👤';" +
                            "   icon.style.fontSize = '12px';" +
                            "   " +
                            "   var lbl = document.createElement('span');" +
                            "   lbl.innerText = 'SON TAKİPÇİ';" +
                            "   lbl.style.color = '#0078d4';" + // Mavi/Tema uyumlu renk[cite: 17]
                            "   lbl.style.fontSize = '11px';" +
                            "   lbl.style.fontWeight = 'bold';" +
                            "   lbl.style.fontFamily = 'Segoe UI, sans-serif';" +
                            "   lbl.style.textShadow = '1px 1px 2px black';" +
                            "   " +
                            "   container.appendChild(icon);" +
                            "   container.appendChild(lbl);" +
                            "   document.body.insertBefore(container, document.body.firstChild);" +
                            "}"
                        );
                    }
                };

                if (webViewLatestFollower.CoreWebView2 != null && !string.IsNullOrEmpty(url))
                {
                    webViewLatestFollower.CoreWebView2.Navigate(url);
                }
            }
            catch { }
        }

        public void UpdateConfiguration(AppConfig config)
        {
            currentConfig = config;
            UpdateStyle(config);
            this.Location = new Point(config.LatestFollowerX, config.LatestFollowerY);
            this.Size = new Size(config.LatestFollowerWidth, config.LatestFollowerHeight);

            if (webViewLatestFollower != null)
            {
                webViewLatestFollower.ZoomFactor = config.LatestFollowerZoom;
            }
        }

        // Form kapatıldığında WebView2 işlemlerini sonlandırarak arka plan CPU tüketimini engeller
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                if (webViewLatestFollower != null && !webViewLatestFollower.IsDisposed)
                {
                    webViewLatestFollower.Dispose();
                }
            }
            catch { }
            base.OnFormClosed(e);
        }
    }
}