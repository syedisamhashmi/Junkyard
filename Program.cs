using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;

using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;

namespace Junkyard;

class Program
{
  private static OutgoingMailBox outgoingMailBox;

  private static string CurrentDirectory => Environment.CurrentDirectory;
  private static string DocumentPath => CurrentDirectory + "/assets/";
  private static string UrlsFile => Path.Combine(DocumentPath, "urls.txt");
  private static string SeenVinsFile => Path.Combine(DocumentPath, "seen_vin.txt");
  private static string SubscribersFile => Path.Combine(DocumentPath, "subscribers.json");

  static async Task Main(string[] args)
  {
    var config = new ConfigurationBuilder()
      .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
      .AddJsonFile("appsettings.json")
      .AddUserSecrets<Program>()
      .Build();
    outgoingMailBox = config
      .GetSection("OutgoingMailBox")
      .Get<OutgoingMailBox>();

    // Grab subscriptions.
    List<Subscriber> subscriberList = await Subscriber.ReadSubscribers(SubscribersFile);
    if (subscriberList is null || subscriberList.Count == 0)
    {
      Console.Error.WriteLine("No subscribers added.");
      Console.WriteLine($"To create a subscriber, edit the JSON file, subscribers.json");
      Console.WriteLine($"({SubscribersFile}), and create an object like:");
      Console.WriteLine("[\n\t{\n\t\t\"name\": \"SomeName\",\n\t\t\"email\":\"subscriber.email@gmail.com\"\n\t}\n]");
      return;
    }

    //? Make requests to PickNPull.
    var client = new HttpClient();
    var responses = new List<PickNPullResponse>();
    List<string> urls = await GetRequestUrls(UrlsFile);
    foreach (var url in urls)
    {
      Console.WriteLine("Getting cars at: " + url);
      var carData = await client.GetStringAsync(url);
      var response = PickNPullResponse.Deserialize(carData);
      responses.AddRange(response);
    }

    //? Build list of car details.
    var cars = new List<CarInfo>();
    responses.ForEach(response =>
      {
        response.vehicles.ForEach(car =>
          {
            car.location = response.location;
            cars.Add(car);
          }
        );
      }
    );
    cars.Sort((car1, car2) => car2.year - car1.year);

    //? Check to see if we found a new car
    bool foundNewCar = false;
    List<string> vins = await GetSeenVins(SeenVinsFile);

    using StreamWriter outputFile = new StreamWriter(SeenVinsFile, true);
    cars.ForEach(car =>
      {
        if (
          !vins.Contains(car.vin) ||
          car.IsFiveDaysOld() ||
          car.IsTenDaysOld()
        )
        {
          outputFile.WriteLine(car.vin);
          foundNewCar = true;
        }
      }
    );

    //? No new car found? We're done! :)
    if (!foundNewCar)
    {
      Console.WriteLine("No Cars Found! " + DateTimeOffset.Now.ToString());
      return;
    }

    //? Build e-mail
    var mailMessage = new MimeMessage();

    mailMessage.From.Add(
      new MailboxAddress(
        outgoingMailBox.Subject,
        outgoingMailBox.Name
      )
    );

    //? Add subscribers to email
    foreach (var subscriber in subscriberList)
    {
      mailMessage.To.Add(
        new MailboxAddress(
          subscriber.Name,
          subscriber.Email
        )
      );
    }

    //? Set subject and body
    mailMessage.Subject = "Isam's Junkard " + DateTime.Now.ToString("MM/dd/yyyy");
    mailMessage.Body = new TextPart(TextFormat.Html)
    {
      Text = BuildEmail(cars, vins)
    };

    //? Send email
    using var smtpClient = new SmtpClient();
    smtpClient.Connect(outgoingMailBox.Host, outgoingMailBox.Port, outgoingMailBox.UseSSL);
    smtpClient.Authenticate(outgoingMailBox.Email, outgoingMailBox.AccessKey);
    smtpClient.Send(mailMessage);
    smtpClient.Disconnect(true);
  }

