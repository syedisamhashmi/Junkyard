using System.Text.Encodings.Web;
using System.Text.Json;

namespace Junkyard;

public class Constants
{
  public static readonly JsonSerializerOptions SerializerOptions = new()
  {
    AllowTrailingCommas = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    PropertyNameCaseInsensitive = true,
  };
}