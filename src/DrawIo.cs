using System.Security;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TablerUtils;

public static class Tags
{
    public static Dictionary<string, TagsEntry>? LoadTags(string jsonFile)
    {
        var obj = JObject.Parse(File.ReadAllText(jsonFile));
        return obj.ToObject<Dictionary<string, TagsEntry>>();
    }
}

public record TagsEntry(
    string name,
    string category,
    string[] tags,
    string version,
    string unicode
);

public record LibraryEntry(
    string xml,
    int w,
    int h,
    string title,
    string aspect = "fixed"
) {
    public static LibraryEntry Make(Svg svg, string title)
    {
        var svgBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(svg.Serialize()));

        var shapeData =
            $"""
            shape=image;
            editableCssRules=\.icon(-filled)?;
            verticalLabelPosition=bottom;
            verticalAlign=top;
            imageAspect=0;
            aspect=fixed;
            connectable=0;
            image=data:image/svg+xml,{svgBase64};
            """.ReplaceLineEndings().Replace(Environment.NewLine, "");

        var entryData = XDocument.Parse(
            $"""
            <mxGraphModel>
                <root>
                    <mxCell id="0" />
                    <mxCell id="1" parent="0" />
                    <mxCell
                        id="2"
                        value=""
                        style="{shapeData}"
                        vertex="1"
                        parent="1"
                    >
                        <mxGeometry width="24" height="24" as="geometry" />
                    </mxCell>
                </root>
            </mxGraphModel>
            """).ToString(SaveOptions.DisableFormatting);
        var escapedXml = JsonConvert.ToString(SecurityElement.Escape(entryData))
            .Trim('"');
        
        return new(escapedXml, 24, 24, title);
    }
}

public record Library(string Name, string Content)
{
    public static Library Make(string Name, IEnumerable<LibraryEntry> entries)
    {
        var jArray = JArray.FromObject(entries);
        return new(Name,
            $"""
            <mxlibrary>{jArray.ToString(Formatting.Indented)}</mxlibrary>
            """
        );
    }
}