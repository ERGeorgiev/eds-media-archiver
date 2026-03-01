namespace EdsMediaArchiver.Helpers;

internal static class FileHelper
{
    public static string GetUniqueFilePath(string path)
    {
        if (File.Exists(path) == false) return path;

        var dir = Path.GetDirectoryName(path)!;
        var baseName = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var counter = 2;

        string candidate = Path.Combine(dir, $"{baseName}_a{ext}");
        while (File.Exists(candidate))
        {
            counter++;
            candidate = Path.Combine(dir, $"{baseName}_a{counter}{ext}");
        }

        return candidate;
    }
}
