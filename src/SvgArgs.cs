using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PowerArgs;

namespace TablerUtils;

public class SvgArgs
{
    [ArgDefaultValue("."), ArgShortcut("-i"), ArgExistingDirectory]
    public string InputFolder { get; set; } = ".";

    [ArgDefaultValue("./output"), ArgShortcut("-o")]
    public string OutputFolder { get; set; } = "./output";
    
    [ArgDefaultValue("#FFFFFF"), ArgShortcut("-c")]
    public string Color { get; set; } = "#FFFFFF";
    
    [ArgDefaultValue("2"), ArgShortcut("-s")]
    public float Stroke { get; set; } = 2;
}