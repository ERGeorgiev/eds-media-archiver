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

        string candidate;
        do
        {
            candidate = Path.Combine(dir, $"{baseName}_{counter}{ext}");
            counter++;
        } while (File.Exists(candidate));

        return candidate;
    }
}
