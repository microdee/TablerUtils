using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using PowerArgs;

namespace TablerUtils;

public record Svg(XDocument Document)
{
    public record Styling(string Color, float Stroke);

    public static Svg Load(string path) => new(XDocument.Load(path));

    public Svg WithStyling(Styling styling)
    {
        var newDoc = new XDocument(Document);
        newDoc.XPathSelectElements("//*").SelectMany(el => el.Attributes()).ForEach(attr =>
        {
            if (attr.Value.Equals("currentColor", StringComparison.InvariantCultureIgnoreCase))
            {
                attr.SetValue(styling.Color);
            }
        });
        newDoc.Root?.AddFirst(XElement.Parse(
            $$"""
            <style type="text/css">
                .icon{stroke:{{styling.Color}};}
                .icon-filled{fill:{{styling.Color}};}
            </style>
            """
        ));
        newDoc.XPathSelectElements("//*").ForEach(el => {
            if (el.Name.LocalName != "path") return;
            
            var fillAttr = el.Attribute(XName.Get("fill"));
            if (fillAttr != null && !fillAttr.Value.Equals("none", StringComparison.InvariantCultureIgnoreCase))
            {
                fillAttr.SetValue(styling.Color);
                el.SetAttributeValue(XName.Get("class"), "icon-filled");
            }
        });
        newDoc.Root?.SetAttributeValue(XName.Get("stroke-width"), styling.Stroke);

        return this with { Document = newDoc };
    }

    public string Serialize()
    {
        return Document.ToString(SaveOptions.DisableFormatting).Replace(" xmlns=\"\"", "");
    }
}