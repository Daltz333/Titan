using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Titan.Models.Json
{
    public record struct SysIdSimpleTest
    {
        [JsonPropertyName("sysid")]
        public bool SysID { get; set; } = true;

        [JsonPropertyName("test")]
        public string TestType { get; set; } = "Simple";

        [JsonPropertyName("units")]
        public string Units { get; set; } = "Rotations";

        [JsonPropertyName("unitsPerRotation")]
        public double UnitsPerRotation { get; set; } = 1.0;

        [JsonPropertyName("fast-forward")]
        public double[][]? FastForward { get; set; }

        [JsonPropertyName("fast-backward")]
        public double[][]? FastBackward { get; set; }

        [JsonPropertyName("slow-forward")]
        public double[][]? SlowForward { get; set; }

        [JsonPropertyName("slow-backward")]
        public double[][]? SlowBackward { get; set; }

        public SysIdSimpleTest()
        {
        }
    }
}
