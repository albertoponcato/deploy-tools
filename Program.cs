using System.Diagnostics;
using System.Text.RegularExpressions;
using Cocona;
using HtmlAgilityPack;

internal enum SitePlatform
{
    Kentico,
    Umbraco,
    Other
}

internal class Program
{
    private static void Main(string[] args)
    {
        var app = CoconaApp.Create();

        app
            .AddCommand("clean", () => PrePublishCleaning())
            .WithDescription("This command will delete all files and folders except those defined");

        app
            .AddCommand("sanitize", () => SanitizeTemplate())
            .WithDescription("This command will prepare templates for publication in staging");

        app
            .AddCommand("days", () => CopyDaysSince20000101())
            .WithDescription("This command will copy the number of days since 2000-01-01 to the clipboard");

        app
            .AddCommand("clean-config", () => DeleteConfigFiles())
            .WithDescription("This command will delete all web.config and appsettings.json / appsettings.*.json files");

        app.Run();
    }

    private static void CopyDaysSince20000101()
    {
        var baseDate = new DateTime(2000, 1, 1);
        var today = DateTime.Today;
        var days = (today - baseDate).Days;

        try
        {
            var psi = new ProcessStartInfo("clip")
            {
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            using var p = Process.Start(psi);
            if (p != null)
            {
                using var sw = p.StandardInput;
                sw.Write(days.ToString());
            }

            Console.WriteLine($"{days} copied to clipboard.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to copy to clipboard. Value: {days}. Error: {ex.Message}");
        }
    }

    private static void SanitizeTemplate()
    {
        var path = Environment.CurrentDirectory;

        Console.WriteLine();
        Console.WriteLine("This command will prepare templates for publication in staging");
        Console.WriteLine();
        Console.WriteLine($"Folder \"{path}\" located.");
        Console.WriteLine();
        Console.WriteLine("Do you want to proceed? Press [Y(yes)] to confirm, or any other key to abort.");

        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input) || !input.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
        {
            Console.WriteLine("Operation aborted by the user.");
            return;
        }

        Console.WriteLine();

        var files = Directory
            .EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
            .Where(s => ".html".Equals(Path.GetExtension(s).ToLowerInvariant()));

        foreach (var file in files)
        {
            string text = File.ReadAllText(file);
            text = Regex.Replace(text, "/inc/", "inc/", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "href=\"/\"", "href=\"index.html\"", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\?id=[a-zA-Z0-9]+", string.Empty);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(text);

            if (file.EndsWith("index.html"))
            {
                var nodes = htmlDoc.DocumentNode?
                    .SelectNodes("//a")?
                    .Where(a => a.GetAttributeValue("href", string.Empty).StartsWith('/') && a.GetAttributeValue("href", string.Empty).EndsWith(".html"))
                    .ToList();

                if (nodes?.Any() == true)
                {
                    foreach (var node in nodes)
                    {
                        var attributeValue = node.GetAttributeValue("href", string.Empty);
                        attributeValue = attributeValue.Substring(1);
                        node.SetAttributeValue("href", attributeValue);
                    }
                }
            }

            htmlDoc.Save(file);
            Console.WriteLine(file);
        }

        Console.WriteLine(path);
        Console.WriteLine("Operation completed");
    }

    private static void DeleteConfigFiles()
    {
        var path = Environment.CurrentDirectory;

        Console.WriteLine();
        Console.WriteLine("This command will delete all web.config and appsettings.json / appsettings.*.json files");

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            Console.WriteLine($"Problem with the path to process \"{path}\"");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Folder \"{path}\" located.");
        Console.WriteLine();

        var files = Directory
            .EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
            .Where(f => IsExcludedFile(Path.GetFileName(f)))
            .ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("No files found. Operation completed.");
            return;
        }

        Console.WriteLine($"The following {files.Count} file(s) will be deleted:");
        Console.WriteLine();
        foreach (var file in files)
        {
            Console.WriteLine($"  {Path.GetFileName(file)}");
        }

        Console.WriteLine();
        Console.WriteLine("Do you want to proceed? Press [Y(yes)] to confirm, or any other key to abort.");

        var confirm = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(confirm) || !confirm.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
        {
            Console.WriteLine("Operation aborted by the user.");
            return;
        }

        Console.WriteLine();

        foreach (var file in files)
        {
            File.Delete(file);
            Console.WriteLine($"File \"{file}\" deleted");
        }

