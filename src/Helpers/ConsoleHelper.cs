using EdsMediaArchiver.Models;

namespace EdsMediaArchiver.Helpers;

internal static class ConsoleHelper
{
    public static bool TryGetUserPreferences(out UserPreferences prefs)
    {
        prefs = new UserPreferences();

        // Ask user preferences
        Console.WriteLine("  What would you like to do?");
        Console.WriteLine();

        Console.WriteLine("  Compress files?");
        Console.WriteLine("    Converts media to optimal formats:");
        Console.WriteLine("      Images (ex. PNG, BMP, GIF) -> JPG (multi-frame GIF -> MP4)");
        Console.WriteLine("        WARNING: .pngs will lose transperancy.");
        Console.WriteLine("      Video  (ex. AVI, MKV, WMV, MOV, 3GP, GIF) -> MP4 (H.264 + AAC)");
        Console.WriteLine("      Audio  (ex. MP3, WAV, FLAC, AAC, WMA, M4A) -> OGG (Vorbis)");
        Console.Write("    (Y/n): ");
        prefs.Compress = ConsoleHelper.AskYesNo();
        Console.WriteLine();

        if (prefs.Compress)
        {
            Console.WriteLine("  Also resize images and videos as part of compression?");
            Console.WriteLine("    Resizes to max 1920 width or height.");
            Console.Write("    (Y/n): ");
            prefs.ResizeOnCompress = ConsoleHelper.AskYesNo();
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("  Standardize file types?");
            Console.WriteLine("    Converts all videos to .mp4, all common images (ex. png/gif) to .jpg and audio to .ogg .");
            Console.WriteLine("    WARNING: .pngs will lose transperancy.");
            Console.WriteLine("    WARNING: Without this, setting/reading file dates may be unreliable.");
            Console.Write("    (Y/n): ");
            prefs.Standardize = ConsoleHelper.AskYesNo();
            Console.WriteLine();
        }

        Console.WriteLine("  Set file dates?");
        Console.WriteLine("    Writes date metadata and sets filesystem Created/Modified dates.");
        Console.Write("    (Y/n): ");
        prefs.SetDates = ConsoleHelper.AskYesNo();

        if (!prefs.Compress && !prefs.Standardize && !prefs.SetDates)
        {
            Console.WriteLine();
            Console.WriteLine("  No options selected.");
            return false;
        }

        Console.WriteLine();
        Console.WriteLine("  Selected options:");
        if (prefs.Compress) Console.WriteLine("    - Compress files");
        if (prefs.ResizeOnCompress) Console.WriteLine("    - Resize files");
        if (prefs.Standardize) Console.WriteLine("    - Standardize files");
        if (prefs.SetDates) Console.WriteLine("    - Set file dates");

        return true;
    }

    public static bool AskYesNo()
    {
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) || input.StartsWith("y", StringComparison.OrdinalIgnoreCase);
    }

    public static void FlushInput()
    {
        while (Console.KeyAvailable)
        {
            Console.ReadKey(intercept: true);
        }
    }
}
