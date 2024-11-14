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
    return JsonSerializer.Deserialize<List<Subscriber>>(json, Constants.SerializerOptions);
  }

  public static async Task<List<Subscriber>> ReadSubscribers(string subscribersPath)
  {
    if (!File.Exists(subscribersPath))
    {
      Console.WriteLine($"{subscribersPath} does not exist. Creating it for you. Please fill it out.");
      var created = File.Create(subscribersPath);
      created.Close();
      return [];
    }
    try
    {
      using var inputFile = new StreamReader(subscribersPath);
      var subscribersJson = await inputFile.ReadToEndAsync();
      return Subscriber.Deserialize(subscribersJson);
    }
    catch
    {
      return null;
    }
  }
}