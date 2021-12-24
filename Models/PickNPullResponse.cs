using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Junkyard;

[Serializable]
class PickNPullResponse
{
    public Location location { get; set; }
    public List<CarInfo> vehicles { get; set; }
    public static List<PickNPullResponse> Deserialize(string json)
    {
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
        };

        return JsonSerializer.Deserialize<List<PickNPullResponse>>(json, options);
    }
}