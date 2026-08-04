using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
// Çakışmaları önlemek için alias tanımları:
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Color = System.Drawing.Color;

namespace tiktok_chat_levach
{
    public partial class Form1 : Form
    {
        public AppConfig currentConfig;
        private ToolStripMenuItem settingsMenuItem;
        private ToolStripMenuItem openMenuItem;
        private ToolStripMenuItem editUrlsMenuItem;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        private Panel mainContainer;

        public WidgetForm widgetForm;
        public ViewerForm viewerForm;
        public GiftFeedForm giftFeedForm;
        public ChatWebForm chatWebForm;
        public LatestFollowerForm latestFollowerForm;

        private Panel loginPanel;
        private TextBox txtOverlay1;
        private TextBox txtViewerUrl;
        private TextBox txtGiftFeedUrl;
        private TextBox txtChatWebUrl;
        private TextBox txtLatestFollowerUrl;
        private Label lblStatus;
        private Button btnResetUrl;

        private Panel pnlLoginPreviewContainer;
        private WebView2 webViewOverlay1;
        private WebView2 webViewViewer;
        private WebView2 webViewGiftFeed;
        private WebView2 webViewChatWeb;
        private WebView2 webViewLatestFollower;

        private Label lblPreviewHeaderOverlay1;
        private Label lblPreviewHeaderViewer;
        private Label lblPreviewHeaderGiftFeed;
        private Label lblPreviewHeaderChatWeb;
        private Label lblPreviewHeaderLatestFollower;

        private Point mouseLocation;
        private bool isConnected = false;
        private bool isChatMode = false;
        private bool isIntentionalDisconnect = false;

