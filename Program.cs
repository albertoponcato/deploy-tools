using Cocona;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

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

        app.Run();
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
                    .Where(a => a.GetAttributeValue("href", string.Empty).StartsWith('/') && a.GetAttributeValue("href", string.Empty).EndsWith(".html"));

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

    private static void PrePublishCleaning()
    {
        string[] excludesFile = ["appsettings.json", "web.config"];
        string[] excludesDirectory = ["assets"];

        var path = Environment.CurrentDirectory;

        Console.WriteLine();
        Console.WriteLine($"This command will delete all files except [{string.Join(", ", excludesFile)}] and all folders except [{string.Join(", ", excludesDirectory)}]");

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

        foreach (string excludeFile in excludesFile)
        {
            FileInfo fileToExclude = new(Path.Combine(path, excludeFile));
            if (!fileToExclude.Exists)
            {
                Console.WriteLine($"File to exclude \"{excludeFile}\" does not exist. Operation aborted.");
                return;
            }
        }

        foreach (string excludeDir in excludesDirectory)
        {
            DirectoryInfo dirToExclude = new(Path.Combine(path, excludeDir));
            if (!dirToExclude.Exists)
            {
                Console.WriteLine($"Directory to exclude \"{excludeDir}\" does not exist. Operation aborted.");
                return;
            }
        }

        foreach (FileInfo file in directory.GetFiles())
        {
            if (!excludesFile.Contains(file.Name, StringComparer.InvariantCultureIgnoreCase))
            {
                file.Delete();
                Console.WriteLine($"File \"{file.Name}\" deleted");
            }
        }

        foreach (DirectoryInfo dir in directory.GetDirectories())
        {
            if (!excludesDirectory.Contains(dir.Name, StringComparer.InvariantCultureIgnoreCase))
            {
                dir.Delete(true);
                Console.WriteLine($"Directory \"{dir.Name}\" deleted");
            }
        }

        Console.WriteLine("Operation completed");
    }
}