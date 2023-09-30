using System.Net;
using System.Text.RegularExpressions;
using ImageMagick;
using Konsole;
using PowerArgs;

namespace TablerUtils;
public class Program
{
    public class DrawIOArgs : SvgArgs
    {
        [ArgDefaultValue("Tabler Icons"), ArgShortcut("-l")]
        public string LibraryPrefix { get; set; } = "Tabler Icons";
    }

    [ArgActionMethod]
    public void DrawIO(DrawIOArgs args)
    {
        var tagsJson = Path.Combine(args.InputFolder, "tags.json");
        if (!File.Exists(tagsJson))
        {
            Console.WriteLine("Fetching tags");
            using var client = new HttpClient();
            using var webStream = client.GetStreamAsync("https://raw.githubusercontent.com/tabler/tabler-icons/master/packages/icons/tags.json");
            using var fileStream = new FileStream(tagsJson, FileMode.OpenOrCreate);
            webStream.Result.CopyTo(fileStream);
        };
        var tags = Tags.LoadTags(tagsJson);
        if (tags == null)
        {
            throw new Exception("Couldn't parse the tags file");
        }
        var categories = tags.Values.DistinctBy(t => t.category).Select(t => t.category);
        Console.WriteLine("Categories:");
        foreach(var category in categories)
        {
            Console.WriteLine("  -" + category);
        }

        Console.WriteLine("Collecting SVG files");
        var svgFiles = Directory.EnumerateFiles(args.InputFolder, "*.svg").ToList();
        var progress = new ProgressBar(PbStyle.DoubleLine, svgFiles.Count);
        var svgTemp = Path.Combine(args.OutputFolder, ".svgTemp");
        
        if (!Directory.Exists(svgTemp))
        {
            Directory.CreateDirectory(svgTemp);
        }

        List<LibraryEntry> allEntries = svgFiles
            .Select(file =>
            {
                progress.Next(Path.GetFileName(file));
                var svgOut = Svg.Load(file).WithStyling(new(args.Color, args.Stroke));
                File.WriteAllText(Path.Combine(svgTemp, Path.GetFileName(file)), svgOut.Serialize());
                return LibraryEntry.Make(svgOut, Path.GetFileNameWithoutExtension(file));
            })
            .ToList();
        Console.WriteLine("Sorting into library files");

        bool IsBrand(LibraryEntry e) =>
            tags[e.title].category.Equals(
                "Brand", StringComparison.InvariantCultureIgnoreCase
            );

        var entriesSansBrands = allEntries.Where(e => !IsBrand(e));

        var libraries = "abcdefghijklmnopqrstuvxyz"
            .Select(c => Library.Make(
                $"{args.LibraryPrefix} {args.Stroke} {c.ToString().ToUpper()}",
                entriesSansBrands.Where(e => c switch
                {
                    'a' => Regex.IsMatch(e.title, @"^[a\W\d_]", RegexOptions.IgnoreCase),
                    _ => e.title.ToLower()[0] == c
                })
            ))
            .Append(Library.Make(
                $"{args.LibraryPrefix} {args.Stroke} Brands",
                allEntries.Where(IsBrand)
            ));

        if (!Directory.Exists(args.OutputFolder))
        {
            Directory.CreateDirectory(args.OutputFolder);
        }

        foreach(var lib in libraries)
        {
            File.WriteAllText(
                Path.Combine(args.OutputFolder, lib.Name + ".xml"),
                lib.Content
            );
        }
    }

    public static void Main(string[] args)
    {
        Args.InvokeAction<Program>(args);
    }
}