        private CoreWebView2Environment sharedWebViewEnvironment = null;

        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WM_NCHITTEST = 0x84;
        private const int HTTRANSPARENT = -1;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (isChatMode)
                {
                    cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW;
                }
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && isChatMode)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                // Simge durumuna geçtiğinde WebView görsel işlemlerini durdurarak CPU'yu rahatlat
                SetWebViewsVisibility(false);
                this.WindowState = FormWindowState.Normal;
                this.Hide();
            }
            UpdateTrayMenuStates();
        }

        public Form1()
        {
            currentConfig = ConfigManager.Load();

            // CPU önceliğini otomatik olarak düşürerek oyunlarda kasma yaşanmasını engeller
            try
            {
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            }
            catch { }

            InitializeComponent();
            InitializeComponentCustom();
            SetupTrayIcon();

            if (!currentConfig.HideInfoOnStartup)
            {
                this.Shown += (s, e) => {
                    InfoDialog infoForm = new InfoDialog(currentConfig);
                    infoForm.ShowDialog(this);
                };
            }
        }

        private async Task<CoreWebView2Environment> GetOrCreateWebViewEnvironmentAsync()
        {
            if (sharedWebViewEnvironment == null)
            {
                try
                {
                    // CPU yükünü ve kaynak tüketimini minimuma indiren optimize edilmiş Chromium argümanları
                    string browserArgs = "--disable-extensions --disable-component-update --disable-background-networking --disable-sync --metrics-recording-only --disable-translate --disable-features=TranslateUI,MediaSessionService,CalculateNativeWinOcclusion --disable-ipc-flooding-protection --disable-hang-monitor --disable-prompt-on-repost --no-pings --enable-low-end-device-mode";
                    var options = new CoreWebView2EnvironmentOptions(browserArgs);
                    sharedWebViewEnvironment = await CoreWebView2Environment.CreateAsync(null, null, options);
                }
                catch
                {
                    sharedWebViewEnvironment = null;
                }
            }
            return sharedWebViewEnvironment;
        }

        private void OptimizeWebViewSettings(WebView2 webView)
        {
            if (webView?.CoreWebView2 != null)
            {
                webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = false;
            }
        }

        private void InitializeComponentCustom()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(18, 18, 24);
            this.TransparencyKey = Color.Empty;
            this.TopMost = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(440, 650);
            this.DoubleBuffered = true;

            this.Paint += (s, e) =>
            {
                if (!isChatMode)
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen penOuter = new Pen(Color.FromArgb(0, 120, 215), 2))
                    {
                        e.Graphics.DrawRectangle(penOuter, 0, 0, this.Width - 1, this.Height - 1);
                    }
                    using (Pen penInner = new Pen(Color.FromArgb(40, 160, 255), 1))
                    {
                        e.Graphics.DrawRectangle(penInner, 1, 1, this.Width - 3, this.Height - 3);
                    }
                }
            };

            Panel pnlTitleBar = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(24, 24, 32)
            };

            pnlTitleBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) mouseLocation = e.Location; };
            pnlTitleBar.MouseMove += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    this.Location = new Point(this.Location.X + (e.X - mouseLocation.X), this.Location.Y + (e.Y - mouseLocation.Y));
                }
            };

            Label lblAppTitle = new Label()
            {
                Text = "TIKTOK CHAT OVERLAY",
                ForeColor = Color.FromArgb(0, 180, 255),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(12, 9),
                AutoSize = true
            };
            pnlTitleBar.Controls.Add(lblAppTitle);

            Button btnCloseWin = new Button()
            {
                Text = "✕",
                Size = new Size(36, 36),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9F)
            };
            btnCloseWin.FlatAppearance.BorderSize = 0;
            btnCloseWin.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 50, 50);
            btnCloseWin.Click += (s, e) => {
                try { trayIcon.Visible = false; } catch { }
                Application.Exit();
            };
            pnlTitleBar.Controls.Add(btnCloseWin);

            Button btnMinimize = new Button()
            {
                Text = "🗕",
                Size = new Size(36, 36),
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9F)
            };
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 60);
            btnMinimize.Click += (s, e) => {
                SetWebViewsVisibility(false);
                this.Hide();
                UpdateTrayMenuStates();
            };
            pnlTitleBar.Controls.Add(btnMinimize);

            this.Controls.Add(pnlTitleBar);

            pnlLoginPreviewContainer = new Panel()
            {
                Location = new Point(12, 45),
                Size = new Size(410, 595),
                BackColor = Color.FromArgb(22, 22, 28),
                Visible = false,
                AutoScroll = true
            };

            Panel pnlInnerPreviewContent = new Panel()
            {
                Location = new Point(0, 0),
                Size = new Size(390, 660),
                BackColor = Color.Transparent
            };

            lblPreviewHeaderOverlay1 = new Label()
            {
                Text = "🌐 Overlay 1 Önizleme",
                ForeColor = Color.MediumOrchid,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(10, 6),
                AutoSize = true
            };
            pnlInnerPreviewContent.Controls.Add(lblPreviewHeaderOverlay1);

            webViewOverlay1 = new WebView2()
            {
                Location = new Point(10, 24),
                Size = new Size(375, 105)
            };
            pnlInnerPreviewContent.Controls.Add(webViewOverlay1);

            lblPreviewHeaderViewer = new Label()
            {
                Text = "🌐 Yayın İzleyici Önizleme",
                ForeColor = Color.Cyan,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(10, 134),
                AutoSize = true
            };
            pnlInnerPreviewContent.Controls.Add(lblPreviewHeaderViewer);

            webViewViewer = new WebView2()
            {
                Location = new Point(10, 152),
                Size = new Size(375, 105)
            };
            pnlInnerPreviewContent.Controls.Add(webViewViewer);

            lblPreviewHeaderGiftFeed = new Label()
            {
                Text = "🌐 Hediye Beslemesi Önizleme",
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(10, 262),
                AutoSize = true
            };
            pnlInnerPreviewContent.Controls.Add(lblPreviewHeaderGiftFeed);

            webViewGiftFeed = new WebView2()
            {
                Location = new Point(10, 280),
                Size = new Size(375, 105)
            };
            pnlInnerPreviewContent.Controls.Add(webViewGiftFeed);

            lblPreviewHeaderChatWeb = new Label()
            {
                Text = "🌐 Sohbet Web Önizleme",
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(10, 390),
                AutoSize = true
            };
            pnlInnerPreviewContent.Controls.Add(lblPreviewHeaderChatWeb);

            webViewChatWeb = new WebView2()
            {
                Location = new Point(10, 408),
                Size = new Size(375, 105)
            };
            pnlInnerPreviewContent.Controls.Add(webViewChatWeb);

            lblPreviewHeaderLatestFollower = new Label()
            {
                Text = "🌐 Son Takipçi Önizleme",
                ForeColor = Color.OrangeRed,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Location = new Point(10, 518),
                AutoSize = true
            };
            pnlInnerPreviewContent.Controls.Add(lblPreviewHeaderLatestFollower);

            webViewLatestFollower = new WebView2()
            {
                Location = new Point(10, 536),
                Size = new Size(375, 105)
            };
            pnlInnerPreviewContent.Controls.Add(webViewLatestFollower);

            pnlLoginPreviewContainer.Controls.Add(pnlInnerPreviewContent);
            this.Controls.Add(pnlLoginPreviewContainer);

            loginPanel = new Panel()
            {
                Location = new Point(12, 45),
                Size = new Size(415, 595),
                BackColor = Color.Transparent
            };

            Label lblTitle = new Label()
            {
                Text = "OVERLAY URL GİRİŞİ",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Location = new Point(15, 10),
                AutoSize = true
            };
            loginPanel.Controls.Add(lblTitle);

            Panel lineHeader = new Panel() { Location = new Point(15, 35), Size = new Size(380, 2), BackColor = Color.FromArgb(0, 120, 215) };
            loginPanel.Controls.Add(lineHeader);

            Label lblOverlay1Desc = new Label()
            {
                Text = "Beğeni / Sıralama OVERLAY URL",
                ForeColor = Color.DarkGray,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(15, 45),
                AutoSize = true
            };
            loginPanel.Controls.Add(lblOverlay1Desc);

            txtOverlay1 = new TextBox()
            {
                Location = new Point(15, 66),
                Size = new Size(380, 28),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(32, 32, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = currentConfig.TikFinityUrl ?? ""
            };
            txtOverlay1.TextChanged += (s, e) => { UpdateLoginPreview(); UpdateTrayMenuStates(); };
            loginPanel.Controls.Add(txtOverlay1);

            Label lblViewerUrlDesc = new Label()
            {
                Text = "YAYIN İZLEYİCİ OVERLAY URL",
                ForeColor = Color.DarkGray,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(15, 100),
                AutoSize = true
            };
            loginPanel.Controls.Add(lblViewerUrlDesc);

            txtViewerUrl = new TextBox()
            {
                Location = new Point(15, 121),
                Size = new Size(380, 28),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(32, 32, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = currentConfig.ViewerUrl ?? ""
            };
            txtViewerUrl.TextChanged += (s, e) => { UpdateLoginPreview(); UpdateTrayMenuStates(); };
            loginPanel.Controls.Add(txtViewerUrl);

            Label lblGiftFeedDesc = new Label()
            {
                Text = "HEDİYE BESLEMESİ OVERLAY URL",
                ForeColor = Color.DarkGray,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(15, 155),
                AutoSize = true
            };
            loginPanel.Controls.Add(lblGiftFeedDesc);

            txtGiftFeedUrl = new TextBox()
            {
                Location = new Point(15, 176),
                Size = new Size(380, 28),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(32, 32, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = currentConfig.GiftFeedUrl ?? ""
            };
            txtGiftFeedUrl.TextChanged += (s, e) => { UpdateLoginPreview(); UpdateTrayMenuStates(); };
            loginPanel.Controls.Add(txtGiftFeedUrl);

            Label lblChatWebDesc = new Label()
            {
                Text = "SOHBET OVERLAY URL",
                ForeColor = Color.DarkGray,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(15, 210),
                AutoSize = true
            };
            loginPanel.Controls.Add(lblChatWebDesc);

            txtChatWebUrl = new TextBox()
            {
                Location = new Point(15, 231),
                Size = new Size(380, 28),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(32, 32, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = currentConfig.ChatWebUrl ?? ""
            };
            txtChatWebUrl.TextChanged += (s, e) => { UpdateLoginPreview(); UpdateTrayMenuStates(); };
            loginPanel.Controls.Add(txtChatWebUrl);

            Label lblLatestFollowerDesc = new Label()
            {
                Text = "SON TAKİPÇİ OVERLAY URL",
                ForeColor = Color.DarkGray,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(15, 265),
                AutoSize = true
            };
            loginPanel.Controls.Add(lblLatestFollowerDesc);

            txtLatestFollowerUrl = new TextBox()
            {
                Location = new Point(15, 286),
                Size = new Size(380, 28),
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.FromArgb(32, 32, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = currentConfig.LatestFollowerUrl ?? ""
            };
            txtLatestFollowerUrl.TextChanged += (s, e) => { UpdateLoginPreview(); UpdateTrayMenuStates(); };
            loginPanel.Controls.Add(txtLatestFollowerUrl);

            lblStatus = new Label()
            {
                Text = "💡 Bilgi: En az bir URL doldurarak 'BAŞLAT' butonuna tıklayabilirsiniz.",
                ForeColor = Color.FromArgb(170, 170, 180),
                Font = new Font("Segoe UI", 7.8F, FontStyle.Regular),
                Location = new Point(15, 322),
                Size = new Size(380, 35)
            };
            loginPanel.Controls.Add(lblStatus);

            Button btnStart = new Button()
            {
                Text = "BAŞLAT",
                Location = new Point(15, 365),
                Size = new Size(182, 38),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnStart.Click += (s, e) => StartConnection();
            loginPanel.Controls.Add(btnStart);

            btnResetUrl = new Button()
            {
                Text = "URL'Yİ SIFIRLA",
                Location = new Point(207, 365),
                Size = new Size(182, 38),
                BackColor = Color.FromArgb(180, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnResetUrl.Click += (s, e) => {
                txtOverlay1.Text = "";
                txtViewerUrl.Text = "";
                txtGiftFeedUrl.Text = "";
                txtChatWebUrl.Text = "";
                txtLatestFollowerUrl.Text = "";
                currentConfig.TikFinityUrl = "";
                currentConfig.ViewerUrl = "";
                currentConfig.GiftFeedUrl = "";
                currentConfig.ChatWebUrl = "";
                currentConfig.LatestFollowerUrl = "";
                ConfigManager.Save(currentConfig);
                UpdateLoginPreview();
                UpdateTrayMenuStates();
            };
            loginPanel.Controls.Add(btnResetUrl);

            LinkLabel lnkDev = new LinkLabel()
            {
                Text = "tiktok/lev.qd",
                LinkColor = Color.FromArgb(0, 180, 255),
                ActiveLinkColor = Color.Cyan,
                VisitedLinkColor = Color.FromArgb(0, 150, 220),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(15, 415),
                AutoSize = true
            };
            lnkDev.LinkClicked += (s, e) => {
                try { Process.Start(new ProcessStartInfo("https://www.tiktok.com/@lev.qd") { UseShellExecute = true }); } catch { }
            };
            loginPanel.Controls.Add(lnkDev);

            Label lblHelpDev = new Label()
            {
                Text = "GELİŞTİRİCİYE YUKARDAKİ TİKTOK URL'Sİne TIKLAYARAK ULAŞABİLİRSİNİZ..",
                ForeColor = Color.DarkGray,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                Location = new Point(15, 437),
                AutoSize = true
            };
            loginPanel.Controls.Add(lblHelpDev);

            this.Controls.Add(loginPanel);

            mainContainer = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Visible = false,
                Padding = new Padding(0)
            };

            this.Controls.Add(mainContainer);

            // Viewer ve Son Takipçi için başlık enjeksiyon olayları
            SetupWebViewLabelInjections();

            UpdateLoginPreview();
            UpdateTrayMenuStates();
        }


        // Arka plandaki WebView bileşenlerinin render yükünü tamamen kesen metot
        private void SetWebViewsVisibility(bool isVisible)
        {
            if (webViewOverlay1 != null) webViewOverlay1.Visible = isVisible;
            if (webViewViewer != null) webViewViewer.Visible = isVisible;
            if (webViewGiftFeed != null) webViewGiftFeed.Visible = isVisible;
            if (webViewChatWeb != null) webViewChatWeb.Visible = isVisible;
            if (webViewLatestFollower != null) webViewLatestFollower.Visible = isVisible;
        }

        private void SetupWebViewLabelInjections()
        {
            webViewViewer.CoreWebView2InitializationCompleted += async (s, e) =>
            {
                if (webViewViewer.CoreWebView2 != null)
                {
                    OptimizeWebViewSettings(webViewViewer);
                    await OptimizeWebViewAnimations(webViewViewer); // CPU animasyon tasarrufu
                    webViewViewer.CoreWebView2.NavigationCompleted += async (sender, args) =>
                    {
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
                            "   var icon = document.createElement('span');" +
                            "   icon.innerText = '👁️';" +
                            "   icon.style.fontSize = '12px';" +
                            "   var lbl = document.createElement('span');" +
                            "   lbl.innerText = 'İZLEYİCİ';" +
                            "   lbl.style.color = '#ff4d4d';" +
                            "   lbl.style.fontSize = '11px';" +
                            "   lbl.style.fontWeight = 'bold';" +
                            "   lbl.style.fontFamily = 'Segoe UI, sans-serif';" +
                            "   lbl.style.textShadow = '1px 1px 2px black';" +
                            "   container.appendChild(icon);" +
                            "   container.appendChild(lbl);" +
                            "   document.body.insertBefore(container, document.body.firstChild);" +
                            "}"
                        );
                    };
                }
            };

            webViewLatestFollower.CoreWebView2InitializationCompleted += async (s, e) =>
            {
                if (webViewLatestFollower.CoreWebView2 != null)
                {
                    OptimizeWebViewSettings(webViewLatestFollower);
                    await OptimizeWebViewAnimations(webViewLatestFollower); // CPU animasyon tasarrufu
                    webViewLatestFollower.CoreWebView2.NavigationCompleted += async (sender, args) =>
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
                            "   var icon = document.createElement('span');" +
                            "   icon.innerText = '👤';" +
                            "   icon.style.fontSize = '12px';" +
                            "   var lbl = document.createElement('span');" +
                            "   lbl.innerText = 'SON TAKİPÇİ';" +
                            "   lbl.style.color = '#0078d4';" +
                            "   lbl.style.fontSize = '11px';" +
                            "   lbl.style.fontWeight = 'bold';" +
                            "   lbl.style.fontFamily = 'Segoe UI, sans-serif';" +
                            "   lbl.style.textShadow = '1px 1px 2px black';" +
                            "   container.appendChild(icon);" +
                            "   container.appendChild(lbl);" +
                            "   document.body.insertBefore(container, document.body.firstChild);" +
                            "}"
                        );
                    };
                }
            };

            webViewOverlay1.CoreWebView2InitializationCompleted += async (s, e) => { OptimizeWebViewSettings(webViewOverlay1); await OptimizeWebViewAnimations(webViewOverlay1); };
            webViewGiftFeed.CoreWebView2InitializationCompleted += async (s, e) => { OptimizeWebViewSettings(webViewGiftFeed); await OptimizeWebViewAnimations(webViewGiftFeed); };
            webViewChatWeb.CoreWebView2InitializationCompleted += async (s, e) => { OptimizeWebViewSettings(webViewChatWeb); await OptimizeWebViewAnimations(webViewChatWeb); };
        }

        private async Task OptimizeWebViewAnimations(WebView2 webView)
        {
            if (webView?.CoreWebView2 != null)
            {
                await webView.CoreWebView2.ExecuteScriptAsync(
                    "var style = document.createElement('style');" +
                    "style.innerHTML = '* { animation-duration: 0s !important; transition-duration: 0s !important; }';" +
                    "document.head.appendChild(style);"
                );
            }
        }

        private async void UpdateLoginPreview()
        {
            string overlay1 = txtOverlay1 != null ? txtOverlay1.Text.Trim() : "";
            string viewer = txtViewerUrl != null ? txtViewerUrl.Text.Trim() : "";
            string giftFeed = txtGiftFeedUrl != null ? txtGiftFeedUrl.Text.Trim() : "";
            string chatWeb = txtChatWebUrl != null ? txtChatWebUrl.Text.Trim() : "";
            string latestFollower = txtLatestFollowerUrl != null ? txtLatestFollowerUrl.Text.Trim() : "";

            bool hasOverlay1 = !string.IsNullOrEmpty(overlay1) && overlay1.StartsWith("http");
            bool hasViewer = !string.IsNullOrEmpty(viewer) && viewer.StartsWith("http");
            bool hasGiftFeed = !string.IsNullOrEmpty(giftFeed) && giftFeed.StartsWith("http");
            bool hasChatWeb = !string.IsNullOrEmpty(chatWeb) && chatWeb.StartsWith("http");
            bool hasLatestFollower = !string.IsNullOrEmpty(latestFollower) && latestFollower.StartsWith("http");

            if (hasOverlay1 || hasViewer || hasGiftFeed || hasChatWeb || hasLatestFollower)
            {
                if (pnlLoginPreviewContainer != null) pnlLoginPreviewContainer.Visible = true;
                if (loginPanel != null) loginPanel.Location = new Point(435, 45);
                this.Size = new Size(860, 690);
                this.CenterToScreen();

                lblPreviewHeaderOverlay1.Visible = hasOverlay1;
                webViewOverlay1.Visible = hasOverlay1;

                lblPreviewHeaderViewer.Visible = hasViewer;
                webViewViewer.Visible = hasViewer;

                lblPreviewHeaderGiftFeed.Visible = hasGiftFeed;
                webViewGiftFeed.Visible = hasGiftFeed;

                lblPreviewHeaderChatWeb.Visible = hasChatWeb;
                webViewChatWeb.Visible = hasChatWeb;

                lblPreviewHeaderLatestFollower.Visible = hasLatestFollower;
                webViewLatestFollower.Visible = hasLatestFollower;

                try
                {
                    var env = await GetOrCreateWebViewEnvironmentAsync();

                    if (hasOverlay1)
                    {
                        if (webViewOverlay1.CoreWebView2 == null)
                        {
                            if (env != null) await webViewOverlay1.EnsureCoreWebView2Async(env);
                            else await webViewOverlay1.EnsureCoreWebView2Async(null);
                        }
                        webViewOverlay1.CoreWebView2.Navigate(overlay1);
                    }
                    if (hasViewer)
                    {
                        if (webViewViewer.CoreWebView2 == null)
                        {
                            if (env != null) await webViewViewer.EnsureCoreWebView2Async(env);
                            else await webViewViewer.EnsureCoreWebView2Async(null);
                        }
                        webViewViewer.CoreWebView2.Navigate(viewer);
                    }
                    if (hasGiftFeed)
                    {
                        if (webViewGiftFeed.CoreWebView2 == null)
                        {
                            if (env != null) await webViewGiftFeed.EnsureCoreWebView2Async(env);
                            else await webViewGiftFeed.EnsureCoreWebView2Async(null);
                        }
                        webViewGiftFeed.CoreWebView2.Navigate(giftFeed);
                    }
                    if (hasChatWeb)
                    {
                        if (webViewChatWeb.CoreWebView2 == null)
                        {
                            if (env != null) await webViewChatWeb.EnsureCoreWebView2Async(env);
                            else await webViewChatWeb.EnsureCoreWebView2Async(null);
                        }
                        webViewChatWeb.CoreWebView2.Navigate(chatWeb);
                    }
                    if (hasLatestFollower)
                    {
                        if (webViewLatestFollower.CoreWebView2 == null)
                        {
                            if (env != null) await webViewLatestFollower.EnsureCoreWebView2Async(env);
                            else await webViewLatestFollower.EnsureCoreWebView2Async(null);
                        }
                        webViewLatestFollower.CoreWebView2.Navigate(latestFollower);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Preview Error: " + ex.Message);
                }
            }
            else
            {
                if (pnlLoginPreviewContainer != null) pnlLoginPreviewContainer.Visible = false;
                if (loginPanel != null) loginPanel.Location = new Point(12, 45);
                this.Size = new Size(440, 650);
                this.CenterToScreen();
            }
        }

        private void SetupTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            trayMenu.BackColor = Color.FromArgb(30, 30, 30);
            trayMenu.ForeColor = Color.White;
            trayMenu.Font = new Font("Segoe UI", 9, FontStyle.Regular);

            openMenuItem = new ToolStripMenuItem("Aç", null, (s, e) => {
                if (openMenuItem.Enabled)
                {
                    this.Show();
                    SetWebViewsVisibility(true); // Görünür olduğunda tekrar aktif et
                    this.Activate();
                    UpdateTrayMenuStates();
                }
            });
            trayMenu.Items.Add(openMenuItem);

            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Göster", null, (s, e) => ToggleFormVisibility(true));
            trayMenu.Items.Add("Gizle", null, (s, e) => ToggleFormVisibility(false));
            trayMenu.Items.Add(new ToolStripSeparator());

            settingsMenuItem = new ToolStripMenuItem("Ayarlar", null, (s, e) => OpenSettings());
            settingsMenuItem.Enabled = false;
            trayMenu.Items.Add(settingsMenuItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            editUrlsMenuItem = new ToolStripMenuItem("URL'leri Düzenle", null, (s, e) => {
                if (editUrlsMenuItem.Enabled)
                {
                    ChangeBroadcaster();
                }
            });
            trayMenu.Items.Add(editUrlsMenuItem);

            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Kapat", null, (s, e) => {
                isIntentionalDisconnect = true;
                try { trayIcon.Visible = false; } catch { }
                try { widgetForm?.Close(); } catch { }
                try { viewerForm?.Close(); } catch { }
                try { giftFeedForm?.Close(); } catch { }
                try { chatWebForm?.Close(); } catch { }
                try { latestFollowerForm?.Close(); } catch { }
                Application.Exit();
            });

            Icon trayAppIcon = SystemIcons.Application;
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "tiktok.png");

            if (File.Exists(iconPath))
            {
                try
                {
                    using (var bmp = new Bitmap(iconPath))
                    {
                        IntPtr hIcon = bmp.GetHicon();
                        trayAppIcon = Icon.FromHandle(hIcon);
                    }
                }
                catch { }
            }

            trayIcon = new NotifyIcon()
            {
                Text = "TikTok Chat Panel",
                Icon = trayAppIcon,
                ContextMenuStrip = trayMenu,
                Visible = true
            };

            trayIcon.DoubleClick += (s, e) =>
            {
                if (openMenuItem != null && openMenuItem.Enabled)
                {
                    this.Show();
                    SetWebViewsVisibility(true);
                    this.Activate();
                    UpdateTrayMenuStates();
                }
            };

            UpdateTrayMenuStates();
        }

        private void UpdateTrayMenuStates()
        {
            bool isHidden = !this.Visible;

            if (openMenuItem != null)
            {
                openMenuItem.Enabled = isHidden;
            }

            if (editUrlsMenuItem != null)
            {
                editUrlsMenuItem.Enabled = !isHidden;
            }

            if (btnResetUrl != null)
            {
                bool hasUrls = !string.IsNullOrEmpty(txtOverlay1?.Text) ||
                               !string.IsNullOrEmpty(txtViewerUrl?.Text) ||
                               !string.IsNullOrEmpty(txtGiftFeedUrl?.Text) ||
                               !string.IsNullOrEmpty(txtChatWebUrl?.Text) ||
                               !string.IsNullOrEmpty(txtLatestFollowerUrl?.Text) ||
                               !string.IsNullOrEmpty(currentConfig.TikFinityUrl) ||
                               !string.IsNullOrEmpty(currentConfig.ViewerUrl) ||
                               !string.IsNullOrEmpty(currentConfig.GiftFeedUrl) ||
                               !string.IsNullOrEmpty(currentConfig.ChatWebUrl) ||
                               !string.IsNullOrEmpty(currentConfig.LatestFollowerUrl);

                btnResetUrl.Enabled = hasUrls;
            }
        }

        private void ToggleFormVisibility(bool show)
        {
            if (show)
            {
                this.Show();
                SetWebViewsVisibility(true);
                this.ShowInTaskbar = false;
                if (widgetForm != null && !widgetForm.IsDisposed) widgetForm.Show();
                if (viewerForm != null && !viewerForm.IsDisposed) viewerForm.Show();
                if (giftFeedForm != null && !giftFeedForm.IsDisposed) giftFeedForm.Show();
                if (chatWebForm != null && !chatWebForm.IsDisposed) chatWebForm.Show();
                if (latestFollowerForm != null && !latestFollowerForm.IsDisposed) latestFollowerForm.Show();
                isConnected = true;
            }
            else
            {
                SetWebViewsVisibility(false);
                this.Hide();
                this.ShowInTaskbar = false;
                if (widgetForm != null && !widgetForm.IsDisposed) widgetForm.Hide();
                if (viewerForm != null && !viewerForm.IsDisposed) viewerForm.Hide();
                if (giftFeedForm != null && !giftFeedForm.IsDisposed) giftFeedForm.Hide();
                if (chatWebForm != null && !chatWebForm.IsDisposed) chatWebForm.Hide();
                if (latestFollowerForm != null && !latestFollowerForm.IsDisposed) latestFollowerForm.Hide();
                isConnected = false;
            }
            UpdateTrayMenuStates();
        }

        private void OpenSettings()
        {
            SettingsForm settingsForm = new SettingsForm(this);
            settingsForm.ShowDialog();
        }

        public void ApplySettings()
        {
            if (isChatMode)
            {
                this.Location = new Point(0, 0);

                if (webViewOverlay1 != null) { webViewOverlay1.Location = new Point(currentConfig.WidgetX, currentConfig.WidgetY); webViewOverlay1.Size = new Size(currentConfig.WidgetWidth, currentConfig.WidgetHeight); webViewOverlay1.ZoomFactor = currentConfig.WidgetZoom; }
                if (webViewViewer != null) { webViewViewer.Location = new Point(currentConfig.ViewerX, currentConfig.ViewerY); webViewViewer.Size = new Size(currentConfig.ViewerWidth, currentConfig.ViewerHeight); webViewViewer.ZoomFactor = currentConfig.ViewerZoom; }
                if (webViewGiftFeed != null) { webViewGiftFeed.Location = new Point(currentConfig.GiftFeedX, currentConfig.GiftFeedY); webViewGiftFeed.Size = new Size(currentConfig.GiftFeedWidth, currentConfig.GiftFeedHeight); webViewGiftFeed.ZoomFactor = currentConfig.GiftFeedZoom; }
                if (webViewChatWeb != null) { webViewChatWeb.Location = new Point(currentConfig.ChatWebX, currentConfig.ChatWebY); webViewChatWeb.Size = new Size(currentConfig.ChatWebWidth, currentConfig.ChatWebHeight); webViewChatWeb.ZoomFactor = currentConfig.ChatWebZoom; }
                if (webViewLatestFollower != null) { webViewLatestFollower.Location = new Point(currentConfig.LatestFollowerX, currentConfig.LatestFollowerY); webViewLatestFollower.Size = new Size(currentConfig.LatestFollowerWidth, currentConfig.LatestFollowerHeight); webViewLatestFollower.ZoomFactor = currentConfig.LatestFollowerZoom; }
            }

            this.TopMost = currentConfig.TopMost;

            if (currentConfig.UseTransparentBackground)
            {
                this.BackColor = Color.Magenta;
                this.TransparencyKey = Color.Magenta;
            }
            else
            {
                this.TransparencyKey = Color.Empty;
                try
                {
                    this.BackColor = ColorTranslator.FromHtml(currentConfig.BackgroundColor);
                }
                catch
                {
                    this.BackColor = Color.FromArgb(18, 18, 20);
                }
            }

            this.Opacity = currentConfig.WindowOpacity;
            if (mainContainer != null) mainContainer.BackColor = Color.Transparent;
        }

        private void StartConnection()
        {
            string overlay1Url = txtOverlay1.Text.Trim();
            string viewerUrl = txtViewerUrl.Text.Trim();
            string giftFeedUrl = txtGiftFeedUrl.Text.Trim();
            string chatWebUrl = txtChatWebUrl.Text.Trim();
            string latestFollowerUrl = txtLatestFollowerUrl.Text.Trim();

            bool hasAnyUrl = !string.IsNullOrEmpty(overlay1Url) ||
                             !string.IsNullOrEmpty(viewerUrl) ||
                             !string.IsNullOrEmpty(giftFeedUrl) ||
                             !string.IsNullOrEmpty(chatWebUrl) ||
                             !string.IsNullOrEmpty(latestFollowerUrl);

            if (!hasAnyUrl)
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = "❌ Hata: Başlatmak için en az bir URL girmelisiniz!";
                return;
            }

            currentConfig.TikFinityUrl = overlay1Url;
            currentConfig.ViewerUrl = viewerUrl;
            currentConfig.GiftFeedUrl = giftFeedUrl;
            currentConfig.ChatWebUrl = chatWebUrl;
            currentConfig.LatestFollowerUrl = latestFollowerUrl;
            ConfigManager.Save(currentConfig);

            isIntentionalDisconnect = false;
            lblStatus.ForeColor = Color.LightGreen;
            lblStatus.Text = "🟢 Pencereler açılıyor...";

            TransitionToChatMode();
            UpdateTrayMenuStates();
        }

        private void ChangeBroadcaster()
        {
            isIntentionalDisconnect = true;
            if (settingsMenuItem != null) settingsMenuItem.Enabled = false;

            try { widgetForm?.Close(); } catch { }
            try { viewerForm?.Close(); } catch { }
            try { giftFeedForm?.Close(); } catch { }
            try { chatWebForm?.Close(); } catch { }
            try { latestFollowerForm?.Close(); } catch { }

            isChatMode = false;
            isConnected = false;

            mainContainer.Visible = false;
            loginPanel.Visible = true;

            this.TopMost = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.FromArgb(18, 18, 24);
            this.TransparencyKey = Color.Empty;
            this.Size = new Size(440, 650);
            this.CenterToScreen();

            txtOverlay1.Enabled = true;
            txtViewerUrl.Enabled = true;
            txtGiftFeedUrl.Enabled = true;
            txtChatWebUrl.Enabled = true;
            txtLatestFollowerUrl.Enabled = true;
            lblStatus.ForeColor = Color.FromArgb(170, 170, 180);
            lblStatus.Text = "💡 Bilgi: En az bir URL doldurarak 'BAŞLAT' butonuna tıklayabilirsiniz.";

            UpdateLoginPreview();
            this.Show();
            SetWebViewsVisibility(true);
            try { this.RecreateHandle(); } catch { }
            isIntentionalDisconnect = false;
            UpdateTrayMenuStates();
        }

        private async void MakeWebViewsTransparent()
        {
            Microsoft.Web.WebView2.WinForms.WebView2[] webViews = {
                webViewOverlay1,
                webViewViewer,
                webViewGiftFeed,
                webViewChatWeb,
                webViewLatestFollower
            };

            var env = await GetOrCreateWebViewEnvironmentAsync();

            foreach (var wv in webViews)
            {
                if (wv != null)
                {
                    try
                    {
                        if (wv.CoreWebView2 == null)
                        {
                            if (env != null) await wv.EnsureCoreWebView2Async(env);
                            else await wv.EnsureCoreWebView2Async(null);
                        }
                        wv.DefaultBackgroundColor = Color.Transparent;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("WebView şeffaflık hatası: " + ex.Message);
                    }
                }
            }
        }

        private void TransitionToChatMode()
        {
            loginPanel.Visible = false;
            if (pnlLoginPreviewContainer != null) pnlLoginPreviewContainer.Visible = false;
            foreach (Control c in this.Controls)
            {
                if (c is Panel && c != mainContainer) c.Visible = false;
            }

            mainContainer.Visible = true;
            isChatMode = true;
            this.TopMost = currentConfig.TopMost;

            if (settingsMenuItem != null) settingsMenuItem.Enabled = true;

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);

            int maxRight = 400;
            int maxBottom = 600;

            if (!string.IsNullOrEmpty(currentConfig.TikFinityUrl))
            {
                maxRight = Math.Max(maxRight, currentConfig.WidgetX + currentConfig.WidgetWidth);
                maxBottom = Math.Max(maxBottom, currentConfig.WidgetY + currentConfig.WidgetHeight);
            }
            if (!string.IsNullOrEmpty(currentConfig.ViewerUrl))
            {
                maxRight = Math.Max(maxRight, currentConfig.ViewerX + currentConfig.ViewerWidth);
                maxBottom = Math.Max(maxBottom, currentConfig.ViewerY + currentConfig.ViewerHeight);
            }
            if (!string.IsNullOrEmpty(currentConfig.GiftFeedUrl))
            {
                maxRight = Math.Max(maxRight, currentConfig.GiftFeedX + currentConfig.GiftFeedWidth);
                maxBottom = Math.Max(maxBottom, currentConfig.GiftFeedY + currentConfig.GiftFeedHeight);
            }
            if (!string.IsNullOrEmpty(currentConfig.ChatWebUrl))
            {
                maxRight = Math.Max(maxRight, currentConfig.ChatWebX + currentConfig.ChatWebWidth);
                maxBottom = Math.Max(maxBottom, currentConfig.ChatWebY + currentConfig.ChatWebHeight);
            }
            if (!string.IsNullOrEmpty(currentConfig.LatestFollowerUrl))
            {
                maxRight = Math.Max(maxRight, currentConfig.LatestFollowerX + currentConfig.LatestFollowerWidth);
                maxBottom = Math.Max(maxBottom, currentConfig.LatestFollowerY + currentConfig.LatestFollowerHeight);
            }

            this.Size = new Size(maxRight, maxBottom);

            mainContainer.AutoScroll = false;
            mainContainer.Controls.Clear();
            mainContainer.Dock = DockStyle.Fill;

            if (!string.IsNullOrEmpty(currentConfig.TikFinityUrl))
            {
                webViewOverlay1.Location = new Point(currentConfig.WidgetX, currentConfig.WidgetY);
                webViewOverlay1.Size = new Size(currentConfig.WidgetWidth, currentConfig.WidgetHeight);
                webViewOverlay1.ZoomFactor = currentConfig.WidgetZoom;
                mainContainer.Controls.Add(webViewOverlay1);
            }

            if (!string.IsNullOrEmpty(currentConfig.ViewerUrl))
            {
                webViewViewer.Location = new Point(currentConfig.ViewerX, currentConfig.ViewerY);
                webViewViewer.Size = new Size(currentConfig.ViewerWidth, currentConfig.ViewerHeight);
                webViewViewer.ZoomFactor = currentConfig.ViewerZoom;
                mainContainer.Controls.Add(webViewViewer);
            }

            if (!string.IsNullOrEmpty(currentConfig.GiftFeedUrl))
            {
                webViewGiftFeed.Location = new Point(currentConfig.GiftFeedX, currentConfig.GiftFeedY);
                webViewGiftFeed.Size = new Size(currentConfig.GiftFeedWidth, currentConfig.GiftFeedHeight);
                webViewGiftFeed.ZoomFactor = currentConfig.GiftFeedZoom;
                mainContainer.Controls.Add(webViewGiftFeed);
            }

            if (!string.IsNullOrEmpty(currentConfig.ChatWebUrl))
            {
                webViewChatWeb.Location = new Point(currentConfig.ChatWebX, currentConfig.ChatWebY);
                webViewChatWeb.Size = new Size(currentConfig.ChatWebWidth, currentConfig.ChatWebHeight);
                webViewChatWeb.ZoomFactor = currentConfig.ChatWebZoom;
                mainContainer.Controls.Add(webViewChatWeb);
            }

            if (!string.IsNullOrEmpty(currentConfig.LatestFollowerUrl))
            {
                webViewLatestFollower.Location = new Point(currentConfig.LatestFollowerX, currentConfig.LatestFollowerY);
                webViewLatestFollower.Size = new Size(currentConfig.LatestFollowerWidth, currentConfig.LatestFollowerHeight);
                webViewLatestFollower.ZoomFactor = currentConfig.LatestFollowerZoom;
                mainContainer.Controls.Add(webViewLatestFollower);
            }

            MakeWebViewsTransparent();
            SetWebViewsVisibility(true);

            ApplySettings();
            try { this.RecreateHandle(); } catch { }
            UpdateTrayMenuStates();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            isIntentionalDisconnect = true;
            try { trayIcon.Visible = false; } catch { }
            trayIcon.Dispose();
            try { widgetForm?.Close(); } catch { }
            try { viewerForm?.Close(); } catch { }
            try { giftFeedForm?.Close(); } catch { }
            try { chatWebForm?.Close(); } catch { }
            try { latestFollowerForm?.Close(); } catch { }

            base.OnFormClosing(e);
        }
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(60, 60, 60);
        public override Color MenuItemBorder => Color.FromArgb(60, 60, 60);
        public override Color ToolStripDropDownBackground => Color.FromArgb(30, 30, 30);
        public override Color ImageMarginGradientBegin => Color.FromArgb(30, 30, 30);
        public override Color ImageMarginGradientEnd => Color.FromArgb(30, 30, 30);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(30, 30, 30);
        public override Color SeparatorDark => Color.Gray;
    }
}