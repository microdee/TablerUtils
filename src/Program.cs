using System.Net;
using System.Text.RegularExpressions;
using ImageMagick;
using Konsole;
using PowerArgs;

namespace TablerUtils;
public class Program
{
    private static Dictionary<string, TagsEntry> GetTags(SvgArgs args)
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
        return tags;
    }

    private static (IEnumerable<string> svgFiles, string svgTemp, ProgressBar progress) GetSvgFiles(SvgArgs args)
    {
        var svgFiles = Directory.EnumerateFiles(args.InputFolder, "*.svg").ToList();
        var progress = new ProgressBar(PbStyle.DoubleLine, svgFiles.Count);
        var svgTemp = Path.Combine(args.OutputFolder, ".svgTemp");
        
        if (!Directory.Exists(svgTemp))
        {
            Directory.CreateDirectory(svgTemp);
        }
        return (svgFiles, svgTemp, progress);
    }

    private static (Svg svg, string fileOut) RenderSvg(SvgArgs args, string file, string svgTemp)
    {
        var svgOut = Svg.Load(file).WithStyling(new(args.Color, args.Stroke));
        var fileOut = Path.Combine(svgTemp, Path.GetFileName(file));
        File.WriteAllText(fileOut, svgOut.Serialize());
        return (svgOut, fileOut);
    }

    public class RenderToPngArgs : SvgArgs
    {
        [ArgDefaultValue("24"), ArgShortcut("-r")]
        public int Resize { get; set; } = 24;
    }

    [ArgActionMethod]
    public void Png(RenderToPngArgs args)
    {
        Console.WriteLine("Initializing ImageMagick");
        MagickNET.Initialize();
        Console.WriteLine("Collecting SVG files");
        var (svgFiles, svgTemp, progress) = GetSvgFiles(args);
        
        if (!Directory.Exists(args.OutputFolder))
        {
            Directory.CreateDirectory(args.OutputFolder);
        }
        int currentProgress = 0;

        svgFiles.AsParallel().ForAll(file =>
        {
            Interlocked.Add(ref currentProgress, 1);
            progress.Refresh(currentProgress, Path.GetFileName(file));
            var (svgOut, fileOut) = RenderSvg(args, file, svgTemp);

            var imageIn = new MagickImage(fileOut, new MagickReadSettings
            {
                BackgroundColor = new MagickColor(0, 0, 0, 0),
                Width = args.Resize,
                Height = args.Resize,
                ColorSpace = ColorSpace.sRGB
            });

            imageIn.Write(
                Path.Combine(args.OutputFolder, Path.GetFileNameWithoutExtension(file) + ".png")
            );
        });
    }

    public class DrawIOArgs : SvgArgs
    {
        [ArgDefaultValue("Tabler Icons"), ArgShortcut("-l")]
        public string LibraryPrefix { get; set; } = "Tabler Icons";
    }

    [ArgActionMethod]
    public void DrawIO(DrawIOArgs args)
    {
        var tags = GetTags(args);
        var categories = tags.Values.DistinctBy(t => t.category).Select(t => t.category);
        Console.WriteLine("Categories:");
        foreach(var category in categories)
        {
            Console.WriteLine("  -" + category);
        }

        Console.WriteLine("Collecting SVG files");
        var (svgFiles, svgTemp, progress) = GetSvgFiles(args);

        List<LibraryEntry> allEntries = svgFiles
            .Select(file =>
            {
                progress.Next(Path.GetFileName(file));
                var (svgOut, _) = RenderSvg(args, file, svgTemp);
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