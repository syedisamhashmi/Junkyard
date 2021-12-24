using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Junkyard;
[Serializable]
public class Subscriber
{
    public string Name { get; set; }
    public string Email { get; set; }
    public static List<Subscriber> Deserialize(string json)
    {
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
        };

        return JsonSerializer.Deserialize<List<Subscriber>>(json, options);
    }
    public static async Task<List<Subscriber>> ReadSubscribers(string subscribersPath)
    {
        try
        {
            using (StreamReader inputFile = new StreamReader(subscribersPath))
            {
                var subscribersJson = await inputFile.ReadToEndAsync();
                return Subscriber.Deserialize(subscribersJson);
            }
        }
        catch
        {
            Console.WriteLine("Subscribers file does not exist.");
            return null;
        }
    }
}