using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace tiktok_chat_levach
{
    public class SettingsForm : Form
    {
        private dynamic mainForm;
        private AppConfig originalConfig;
        private AppConfig tempConfig;
        private bool isSaved = false;

        private Panel mainContentPanel;
        private Dictionary<string, Panel> pages = new Dictionary<string, Panel>();
        private Dictionary<string, Button> navButtons = new Dictionary<string, Button>();

        private TrackBar tbWidgetZoom;
        private TrackBar tbViewerZoom;
        private TrackBar tbGiftFeedZoom;
        private TrackBar tbChatWebZoom;
        private TrackBar tbLatestFollowerZoom;

        // Çakışmayı önlemek için tam namespace belirtildi
        private System.Windows.Forms.Timer previewDebounceTimer;

        public SettingsForm(Form mainForm)
        {
            this.mainForm = mainForm;
            originalConfig = ConfigManager.Load();
            tempConfig = CloneConfig(originalConfig);

            // Debounce Timer Tanımlaması
            previewDebounceTimer = new System.Windows.Forms.Timer();
            previewDebounceTimer.Interval = 100;
            previewDebounceTimer.Tick += (s, e) =>
            {
                previewDebounceTimer.Stop();
                ExecuteLivePreview();
            };

            InitializeModernUI();
            ApplyLivePreview();
        }

        private AppConfig CloneConfig(AppConfig source)
        {
            return new AppConfig
            {
                FormWidth = source.FormWidth,
                FormHeight = source.FormHeight,
                FormX = source.FormX,
                FormY = source.FormY,
                BroadcasterUsername = source.BroadcasterUsername,
                TopMost = source.TopMost,

                UseTransparentBackground = source.UseTransparentBackground,
                BackgroundColor = source.BackgroundColor,
                ThemeMode = source.ThemeMode,
                CornerRadius = source.CornerRadius,
                WindowOpacity = source.WindowOpacity,

                ViewerUrl = source.ViewerUrl,
                ViewerX = source.ViewerX,
                ViewerY = source.ViewerY,
                ViewerWidth = source.ViewerWidth,
                ViewerHeight = source.ViewerHeight,
                ViewerZoom = source.ViewerZoom,

                TikFinityUrl = source.TikFinityUrl,
                WidgetX = source.WidgetX,
                WidgetY = source.WidgetY,
                WidgetWidth = source.WidgetWidth,
                WidgetHeight = source.WidgetHeight,
                WidgetZoom = source.WidgetZoom,

                GiftFeedUrl = source.GiftFeedUrl,
                GiftFeedX = source.GiftFeedX,
                GiftFeedY = source.GiftFeedY,
                GiftFeedWidth = source.GiftFeedWidth,
                GiftFeedHeight = source.GiftFeedHeight,
                GiftFeedZoom = source.GiftFeedZoom,

                ChatWebUrl = source.ChatWebUrl,
                ChatWebX = source.ChatWebX,
                ChatWebY = source.ChatWebY,
                ChatWebWidth = source.ChatWebWidth,
                ChatWebHeight = source.ChatWebHeight,
                ChatWebZoom = source.ChatWebZoom,

                LatestFollowerUrl = source.LatestFollowerUrl,
                LatestFollowerX = source.LatestFollowerX,
                LatestFollowerY = source.LatestFollowerY,
                LatestFollowerWidth = source.LatestFollowerWidth,
                LatestFollowerHeight = source.LatestFollowerHeight,
                LatestFollowerZoom = source.LatestFollowerZoom
            };
        }

        private void InitializeModernUI()
        {
            this.Text = "TikTok Live - Ayarlar Merkezi";
            this.Size = new Size(1000, 720);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.FromArgb(240, 240, 240);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.ShowInTaskbar = false;

            Panel sidebar = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Color.FromArgb(24, 24, 24) };

            Label lblTitle = new Label { Text = "AYARLAR", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(120, 120, 120), Location = new Point(20, 20), AutoSize = true };
            sidebar.Controls.Add(lblTitle);

            Button btnGenel = CreateSidebarButton("⚙️  Genel & Görünüm", "Genel", 60);
            Button btnChatWeb = CreateSidebarButton("💬  Sohbet Web Alanı", "ChatWeb", 115);
            Button btnWidget = CreateSidebarButton("🧩  Widget Ayarları", "Widget", 170);
            Button btnViewer = CreateSidebarButton("🌐  Viewer Ayarları", "Viewer", 225);
            Button btnGiftFeed = CreateSidebarButton("🎁  Hediye Beslemesi", "GiftFeed", 280);
            Button btnLatestFollower = CreateSidebarButton("👤  Son Takipçi", "LatestFollower", 335);

            sidebar.Controls.Add(btnGenel);
            sidebar.Controls.Add(btnChatWeb);
            sidebar.Controls.Add(btnWidget);
            sidebar.Controls.Add(btnViewer);
            sidebar.Controls.Add(btnGiftFeed);
            sidebar.Controls.Add(btnLatestFollower);

            mainContentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(32, 32, 32), Padding = new Padding(15) };

            pages.Add("Genel", BuildGenelPage());
            pages.Add("ChatWeb", BuildChatWebPage());
            pages.Add("Widget", BuildWidgetPage());
            pages.Add("Viewer", BuildViewerPage());
            pages.Add("GiftFeed", BuildGiftFeedPage());
            pages.Add("LatestFollower", BuildLatestFollowerPage());

            foreach (var page in pages.Values)
            {
                page.Dock = DockStyle.Fill;
                page.Visible = false;
                mainContentPanel.Controls.Add(page);
            }

            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.FromArgb(24, 24, 24) };

            Button btnSave = CreateAccentButton("Değişiklikleri Kaydet", 620, 15, 170, 40);
            Button btnCancel = CreateSecondaryButton("Vazgeç", 805, 15, 150, 40);

            btnSave.Click += (s, e) =>
            {
                isSaved = true;
                tempConfig.FormX = mainForm.Location.X;
                tempConfig.FormY = mainForm.Location.Y;
                ConfigManager.Save(tempConfig);
                mainForm.currentConfig = tempConfig;
                mainForm.ApplySettings();
                MessageBox.Show("Ayarlar başarıyla kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            };
            btnCancel.Click += (s, e) => this.Close();

            bottomPanel.Controls.Add(btnSave);
            bottomPanel.Controls.Add(btnCancel);

            this.Controls.Add(mainContentPanel);
            this.Controls.Add(sidebar);
            this.Controls.Add(bottomPanel);

            SwitchPage("Genel");
        }

        private Panel BuildGenelPage()
        {
            FlowLayoutPanel pnl = CreateScrollablePage();
            pnl.Controls.Add(CreateHeader("Genel Pencere Ayarları"));

            pnl.Controls.Add(CreateCheckBox("Sohbet takip ve diğer pencereler ekran üzerinde yapışık dursun kapatmak için tiki kaldırın", tempConfig.TopMost, c => { tempConfig.TopMost = c; ApplyLivePreview(); }));

            pnl.Controls.Add(CreateHeader("Sistem ve Veri Yönetimi"));

            Button btnReset = CreateDangerButton("⚠️  Kişisel Ayarları Sıfırla (URL'ler Kalır)", 560, 40);
            btnReset.Margin = new Padding(0, 10, 0, 5);
            btnReset.Click += (s, e) =>
            {
                if (MessageBox.Show("Kişisel ayarlarınız sıfırlanacak ama eklenilen URL'ler gitmeyecek. Emin misiniz?", "Dikkat", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    string savedViewerUrl = tempConfig.ViewerUrl;
                    string savedTikFinityUrl = tempConfig.TikFinityUrl;
                    string savedGiftFeedUrl = tempConfig.GiftFeedUrl;
                    string savedChatWebUrl = tempConfig.ChatWebUrl;
                    string savedLatestFollowerUrl = tempConfig.LatestFollowerUrl;
                    string savedBroadcaster = tempConfig.BroadcasterUsername;

                    tempConfig = new AppConfig
                    {
                        ViewerUrl = savedViewerUrl,
                        TikFinityUrl = savedTikFinityUrl,
                        GiftFeedUrl = savedGiftFeedUrl,
                        ChatWebUrl = savedChatWebUrl,
                        LatestFollowerUrl = savedLatestFollowerUrl,
                        BroadcasterUsername = savedBroadcaster
                    };

                    ConfigManager.Save(tempConfig);
                    isSaved = true;
                    mainForm.currentConfig = tempConfig;
                    mainForm.ApplySettings();
                    MessageBox.Show("Kişisel ayarlarınız sıfırlandı, eklenen URL'ler korundu.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            };
            pnl.Controls.Add(btnReset);

            Label lblResetInfo = new Label
            {
                Text = "💡 Bilgi: Kişisel ayarlarınız sıfırlanır ama eklenilen url'ler gitmez.",
                ForeColor = Color.FromArgb(170, 170, 180),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
            };
            pnl.Controls.Add(lblResetInfo);

            return pnl;
        }

        private Panel BuildChatWebPage()
        {
            FlowLayoutPanel pnl = CreateScrollablePage();
            pnl.Controls.Add(CreateHeader("Sohbet Web Alanı Ayarları"));

            pnl.Controls.Add(CreateLabel("Sohbet Web URL Adresi"));
            TextBox txtChatWebUrl = new TextBox { Text = tempConfig.ChatWebUrl, Width = 560, Font = new Font("Segoe UI", 9.5F) };
            txtChatWebUrl.TextChanged += (s, e) => { tempConfig.ChatWebUrl = txtChatWebUrl.Text; ApplyLivePreview(); };
            pnl.Controls.Add(txtChatWebUrl);

            pnl.Controls.Add(CreateLabel("Sohbet Web Konumunu Ok Tuşlarıyla Kaydır"));
            Panel chatWebPosPanel = new Panel { Width = 260, Height = 95, BackColor = Color.FromArgb(40, 40, 40), Margin = new Padding(0, 5, 0, 15) };
            Button btnChatWebUp = CreateMiniButton("▲", 105, 10, 45, 35);
            Button btnChatWebDown = CreateMiniButton("▼", 105, 50, 45, 35);
            Button btnChatWebLeft = CreateMiniButton("◀", 55, 50, 45, 35);
            Button btnChatWebRight = CreateMiniButton("▶", 155, 50, 45, 35);

            btnChatWebUp.Click += (s, e) => { tempConfig.ChatWebY = Math.Max(0, tempConfig.ChatWebY - 20); ApplyLivePreview(); };
            btnChatWebDown.Click += (s, e) => { tempConfig.ChatWebY = tempConfig.ChatWebY + 20; ApplyLivePreview(); };
            btnChatWebLeft.Click += (s, e) => { tempConfig.ChatWebX = Math.Max(0, tempConfig.ChatWebX - 20); ApplyLivePreview(); };
            btnChatWebRight.Click += (s, e) => { tempConfig.ChatWebX = tempConfig.ChatWebX + 20; ApplyLivePreview(); };

            chatWebPosPanel.Controls.AddRange(new Control[] { btnChatWebUp, btnChatWebDown, btnChatWebLeft, btnChatWebRight });
            pnl.Controls.Add(chatWebPosPanel);

            pnl.Controls.Add(CreateLabel("Sohbet Web Genişliği"));
            pnl.Controls.Add(CreateTrackBar(50, 2000, tempConfig.ChatWebWidth, v => { tempConfig.ChatWebWidth = v; ApplyLivePreview(); }));

            pnl.Controls.Add(CreateLabel("Sohbet Web Yüksekliği"));
            pnl.Controls.Add(CreateTrackBar(50, 2000, tempConfig.ChatWebHeight, v => { tempConfig.ChatWebHeight = v; ApplyLivePreview(); }));

            pnl.Controls.Add(CreateLabel("Sohbet Web Yakınlaştırma (Zoom %50 - %200)"));
            int zoomPercent = (int)(tempConfig.ChatWebZoom * 100);
            tbChatWebZoom = CreateTrackBar(50, 200, zoomPercent, v => { tempConfig.ChatWebZoom = v / 100.0; ApplyLivePreview(); });
            pnl.Controls.Add(tbChatWebZoom);

            return pnl;
        }

        private Panel BuildWidgetPage()
        {
            FlowLayoutPanel pnl = CreateScrollablePage();
            pnl.Controls.Add(CreateHeader("TikFinity Widget Konum ve Boyut Ayarları"));

            pnl.Controls.Add(CreateLabel("TikFinity URL Adresi"));
            TextBox txtTikFinityUrl = new TextBox { Text = tempConfig.TikFinityUrl, Width = 560, Font = new Font("Segoe UI", 9.5F) };
            txtTikFinityUrl.TextChanged += (s, e) => { tempConfig.TikFinityUrl = txtTikFinityUrl.Text; ApplyLivePreview(); };
            pnl.Controls.Add(txtTikFinityUrl);

            pnl.Controls.Add(CreateLabel("Widget Konumunu Ok Tuşlarıyla Kaydır"));
            Panel widgetPosPanel = new Panel { Width = 260, Height = 95, BackColor = Color.FromArgb(40, 40, 40), Margin = new Padding(0, 5, 0, 15) };
            Button btnWidgetUp = CreateMiniButton("▲", 105, 10, 45, 35);
            Button btnWidgetDown = CreateMiniButton("▼", 105, 50, 45, 35);
            Button btnWidgetLeft = CreateMiniButton("◀", 55, 50, 45, 35);
            Button btnWidgetRight = CreateMiniButton("▶", 155, 50, 45, 35);

            btnWidgetUp.Click += (s, e) => { tempConfig.WidgetY = Math.Max(0, tempConfig.WidgetY - 20); ApplyLivePreview(); };
            btnWidgetDown.Click += (s, e) => { tempConfig.WidgetY = tempConfig.WidgetY + 20; ApplyLivePreview(); };
            btnWidgetLeft.Click += (s, e) => { tempConfig.WidgetX = Math.Max(0, tempConfig.WidgetX - 20); ApplyLivePreview(); };
            btnWidgetRight.Click += (s, e) => { tempConfig.WidgetX = tempConfig.WidgetX + 20; ApplyLivePreview(); };

            widgetPosPanel.Controls.AddRange(new Control[] { btnWidgetUp, btnWidgetDown, btnWidgetLeft, btnWidgetRight });
            pnl.Controls.Add(widgetPosPanel);

            pnl.Controls.Add(CreateLabel("Widget Genişliği"));
            pnl.Controls.Add(CreateTrackBar(50, 2000, tempConfig.WidgetWidth, v => { tempConfig.WidgetWidth = v; ApplyLivePreview(); }));

            pnl.Controls.Add(CreateLabel("Widget Yüksekliği"));
            pnl.Controls.Add(CreateTrackBar(50, 2000, tempConfig.WidgetHeight, v => { tempConfig.WidgetHeight = v; ApplyLivePreview(); }));

            pnl.Controls.Add(CreateLabel("Widget İçerik Boyutu / Yakınlaştırma (Zoom %50 - %200)"));
            int zoomPercent = (int)(tempConfig.WidgetZoom * 100);
            tbWidgetZoom = CreateTrackBar(50, 200, zoomPercent, v => { tempConfig.WidgetZoom = v / 100.0; ApplyLivePreview(); });
            pnl.Controls.Add(tbWidgetZoom);

            return pnl;
        }

        private Panel BuildViewerPage()
        {
            FlowLayoutPanel pnl = CreateScrollablePage();
            pnl.Controls.Add(CreateHeader("Viewer Konum ve Boyut Ayarları"));

            pnl.Controls.Add(CreateLabel("Viewer URL Adresi"));
            TextBox txtViewerUrl = new TextBox { Text = tempConfig.ViewerUrl, Width = 560, Font = new Font("Segoe UI", 9.5F) };
            txtViewerUrl.TextChanged += (s, e) => { tempConfig.ViewerUrl = txtViewerUrl.Text; ApplyLivePreview(); };
            pnl.Controls.Add(txtViewerUrl);

            pnl.Controls.Add(CreateLabel("Viewer Konumunu Ok Tuşlarıyla Kaydır"));
            Panel viewerPosPanel = new Panel { Width = 260, Height = 95, BackColor = Color.FromArgb(40, 40, 40), Margin = new Padding(0, 5, 0, 15) };
            Button btnViewerUp = CreateMiniButton("▲", 105, 10, 45, 35);
            Button btnViewerDown = CreateMiniButton("▼", 105, 50, 45, 35);
            Button btnViewerLeft = CreateMiniButton("◀", 55, 50, 45, 35);
            Button btnViewerRight = CreateMiniButton("▶", 155, 50, 45, 35);

            btnViewerUp.Click += (s, e) => { tempConfig.ViewerY = Math.Max(0, tempConfig.ViewerY - 20); ApplyLivePreview(); };
            btnViewerDown.Click += (s, e) => { tempConfig.ViewerY = tempConfig.ViewerY + 20; ApplyLivePreview(); };
            btnViewerLeft.Click += (s, e) => { tempConfig.ViewerX = Math.Max(0, tempConfig.ViewerX - 20); ApplyLivePreview(); };
            btnViewerRight.Click += (s, e) => { tempConfig.ViewerX = tempConfig.ViewerX + 20; ApplyLivePreview(); };

            viewerPosPanel.Controls.AddRange(new Control[] { btnViewerUp, btnViewerDown, btnViewerLeft, btnViewerRight });
            pnl.Controls.Add(viewerPosPanel);

            pnl.Controls.Add(CreateLabel("Viewer Genişliği"));
            pnl.Controls.Add(CreateTrackBar(50, 2000, tempConfig.ViewerWidth, v => { tempConfig.ViewerWidth = v; ApplyLivePreview(); }));

            pnl.Controls.Add(CreateLabel("Viewer Yüksekliği"));
            pnl.Controls.Add(CreateTrackBar(50, 2000, tempConfig.ViewerHeight, v => { tempConfig.ViewerHeight = v; ApplyLivePreview(); }));

            pnl.Controls.Add(CreateLabel("Viewer Yakınlaştırma (Zoom %50 - %200)"));
            int zoomPercent = (int)(tempConfig.ViewerZoom * 100);
            tbViewerZoom = CreateTrackBar(50, 200, zoomPercent, v => { tempConfig.ViewerZoom = v / 100.0; ApplyLivePreview(); });
            pnl.Controls.Add(tbViewerZoom);

            return pnl;
        }

        private Panel BuildGiftFeedPage()
        {
            FlowLayoutPanel pnl = CreateScrollablePage();
            pnl.Controls.Add(CreateHeader("Hediye Beslemesi Konum ve Boyut Ayarları"));

            pnl.Controls.Add(CreateLabel("Hediye Beslemesi URL Adresi"));
            TextBox txtGiftFeedUrl = new TextBox { Text = tempConfig.GiftFeedUrl, Width = 560, Font = new Font("Segoe UI", 9.5F) };
            txtGiftFeedUrl.TextChanged += (s, e) => { tempConfig.GiftFeedUrl = txtGiftFeedUrl.Text; ApplyLivePreview(); };
            pnl.Controls.Add(txtGiftFeedUrl);

            pnl.Controls.Add(CreateLabel("Hediye Beslemesi Konumunu Ok Tuşlarıyla Kaydır"));
            Panel giftFeedPosPanel = new Panel { Width = 260, Height = 95, BackColor = Color.FromArgb(40, 40, 40), Margin = new Padding(0, 5, 0, 15) };
            Button btnGiftFeedUp = CreateMiniButton("▲", 105, 10, 45, 35);
            Button btnGiftFeedDown = CreateMiniButton("▼", 105, 50, 45, 35);
            Button btnGiftFeedLeft = CreateMiniButton("◀", 55, 50, 45, 35);
            Button btnGiftFeedRight = CreateMiniButton("▶", 155, 50, 45, 35);

            btnGiftFeedUp.Click += (s, e) => { tempConfig.GiftFeedY = Math.Max(0, tempConfig.GiftFeedY - 20); ApplyLivePreview(); };
            btnGiftFeedDown.Click += (s, e) => { tempConfig.GiftFeedY = tempConfig.GiftFeedY + 20; ApplyLivePreview(); };
            btnGiftFeedLeft.Click += (s, e) => { tempConfig.GiftFeedX = Math.Max(0, tempConfig.GiftFeedX - 20); ApplyLivePreview(); };
            btnGiftFeedRight.Click += (s, e) => { tempConfig.GiftFeedX = tempConfig.GiftFeedX + 20; ApplyLivePreview(); };

            giftFeedPosPanel.Controls.AddRange(new Control[] { btnGiftFeedUp, btnGiftFeedDown, btnGiftFeedLeft, btnGiftFeedRight });
            pnl.Controls.Add(giftFeedPosPanel);

            pnl.Controls.Add(CreateLabel("Hediye Beslemesi Genişliği"));
            pnl.Controls.Add(CreateTrackBar(50, 2000, tempConfig.GiftFeedWidth, v => { tempConfig.GiftFeedWidth = v; ApplyLivePreview(); }));

            pnl.Controls.Add(CreateLabel("Hediye Beslemesi Yüksekliği"));
            pnl.Controls.Add(CreateTrackBar(50, 2000, tempConfig.GiftFeedHeight, v => { tempConfig.GiftFeedHeight = v; ApplyLivePreview(); }));

            pnl.Controls.Add(CreateLabel("Hediye Beslemesi Yakınlaştırma (Zoom %50 - %200)"));
            int zoomPercent = (int)(tempConfig.GiftFeedZoom * 100);
            tbGiftFeedZoom = CreateTrackBar(50, 200, zoomPercent, v => { tempConfig.GiftFeedZoom = v / 100.0; ApplyLivePreview(); });
            pnl.Controls.Add(tbGiftFeedZoom);

            return pnl;
        }

        private Panel BuildLatestFollowerPage()
        {
            FlowLayoutPanel pnl = CreateScrollablePage();
            pnl.Controls.Add(CreateHeader("Son Takipçi Konum ve Boyut Ayarları"));

            pnl.Controls.Add(CreateLabel("Son Takipçi URL Adresi"));
            TextBox txtLatestFollowerUrl = new TextBox { Text = tempConfig.LatestFollowerUrl, Width = 560, Font = new Font("Segoe UI", 9.5F) };
            txtLatestFollowerUrl.TextChanged += (s, e) => { tempConfig.LatestFollowerUrl = txtLatestFollowerUrl.Text; ApplyLivePreview(); };
            pnl.Controls.Add(txtLatestFollowerUrl);

            pnl.Controls.Add(CreateLabel("Son Takipçi Konumunu Ok Tuşlarıyla Kaydır"));
            Panel latestFollowerPosPanel = new Panel { Width = 260, Height = 95, BackColor = Color.FromArgb(40, 40, 40), Margin = new Padding(0, 5, 0, 15) };
            Button btnLatestFollowerUp = CreateMiniButton("▲", 105, 10, 45, 35);
            Button btnLatestFollowerDown = CreateMiniButton("▼", 105, 50, 45, 35);
            Button btnLatestFollowerLeft = CreateMiniButton("◀", 55, 50, 45, 35);
            Button btnLatestFollowerRight = CreateMiniButton("▶", 155, 50, 45, 35);

            btnLatestFollowerUp.Click += (s, e) => { tempConfig.LatestFollowerY = Math.Max(0, tempConfig.LatestFollowerY - 20); ApplyLivePreview(); };
            btnLatestFollowerDown.Click += (s, e) => { tempConfig.LatestFollowerY = tempConfig.LatestFollowerY + 20; ApplyLivePreview(); };
            btnLatestFollowerLeft.Click += (s, e) => { tempConfig.LatestFollowerX = Math.Max(0, tempConfig.LatestFollowerX - 20); ApplyLivePreview(); };
            btnLatestFollowerRight.Click += (s, e) => { tempConfig.LatestFollowerX = tempConfig.LatestFollowerX + 20; ApplyLivePreview(); };

            latestFollowerPosPanel.Controls.AddRange(new Control[] { btnLatestFollowerUp, btnLatestFollowerDown, btnLatestFollowerLeft, btnLatestFollowerRight });
            pnl.Controls.Add(latestFollowerPosPanel);

            pnl.Controls.Add(CreateLabel("Son Takipçi Genişliği"));
            pnl.Controls.Add(CreateTrackBar(50, 2000, tempConfig.LatestFollowerWidth, v => { tempConfig.LatestFollowerWidth = v; ApplyLivePreview(); }));

            pnl.Controls.Add(CreateLabel("Son Takipçi Yüksekliği"));
            pnl.Controls.Add(CreateTrackBar(50, 2000, tempConfig.LatestFollowerHeight, v => { tempConfig.LatestFollowerHeight = v; ApplyLivePreview(); }));

            pnl.Controls.Add(CreateLabel("Son Takipçi Yakınlaştırma (Zoom %50 - %200)"));
            int zoomPercent = (int)(tempConfig.LatestFollowerZoom * 100);
            tbLatestFollowerZoom = CreateTrackBar(50, 200, zoomPercent, v => { tempConfig.LatestFollowerZoom = v / 100.0; ApplyLivePreview(); });
            pnl.Controls.Add(tbLatestFollowerZoom);

            return pnl;
        }

        private void SwitchPage(string pageName)
        {
            foreach (var page in pages.Values) page.Visible = false;
            if (pages.ContainsKey(pageName)) pages[pageName].Visible = true;

            foreach (var kvp in navButtons)
            {
                if (kvp.Key == pageName)
                {
                    kvp.Value.BackColor = Color.FromArgb(50, 50, 50);
                    kvp.Value.ForeColor = Color.White;
                }
                else
                {
                    kvp.Value.BackColor = Color.Transparent;
                    kvp.Value.ForeColor = Color.FromArgb(180, 180, 180);
                }
            }
        }

        private Button CreateSidebarButton(string text, string pageKey, int yPos)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(10, yPos),
                Size = new Size(210, 42),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 40);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 60, 60);

            btn.Click += (s, e) => SwitchPage(pageKey);
            navButtons.Add(pageKey, btn);
            return btn;
        }

        private FlowLayoutPanel CreateScrollablePage()
        {
            return new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                WrapContents = false,
                Padding = new Padding(10, 0, 10, 20)
            };
        }

        private Label CreateHeader(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(0, 120, 212),
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 15)
            };
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
                Margin = new Padding(0, 12, 0, 4),
                Font = new Font("Segoe UI", 9.5F)
            };
        }

        private CheckBox CreateCheckBox(string text, bool check, Action<bool> onChange)
        {
            var cb = new CheckBox
            {
                Text = text,
                Checked = check,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F),
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 8),
                Cursor = Cursors.Hand
            };
            cb.CheckedChanged += (s, e) => onChange(cb.Checked);
            return cb;
        }

        private TrackBar CreateTrackBar(int min, int max, int val, Action<int> onChange)
        {
            var tb = new TrackBar
            {
                Minimum = min,
                Maximum = max,
                Value = Math.Clamp(val, min, max),
                Width = 560,
                TickStyle = TickStyle.None,
                Margin = new Padding(0, 2, 0, 10)
            };
            tb.ValueChanged += (s, e) => onChange(tb.Value);
            return tb;
        }

        private Button CreateAccentButton(string text, int x, int y, int w, int h)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 120, 212),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 110, 190);
            return btn;
        }

        private Button CreateSecondaryButton(string text, int x, int y, int w, int h)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(240, 240, 240),
                BackColor = Color.FromArgb(55, 55, 55),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5F)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
            return btn;
        }

        private Button CreateMiniButton(string text, int x, int y, int w, int h)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80);
            return btn;
        }

        private Button CreateDangerButton(string text, int w, int h)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(196, 43, 28),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(210, 50, 35);
            return btn;
        }

        private void ApplyLivePreview()
        {
            if (previewDebounceTimer != null)
            {
                previewDebounceTimer.Stop();
                previewDebounceTimer.Start();
            }
            else
            {
                ExecuteLivePreview();
            }
        }

        private void ExecuteLivePreview()
        {
            tempConfig.FormX = mainForm.Location.X;
            tempConfig.FormY = mainForm.Location.Y;
            mainForm.currentConfig = tempConfig;
            mainForm.ApplySettings();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            previewDebounceTimer?.Stop();
            previewDebounceTimer?.Dispose();

            if (!isSaved)
            {
                mainForm.currentConfig = originalConfig;
                mainForm.ApplySettings();
            }
            base.OnFormClosing(e);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            SuspendLayout();
            // 
            // SettingsForm
            // 
            ClientSize = new Size(284, 261);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "SettingsForm";
            ShowInTaskbar = false;
            ResumeLayout(false);
        }
    }
}