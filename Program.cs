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

namespace Junkyard;
class Program
{
    private static OutgoingMailBox outgoingMailBox;
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>()
            .Build();
        outgoingMailBox = config.GetSection("OutgoingMailBox").Get<OutgoingMailBox>();

        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/IsamsJunkyard/";

        //? Make requests to PickNPull.
        HttpClient client = new HttpClient();
        List<PickNPullResponse> responses = new List<PickNPullResponse>();
        List<string> urls = await GetRequestUrls(Path.Combine(docPath, "URLS.txt"));
        foreach (var url in urls)
        {
            Console.WriteLine("Getting cars at: " + url);
            var response = PickNPullResponse.Deserialize(await client.GetStringAsync(url));
            responses.AddRange(response);
        }

        //? Build list of car details.
        List<CarInfo> cars = new List<CarInfo>();
        responses.ForEach(response =>
        {
            response.vehicles.ForEach(car =>
            {
                car.location = response.location;
                cars.Add(car);
            });
        });
        cars.Sort((car1, car2) => car2.year - car1.year);

        //? Check to see if we found a new car
        bool foundNewCar = false;
        List<string> vins = await GetSeenVins(Path.Combine(docPath, "SEEN_VIN.txt"));
        using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "SEEN_VIN.txt"), true))
        {
            cars.ForEach(car =>
            {
                if (!vins.Contains(car.vin) || car.isFiveDaysOld() || car.isTenDaysOld())
                {
                    outputFile.WriteLine(car.vin);
                    foundNewCar = true;
                }
            });
        }

        //? No new car found? We're done! :)
        if (!foundNewCar)
        {
            Console.WriteLine("No Cars Found! " + DateTimeOffset.Now.ToString());
            return;
        }

        //? Build e-mail
        var mailMessage = new MimeMessage();
        mailMessage.From.Add(new MailboxAddress(outgoingMailBox.Name, outgoingMailBox.Email));
        //? Add subscribers to email
        List<Subscriber> subscriberList = await Subscriber.ReadSubscribers(Path.Combine(docPath, "SUBSCRIBERS.txt"));
        foreach (var subscriber in subscriberList)
        {
            mailMessage.To.Add(new MailboxAddress(subscriber.Name, subscriber.Email));
        }

        //? Set subject and body
        mailMessage.Subject = "Isam's Junkard " + DateTime.Now.ToString("MM/dd/yyyy");
        mailMessage.Body = new TextPart("html")
        {
            Text = buildEmail(cars, vins)
        };

        //? Send email
        using (var smtpClient = new SmtpClient())
        {
            smtpClient.Connect(outgoingMailBox.Host, outgoingMailBox.Port, outgoingMailBox.UseSSL);
            smtpClient.Authenticate(outgoingMailBox.Email, outgoingMailBox.AccessKey);
            smtpClient.Send(mailMessage);
            smtpClient.Disconnect(true);
        }
    }
    public static string buildEmail(List<CarInfo> cars, List<string> vins)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<html>");
        sb.Append("<body>");
        sb.Append("<div>");
        sb.Append("<table>");
        sb.Append("<tbody>");
        if (cars.Any(car => car.isFiveDaysOld()))
        {
            sb.Append("<tr>");
            sb.Append($"<td style='border: solid black 2px;'>NEW</td>");
            sb.Append($"<td style='border: solid black 2px;'>NEW</td>");
            sb.Append($"<td style='border: solid black 2px;'>NEW</td>");
            sb.Append($"<td style='border: solid black 2px;'>NEW</td>");
            sb.Append($"<td style='border: solid black 2px;'>NEW</td>");
            sb.Append("</tr>");
        }
        foreach (var car in cars.Where(car => car.isFiveDaysOld()).ToList())
        {
            if (!vins.Contains(car.vin) || car.isFiveDaysOld())
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
        if (cars.Any(car => car.isTenDaysOld()))
        {
            sb.Append("<tr>");
            sb.Append($"<td style='border: solid black 2px;'>OLD</td>");
            sb.Append($"<td style='border: solid black 2px;'>OLD</td>");
            sb.Append($"<td style='border: solid black 2px;'>OLD</td>");
            sb.Append($"<td style='border: solid black 2px;'>OLD</td>");
            sb.Append($"<td style='border: solid black 2px;'>OLD</td>");
            sb.Append("</tr>");
        }
        foreach (var car in cars.Where(car => !car.isFiveDaysOld()))
        {
            if (!vins.Contains(car.vin) || car.isTenDaysOld())
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
                var file = File.Create(filePath);
                file.Close();
                return new List<string>();
            }
            using (StreamReader file = new StreamReader(filePath))
            {
                return (await file.ReadToEndAsync())
                    .Split("\n",
                        options: StringSplitOptions.RemoveEmptyEntries |
                                 StringSplitOptions.TrimEntries
                    ).ToList();
                ;
            }
        }
        catch
        {
            Console.WriteLine($"{filePath} does not exist or you do not have permission.");
            return null;
        }

    }
}
