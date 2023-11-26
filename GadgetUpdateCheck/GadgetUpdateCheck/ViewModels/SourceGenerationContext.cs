using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GadgetUpdateCheck.ViewModels;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
public sealed partial class SourceGenerationContext : JsonSerializerContext
{
}
