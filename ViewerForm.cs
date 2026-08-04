using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace tiktok_chat_levach
{
    public partial class ViewerForm : Form
    {
        public WebView2 webViewViewer;
        private AppConfig currentConfig;

        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WM_NCHITTEST = 0x84;
        private const int HTTRANSPARENT = -1;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
                return;
            }
            base.WndProc(ref m);
        }

        public ViewerForm(AppConfig config)
        {
            currentConfig = config;
            UpdateStyle(config);

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(config.ViewerX, config.ViewerY);
            this.Size = new Size(config.ViewerWidth, config.ViewerHeight);
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Text = "TikTok Live - İzleyici Penceresi"; 
            this.Visible = config.ShowViewerCount;

            webViewViewer = new WebView2()
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(webViewViewer);

            if (!string.IsNullOrEmpty(config.ViewerUrl))
            {
                InitializeWebView(config.ViewerUrl);
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
                if (webViewViewer == null || webViewViewer.IsDisposed) return;

                string userDataFolder = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "WebView2_Cache_Viewer");
                
                // CPU yükünü düşürmek için optimize edilmiş Chromium argümanları
                var options = new CoreWebView2EnvironmentOptions();
                options.AdditionalBrowserArguments = "--disable-extensions --disable-background-networking --disable-sync --disable-component-extensions-with-background-pages";

                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);

                await webViewViewer.EnsureCoreWebView2Async(environment);

                try { webViewViewer.DefaultBackgroundColor = Color.Transparent; } catch { }

                webViewViewer.ZoomFactor = currentConfig.ViewerZoom;

                webViewViewer.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    // Sol tarafta göz simgesi (👁️) ve yanında kırmızı "İZLEYİCİ" yazısı ekleyen script[cite: 16]
                    await webViewViewer.CoreWebView2.ExecuteScriptAsync(
                        "document.body.style.backgroundColor = 'transparent';" +
                        "document.documentElement.style.backgroundColor = 'transparent';" +
                        "document.body.style.overflow = 'hidden';" +
                        "if (!document.getElementById('customViewerLabel')) {" +
                        "   var container = document.createElement('div');" +
                        "   container.id = 'customViewerLabel';" +
                        "   container.style.display = 'flex';" +
                        "   container.style.alignItems = 'center';" +
                        "   container.style.gap = '4px';" +
                        "   container.style.marginBottom = '2px';" +
                        "   " +
                        "   var icon = document.createElement('span');" +
                        "   icon.innerText = '👁️';" +
                        "   icon.style.fontSize = '12px';" +
                        "   " +
                        "   var lbl = document.createElement('span');" +
                        "   lbl.innerText = 'İZLEYİCİ';" +
                        "   lbl.style.color = '#ff4d4d';" + // Kırmızı renk[cite: 16]
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
                };

                if (webViewViewer.CoreWebView2 != null && !string.IsNullOrEmpty(url))
                {
                    webViewViewer.CoreWebView2.Navigate(url);
                }
            }
            catch { }
        }

        public void UpdateConfiguration(AppConfig config)
        {
            currentConfig = config;
            UpdateStyle(config);
            this.Location = new Point(config.ViewerX, config.ViewerY);
            this.Size = new Size(config.ViewerWidth, config.ViewerHeight);
            this.Visible = config.ShowViewerCount; 

            if (webViewViewer != null)
            {
                webViewViewer.ZoomFactor = config.ViewerZoom;

                if (webViewViewer.CoreWebView2 != null && !string.IsNullOrEmpty(config.ViewerUrl))
                {
                    string currentSource = webViewViewer.Source?.ToString() ?? "";
                    if (currentSource != config.ViewerUrl)
                    {
                        webViewViewer.CoreWebView2.Navigate(config.ViewerUrl);
                    }
                }
            }
        }

        // Form kapatıldığında WebView2 işlemlerini sonlandırarak arka plan CPU tüketimini engeller
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                if (webViewViewer != null && !webViewViewer.IsDisposed)
                {
                    webViewViewer.Dispose();
                }
            }
            catch { }
            base.OnFormClosed(e);
        }
    }
}