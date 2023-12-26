using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Titan.Models.Json.SerializationContext
{
    [JsonSerializable(typeof(SysIdSimpleTest))]
    public partial class SimpleTestContext : JsonSerializerContext
    {
    }
}