        Console.WriteLine("Operation completed");
    }

    private static void PrePublishCleaning()
    {
        var path = Environment.CurrentDirectory;

        SitePlatform platform = DetectPlatform(path);
        string? excludeDirName = platform switch
        {
            SitePlatform.Kentico => "assets",
            SitePlatform.Umbraco => "wwwroot",
            _ => null
        };

        (string DirName, string KeepName)[] prunedDirs = platform == SitePlatform.Umbraco
            ? [("wwwroot", "media"), ("umbraco", "Licenses")]
            : [];

        Console.WriteLine();
        Console.Write("Detected platform: ");
        WriteLinePlatform(platform);
        string folderClause = excludeDirName is null ? "all folders" : $"all folders except \"{excludeDirName}\"";
        Console.WriteLine($"This command will delete all files except \"web.config\" and any \"appsettings*.json\" file, and {folderClause}");
        foreach ((string dirName, string keepName) in prunedDirs)
        {
            Console.WriteLine($"Inside \"{dirName}\", all folders except \"{keepName}\" will be deleted, if present.");
        }

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            Console.WriteLine($"Problem with the path to process \"{path}\"");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Folder \"{path}\" located.");
        Console.WriteLine();
        Console.WriteLine("Do you want to proceed? Press [Y(yes)] to confirm, or any other key to abort.");

        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input) || !input.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
        {
            Console.WriteLine("Operation aborted by the user.");
            return;
        }

        Console.WriteLine();

        DirectoryInfo directory = new(path);
        FileInfo[] topFiles = directory.GetFiles();

        if (!topFiles.Any(f => f.Name.Equals("web.config", StringComparison.InvariantCultureIgnoreCase)))
        {
            Console.WriteLine("File to exclude \"web.config\" does not exist. Operation aborted.");
            return;
        }

        if (!topFiles.Any(f => IsAppSettingsFileName(f.Name)))
        {
            Console.WriteLine("No \"appsettings*.json\" file to exclude found. Operation aborted.");
            return;
        }

        if (excludeDirName is not null && !Directory.Exists(Path.Combine(path, excludeDirName)))
        {
            Console.WriteLine($"Directory to exclude \"{excludeDirName}\" does not exist. Operation aborted.");
            return;
        }

        foreach (FileInfo file in topFiles)
        {
            if (!IsExcludedFile(file.Name))
            {
                file.Delete();
                Console.WriteLine($"File \"{file.Name}\" deleted");
            }
        }

        foreach (DirectoryInfo dir in directory.GetDirectories())
        {
            string? keepName = prunedDirs.FirstOrDefault(p => p.DirName.Equals(dir.Name, StringComparison.InvariantCultureIgnoreCase)).KeepName;
            if (keepName is not null)
            {
                DeleteAllExcept(dir, keepName);
                continue;
            }

            if (dir.Name.Equals(excludeDirName, StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            dir.Delete(true);
            Console.WriteLine($"Directory \"{dir.Name}\" deleted");
        }

        Console.WriteLine("Operation completed");
    }

    private static bool IsAppSettingsFileName(string name) =>
        name.Equals("appsettings.json", StringComparison.InvariantCultureIgnoreCase)
        || (name.StartsWith("appsettings.", StringComparison.InvariantCultureIgnoreCase) && name.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase));

    private static bool IsExcludedFile(string name) =>
        name.Equals("web.config", StringComparison.InvariantCultureIgnoreCase) || IsAppSettingsFileName(name);

    private static void DeleteAllExcept(DirectoryInfo dir, string keepDirName)
    {
        foreach (FileInfo file in dir.GetFiles())
        {
            file.Delete();
            Console.WriteLine($"File \"{file.Name}\" deleted");
        }

        foreach (DirectoryInfo subDir in dir.GetDirectories())
        {
            if (!subDir.Name.Equals(keepDirName, StringComparison.InvariantCultureIgnoreCase))
            {
                subDir.Delete(true);
                Console.WriteLine($"Directory \"{subDir.Name}\" deleted");
            }
        }
    }

    private static void WriteLinePlatform(SitePlatform platform)
    {
        ConsoleColor color = platform switch
        {
            SitePlatform.Kentico => ConsoleColor.DarkYellow,
            SitePlatform.Umbraco => ConsoleColor.Blue,
            _ => Console.ForegroundColor
        };

        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(platform);
        Console.ForegroundColor = originalColor;
    }

    private static SitePlatform DetectPlatform(string path)
    {
        bool HasDll(string pattern) => Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly).Any();

        if (HasDll("CMS.*.dll") || HasDll("Kentico.Xperience.*.dll") || Directory.Exists(Path.Combine(path, "assets")))
        {
            return SitePlatform.Kentico;
        }

        if (HasDll("Umbraco.Cms.*.dll") || Directory.Exists(Path.Combine(path, "umbraco")))
        {
            return SitePlatform.Umbraco;
        }

        return SitePlatform.Other;
    }
}