  public static string BuildEmail(List<CarInfo> cars, List<string> vins)
  {
    var sb = new StringBuilder();
    sb.Append("<html>");
    sb.Append("<body>");
    sb.Append("<div>");
    sb.Append("<table>");
    sb.Append("<tbody>");
    if (cars.Any(car => car.IsFiveDaysOld()))
    {
      sb.Append("<tr>");
      sb.Append("  <td style='border: solid black 2px;'>NEW</td>");
      sb.Append("  <td style='border: solid black 2px;'>NEW</td>");
      sb.Append("  <td style='border: solid black 2px;'>NEW</td>");
      sb.Append("  <td style='border: solid black 2px;'>NEW</td>");
      sb.Append("  <td style='border: solid black 2px;'>NEW</td>");
      sb.Append("</tr>");
    }
    foreach (var car in cars.Where(car => car.IsFiveDaysOld()).ToList())
    {
      if (!vins.Contains(car.vin) || car.IsFiveDaysOld())
      {
        sb.Append("<tr>");
        sb.Append($"<td style='border: solid black 2px;'>{car.make} - {car.model} ({car.year})</td>");
        sb.Append($"<td style='border: solid black 2px;'>{car.locationName}</td>");
        sb.Append($"<td style='border: solid black 2px;'><img src='https://cdn.row52.com/images/{car.size1}.JPG'></td>");
        sb.Append($"<td style='border: solid black 2px;'>{car.vin}</td>");
        sb.Append($"<td style='border: solid black 2px;'>Added {DateTime.Parse(car.dateAdded).ToString("MM/dd/yyyy")}</td>");
        sb.Append("</tr>");
      }
    }
    if (cars.Any(car => car.IsTenDaysOld()))
    {
      sb.Append("<tr>");
      sb.Append($"<td style='border: solid black 2px;'>OLD</td>");
      sb.Append($"<td style='border: solid black 2px;'>OLD</td>");
      sb.Append($"<td style='border: solid black 2px;'>OLD</td>");
      sb.Append($"<td style='border: solid black 2px;'>OLD</td>");
      sb.Append($"<td style='border: solid black 2px;'>OLD</td>");
      sb.Append("</tr>");
    }
    foreach (var car in cars.Where(car => !car.IsFiveDaysOld()))
    {
      if (!vins.Contains(car.vin) || car.IsTenDaysOld())
      {
        sb.Append("<tr>");
        sb.Append($"<td style='border: solid black 2px;'>{car.make} - {car.model} ({car.year})</td>");
        sb.Append($"<td style='border: solid black 2px;'>{car.locationName}</td>");
        sb.Append($"<td style='border: solid black 2px;'><img src='https://cdn.row52.com/images/{car.size1}.JPG'></td>");
        sb.Append($"<td style='border: solid black 2px;'>{car.vin}</td>");
        sb.Append($"<td style='border: solid black 2px;'>Added {DateTime.Parse(car.dateAdded).ToString("MM/dd/yyyy")}</td>");
        sb.Append("</tr>");
      }
    }
    sb.Append("</tbody>");
    sb.Append("</table>");
    sb.Append("</div>");
    sb.Append("</body>");
    sb.Append("</html>");
    return sb.ToString();
  }

  public static async Task<List<string>> GetRequestUrls(string urlsFilePath)
  {
    return await GetLinesArrayFromFile(urlsFilePath);
  }

  public static async Task<List<string>> GetSeenVins(string vinFilePath)
  {
    return await GetLinesArrayFromFile(vinFilePath);
  }

  public static async Task<List<string>> GetLinesArrayFromFile(string filePath)
  {
    try
    {
      if (!File.Exists(filePath))
      {
        Console.WriteLine($"{filePath} does not exist. Creating it for you. Please fill it out if necessary.");
        var created = File.Create(filePath);
        created.Close();
        return new List<string>();
      }
      using var file = new StreamReader(filePath);
      var fileContents = await file.ReadToEndAsync();
      return fileContents
        .Split(
          "\n",
          options:
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries
        )
        .ToList();
    }
    catch
    {
      Console.WriteLine($"{filePath} does not exist or you do not have permission.");
      return null;
    }
  }
}
