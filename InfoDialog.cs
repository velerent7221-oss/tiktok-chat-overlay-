using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace tiktok_chat_levach
{
    public partial class InfoDialog : Form
    {
        private CheckBox chkDontShowAgain;
        private AppConfig config;
        private Point mouseLocation;

        private Panel mainContentPanel;
        private readonly Dictionary<string, Panel> pages = new Dictionary<string, Panel>();
        private readonly Dictionary<string, Button> navButtons = new Dictionary<string, Button>();

        public InfoDialog(AppConfig currentConfig)
        {
            config = currentConfig ?? throw new ArgumentNullException(nameof(currentConfig));
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            this.SuspendLayout();

            this.Text = "TikTok Chat Levach - Rehber ve Tanıtım";
            this.Size = new Size(820, 530);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(18, 18, 24);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false; // Görev çubuğunda görünmesi engellendi

            this.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) mouseLocation = e.Location; };
            this.MouseMove += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    this.Location = new Point(this.Location.X + (e.X - mouseLocation.X), this.Location.Y + (e.Y - mouseLocation.Y));
                }
            };

            this.Paint += (s, e) =>
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
            };

            Panel sidebar = new Panel
            {
                Location = new Point(12, 12),
                Size = new Size(185, 450),
                BackColor = Color.FromArgb(24, 24, 32)
            };

            Label lblMenuTitle = new Label
            {
                Text = "REHBER MENÜSÜ",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 150, 255),
                Location = new Point(14, 18),
                AutoSize = true
            };
            sidebar.Controls.Add(lblMenuTitle);

            Panel sideLine = new Panel { Location = new Point(14, 45), Size = new Size(155, 1), BackColor = Color.FromArgb(50, 50, 65) };
            sidebar.Controls.Add(sideLine);

            int btnStartY = 55;
            sidebar.Controls.Add(CreateSidebarButton("⚙️ Genel & Görünüm", "Genel", ref btnStartY));
            sidebar.Controls.Add(CreateSidebarButton("💬 Sohbet Web Alanı", "ChatWeb", ref btnStartY));
            sidebar.Controls.Add(CreateSidebarButton("🧩 Widget Ayarları", "Widget", ref btnStartY));
            sidebar.Controls.Add(CreateSidebarButton("🌐 Viewer Ayarları", "Viewer", ref btnStartY));
            sidebar.Controls.Add(CreateSidebarButton("🎁 Hediye Beslemesi", "GiftFeed", ref btnStartY));
            sidebar.Controls.Add(CreateSidebarButton("👤 Son Takipçi", "LatestFollower", ref btnStartY));

            Label lblSettingsNotice = new Label
            {
                Text = "📌 Sol menüden modüllere tıklayarak ne işe yaradıklarını öğrenebilirsiniz.",
                Font = new Font("Segoe UI", 7.8F, FontStyle.Italic),
                ForeColor = Color.DarkGray,
                Location = new Point(12, 345),
                Size = new Size(160, 90)
            };
            sidebar.Controls.Add(lblSettingsNotice);
            this.Controls.Add(sidebar);

            mainContentPanel = new Panel
            {
                Location = new Point(205, 12),
                Size = new Size(600, 450),
                BackColor = Color.FromArgb(28, 28, 38),
                Padding = new Padding(15)
            };

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

            this.Controls.Add(mainContentPanel);
            SwitchPage("Genel");

            chkDontShowAgain = new CheckBox
            {
                Text = "Bir daha bu rehberi açılışta gösterme",
                ForeColor = Color.DarkGray,
                Location = new Point(20, 480),
                AutoSize = true,
                Checked = config.HideInfoOnStartup
            };

            Button btnClose = new Button
            {
                Text = "ANLADIM & BAŞLA",
                DialogResult = DialogResult.OK,
                Location = new Point(635, 472),
                Size = new Size(170, 36),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) =>
            {
                config.HideInfoOnStartup = chkDontShowAgain.Checked;
                ConfigManager.Save(config);
                this.Close();
            };

            this.Controls.Add(chkDontShowAgain);
            this.Controls.Add(btnClose);

            this.ResumeLayout(false);
        }

        private void SwitchPage(string pageName)
        {
            foreach (var page in pages.Values) page.Visible = false;
            if (pages.ContainsKey(pageName)) pages[pageName].Visible = true;

            foreach (var kvp in navButtons)
            {
                if (kvp.Key == pageName)
                {
                    kvp.Value.BackColor = Color.FromArgb(0, 120, 215);
                    kvp.Value.ForeColor = Color.White;
                }
                else
                {
                    kvp.Value.BackColor = Color.Transparent;
                    kvp.Value.ForeColor = Color.FromArgb(180, 180, 190);
                }
            }
        }

        private Panel BuildGenelPage()
        {
            var pnl = CreateScrollablePage();
            pnl.Controls.Add(CreateHeader("Genel Pencere ve Görünüm"));
            pnl.Controls.Add(CreateBody("Bu uygulama, TikTok canlı yayıncıları için özel olarak geliştirilmiş çok fonksiyonlu bir yayın kontrol ve etkileşim aracıdır.\n\nCanlı yayın esnasında izleyici istatistiklerini takip etmenizi, pencereleri özelleştirmenizi ve OBS üzerinde şık bir entegrasyon kurmanızı sağlar."));
            pnl.Controls.Add(CreateHeader("Öne Çıkan Özellikler"));
            pnl.Controls.Add(CreateBody("• Şeffaf ve modern arayüz tasarımıyla yayın üstünde şık durur.\n• 'TopMost' (Her Zaman Üstte) seçeneğiyle pencerelerin kaybolmasını önler.\n• Düşük sistem tüketimiyle yayın performansınızı etkilemez."));
            return pnl;
        }

        private Panel BuildChatWebPage()
        {
            var pnl = CreateScrollablePage();
            pnl.Controls.Add(CreateHeader("Sohbet Web Alanı Nedir?"));
            pnl.Controls.Add(CreateBody("TikTok canlı yayın sohbet ekranını (chat) oyun oynarken veya yayın yönetirken tek bir noktadan takip etmenizi sağlayan web tabanlı sohbet penceresidir.\n\nİzleyicilerinizin mesajlarına hızlıca odaklanmanıza yardımcı olur."));
            pnl.Controls.Add(CreateHeader("Öğretici İpucu"));
            pnl.Controls.Add(CreateBody("Ayarlar merkezinden genişlik, yükseklik ve yakınlaştırma (Zoom) oranlarını değiştirerek sohbet alanını tamamen kendi ekran düzeninize göre optimize edebilirsiniz."));
            return pnl;
        }

        private Panel BuildWidgetPage()
        {
            var pnl = CreateScrollablePage();
            pnl.Controls.Add(CreateHeader("TikFinity Widget Entegrasyonu"));
            pnl.Controls.Add(CreateBody("TikFinity, TikTok yayınları için ses efektleri, uyarılar (alert) ve görsel öğeler tetiklemenize olanak tanır.\n\nBu modül sayesinde TikFinity üzerindeki web tabanlı widget'larınızı doğrudan uygulama içinde pencereler şeklinde çalıştırabilirsiniz."));
            pnl.Controls.Add(CreateHeader("Nasıl Yapılandırılır?"));
            pnl.Controls.Add(CreateBody("Ayarlar ekranından TikFinity URL adresinizi girerek bu alanı aktif hale getirebilir, konumunu dilediğiniz gibi ayarlayabilirsiniz."));
            return pnl;
        }

        private Panel BuildViewerPage()
        {
            var pnl = CreateScrollablePage();
            pnl.Controls.Add(CreateHeader("Viewer (Yayın İzleyici) Modülü"));
            pnl.Controls.Add(CreateBody("Viewer modülü, yayınınızdaki anlık izleyici sayısını, etkileşimleri ve temel yayın verilerini minimalist bir şeritte takip etmenizi sağlar.\n\nBu modülü kullanarak yayın akışınızı bölmeden anlık izleyici istatistiklerinizi kontrol edebilirsiniz."));
            pnl.Controls.Add(CreateHeader("Kullanım İpucu"));
            pnl.Controls.Add(CreateBody("Ok tuşları yardımıyla pencereyi piksel piksel ekranınızın dilediğiniz köşesine sabitleyebilirsiniz."));
            return pnl;
        }

        private Panel BuildGiftFeedPage()
        {
            var pnl = CreateScrollablePage();
            pnl.Controls.Add(CreateHeader("Hediye Akışı (GiftFeed) Modülü"));
            pnl.Controls.Add(CreateBody("Yayınınız esnasında izleyiciler tarafından gönderilen tüm hediyeleri (Gül, Aslan, TikTok Kaseti vb.) anlık olarak ekrana listeleyen akış modülüdür.\n\nKimin ne kadar hediye attığını kaçırmamanız için özel olarak tasarlanmıştır."));
            pnl.Controls.Add(CreateHeader("Görsel Özelleştirme"));
            pnl.Controls.Add(CreateBody("Arka plan şeffaflığını, yazı tiplerini ve renkleri kendi yayın konseptinize uygun olarak ayarlayabilirsiniz."));
            return pnl;
        }

        private Panel BuildLatestFollowerPage()
        {
            var pnl = CreateScrollablePage();
            pnl.Controls.Add(CreateHeader("Son Takipçi (LatestFollower) Modülü"));
            pnl.Controls.Add(CreateBody("Yayınınızı takip eden en son kullanıcıyı şık bir görsel bileşenle ekranda göstermenizi sağlayan modüldür.\n\nTakipçilerinize canlı yayında teşekkür etmek ve etkileşimi artırmak için harika bir araçtır."));
            pnl.Controls.Add(CreateHeader("Kullanım İpucu"));
            pnl.Controls.Add(CreateBody("Son takipçi URL adresinizi ilgili alana girerek ve boyut/konum ayarlarını yapılandırarak ekranınızda dilediğiniz yerde sergileyebilirsiniz."));
            return pnl;
        }

        private Button CreateSidebarButton(string text, string pageKey, ref int yPos)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(10, yPos),
                Size = new Size(165, 38),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 180, 190),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 8.8F, FontStyle.Regular)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 45, 60);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 60, 80);

            btn.Click += (s, e) => SwitchPage(pageKey);
            navButtons.Add(pageKey, btn);
            yPos += 42;
            return btn;
        }

        private FlowLayoutPanel CreateScrollablePage()
        {
            return new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = true,
                WrapContents = false,
                Padding = new Padding(10, 5, 10, 20),
                Dock = DockStyle.Fill
            };
        }

        private Label CreateHeader(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(0, 150, 255),
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                AutoSize = true,
                MaximumSize = new Size(550, 0),
                Margin = new Padding(0, 10, 0, 8)
            };
        }

        private Label CreateBody(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(210, 210, 220),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                AutoSize = true,
                MaximumSize = new Size(550, 0),
                Margin = new Padding(0, 0, 0, 15)
            };
        }
    }
}