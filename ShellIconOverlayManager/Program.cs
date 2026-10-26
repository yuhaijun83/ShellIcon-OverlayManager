using System;
using System.Windows.Forms;
using ShellIconOverlayManager.Data;
using ShellIconOverlayManager.Localization;
using ShellIconOverlayManager.Ui;

namespace ShellIconOverlayManager
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 语言资源统一放在 Languages\<culture>\ 目录，需先注册加载器
            SatelliteLoader.Install();

            try
            {
                AppDatabase.Initialize();
                Localizer.Initialize();
                var fontSizeText = AppDatabase.GetSetting("ContentFontSize", null);
                if (float.TryParse(fontSizeText, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var fontSize))
                {
                    UiFont.SetContentSize(fontSize);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Shell Icon Overlay Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new MainForm());
        }
    }
}
