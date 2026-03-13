using System;
using System.IO;

namespace ScreenTimeController;

public static class AbnormalExitTracker
{
    private static readonly string AbnormalExitFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ScreenTimeController",
        "abnormal_exits.txt");

    public static int GetTodayAbnormalExitCount()
    {
        try
        {
            if (!File.Exists(AbnormalExitFilePath))
            {
                return 0;
            }

            string[] lines = File.ReadAllLines(AbnormalExitFilePath);
            if (lines.Length < 2)
            {
                return 0;
            }

            string today = DateTime.Today.ToString("yyyy-MM-dd");
            if (!lines[0].StartsWith(today))
            {
                return 0;
            }

            if (int.TryParse(lines[1], out int count))
            {
                return count;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    public static void ResetTodayCount()
    {
        try
        {
            string commonDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScreenTimeController");
            if (!Directory.Exists(commonDataDir))
            {
                Directory.CreateDirectory(commonDataDir);
            }

            string today = DateTime.Today.ToString("yyyy-MM-dd");
            File.WriteAllLines(AbnormalExitFilePath, new string[] { today, "0" });
        }
        catch { }
    }

    public static AbnormalExitHistory GetHistory(int days = 7)
    {
        var history = new AbnormalExitHistory
        {
            TodayCount = GetTodayAbnormalExitCount(),
            LastCheck = DateTime.Now
        };
        return history;
    }
}

public class AbnormalExitHistory
{
    public int TodayCount { get; set; }
    public DateTime LastCheck { get; set; }
}
