using System;
using System.IO;
using Newtonsoft.Json;

namespace tiktok_chat_levach
{
    public class AppConfig
    {
        // Genel Varsayılan Zoom (Hata Alan Kısımların Çalışması İçin Eklendi)
        public double ZoomFactor { get; set; } = 1.0;

        public string LatestFollowerUrl { get; set; } = "";
        public int LatestFollowerX { get; set; } = 100;
        public int LatestFollowerY { get; set; } = 100;
        public int LatestFollowerWidth { get; set; } = 300;
        public int LatestFollowerHeight { get; set; } = 150;
        public double LatestFollowerZoom { get; set; } = 1.0;

        // BİLGİLENDİRME EKRANI GÖSTER / GÖSTERME
        public bool HideInfoOnStartup { get; set; } = false;

        // Pencere Konum, Boyut ve Görünüm Ayarları
        public int FormWidth { get; set; } = 420;
        public int FormHeight { get; set; } = 600;
        public int FormX { get; set; } = 100;
        public int FormY { get; set; } = -1;
        public bool TopMost { get; set; } = true;

        // Arka Plan ve Modern Tema Ayarları
        public bool UseTransparentBackground { get; set; } = true;
        public string BackgroundColor { get; set; } = "#121214";
        public string ThemeMode { get; set; } = "Dark";
        public int CornerRadius { get; set; } = 12;
        public double WindowOpacity { get; set; } = 1.0;

        // Diğer Formların/Kodların Hata Vermemesi İçin Temel Alanlar
        public bool ShowChat { get; set; } = true;
        public string ChatTextColor { get; set; } = "#FFFFFF";
        public string NicknameTextColor { get; set; } = "#00FFCC";
        public string ChatFontName { get; set; } = "Segoe UI";
        public float ChatFontSize { get; set; } = 9.5f;
        public int MaxChatLines { get; set; } = 50;
        public bool ShowBadge { get; set; } = true;
        public int TextOffset { get; set; } = 5;

        public bool ShowGifts { get; set; } = true;
        public int GiftPanelHeight { get; set; } = 50;
        public string GiftTextColor { get; set; } = "#FFD700";
        public string GiftFontName { get; set; } = "Segoe UI";
        public float GiftFontSize { get; set; } = 9.5f;
        public bool EnableGiftAnimations { get; set; } = true;

        public bool ShowJoins { get; set; } = true;
        public int JoinPanelHeight { get; set; } = 40;
        public string JoinTextColor { get; set; } = "#00FF00";
        public string JoinFontName { get; set; } = "Segoe UI";
        public float JoinFontSize { get; set; } = 9.5f;
        public bool GroupJoinMessages { get; set; } = true;

        public bool PlaySoundOnGift { get; set; } = false;
        public string GiftSoundPath { get; set; } = "";

        // İzlenme (Viewer) Penceresi Ayarları
        public string ViewerUrl { get; set; } = "";
        public int ViewerX { get; set; } = 10;
        public int ViewerY { get; set; } = 450;
        public int ViewerWidth { get; set; } = 160;
        public int ViewerHeight { get; set; } = 40;
        public double ViewerZoom { get; set; } = 1.0;
        public string ViewerTextColor { get; set; } = "#00FF00";
        public string ViewerBackgroundColor { get; set; } = "#202020";
        public bool ShowViewerBackground { get; set; } = true;
        public string ViewerFontName { get; set; } = "Segoe UI";
        public float ViewerFontSize { get; set; } = 9.5f;
        public bool ShowViewerCount { get; set; } = true;

        // Beğeni Sıralama Widget Ayarları (TikFinity vb.)
        public string TikFinityUrl { get; set; } = "";
        public int WidgetX { get; set; } = 220;
        public int WidgetY { get; set; } = 5;
        public int WidgetWidth { get; set; } = 180;
        public int WidgetHeight { get; set; } = 200;
        public double WidgetZoom { get; set; } = 1.0;

        // Hediye Beslemesi (Gift Feed) Ayarları
        public string GiftFeedUrl { get; set; } = "";
        public int GiftFeedX { get; set; } = 400;
        public int GiftFeedY { get; set; } = 400;
        public int GiftFeedWidth { get; set; } = 350;
        public int GiftFeedHeight { get; set; } = 250;
        public double GiftFeedZoom { get; set; } = 1.0;
        public string GiftFeedTextColor { get; set; } = "#FFFFFF";
        public string GiftFeedBackgroundColor { get; set; } = "#202020";
        public bool ShowGiftFeedBackground { get; set; } = true;
        public string GiftFeedFontName { get; set; } = "Segoe UI";
        public float GiftFeedFontSize { get; set; } = 9.5F;

        // Sohbet Web URL Ayarları
        public string ChatWebUrl { get; set; } = "";
        public bool ShowChatWebBackground { get; set; } = true;
        public int ChatWebX { get; set; } = 400;
        public int ChatWebY { get; set; } = 100;
        public int ChatWebWidth { get; set; } = 350;
        public int ChatWebHeight { get; set; } = 400;
        public double ChatWebZoom { get; set; } = 1.0;
        public string ChatWebTextColor { get; set; } = "#FFFFFF";
        public string ChatWebBackgroundColor { get; set; } = "#202020";
        public string ChatWebFontName { get; set; } = "Segoe UI";
        public float ChatWebFontSize { get; set; } = 9.5F;
        public string BroadcasterUsername { get; set; } = "";
    }

    public static class ConfigManager
    {
        private static string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (!File.Exists(configPath) || new FileInfo(configPath).Length == 0)
                {
                    var defaultConfig = new AppConfig();
                    Save(defaultConfig);
                    return defaultConfig;
                }

                string json = File.ReadAllText(configPath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    var defaultConfig = new AppConfig();
                    Save(defaultConfig);
                    return defaultConfig;
                }

                var config = JsonConvert.DeserializeObject<AppConfig>(json);
                return config ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void Save(AppConfig config)
        {
            try
            {
                if (config == null) return;
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch { }
        }

        public static void ResetToDefault()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }
            }
            catch { }
        }
    }
}