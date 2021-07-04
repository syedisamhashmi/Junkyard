using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using System.Collections.Generic;

using MimeKit;
using MailKit.Net.Smtp;
using System.Text.Json.Serialization;



namespace Junkyard
{
    class Program
    {
        static async Task Main(string[] args)
        {
            HttpClient client = new HttpClient();

            bool foundNewCar = false;
            List<string> vins = new List<string>();
            List<string> urls = new List<string>();
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            using (StreamReader inputFile = new StreamReader(Path.Combine(docPath, "URLS.txt")))
            {
                var line = "";
                while (line != null)
                {
                    line = inputFile.ReadLine();
                    if (String.IsNullOrEmpty(line) || String.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    urls.Add(line);
                }
            }


            var cars = new List<CarInfo>();
            foreach (var arg in urls)
            {
                Console.WriteLine("Getting cars at: " + arg);
                var stringTask = client.GetStringAsync(arg);

                var pageContents = "";
                // try
                // {
                //     // Open the text file using a stream reader.
                //     using (var sr = new StreamReader("PickNPull1.txt"))
                //     {
                //         // Read the stream as a string, and write the string to the console.
                //         pageContents += await sr.ReadToEndAsync();
                //     }
                // }
                // catch (IOException e)
                // {
                //     Console.WriteLine("The file could not be read:");
                //     Console.WriteLine(e.Message);
                // }
                
                pageContents = await stringTask;
                var response = Deserialize(pageContents);

                
                response.ForEach(rec =>
                {
                    rec.vehicles.ForEach(car => {
                        car.location = rec.location;
                        cars.Add(car);
                    });
                });
                
                cars.Sort((car1, car2) => car2.year - car1.year);

                try 
                {
                    using (StreamReader inputFile = new StreamReader(Path.Combine(docPath, "SEEN_VIN.txt")))
                    {
                        var line = "";
                        while (line != null)
                        {
                            line = inputFile.ReadLine();
                            vins.Add(line);
                        }
                    }

                }
                catch
                {
                    Console.WriteLine("File doesnt exist I guess.");
                }
                // Write the string array to a new file named "WriteLines.txt".
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "SEEN_VIN.txt"), true))
                {
                    foreach (var car in cars)
                    {
                        if (!vins.Contains(car.vin) || car.isFiveDaysOld() || car.isTenDaysOld())
                        {
                            outputFile.WriteLine(car.vin);
                            foundNewCar = true;
                        }
                    }
                }
            }

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
                                    // sb.Append($"<br/>");
                                    sb.Append($"<td style='border: solid black 2px;'>{car.locationName}</td>");
                                    // sb.Append($"<br/>");
                                    sb.Append($"<td style='border: solid black 2px;'><img src='https://cdn.row52.com/images/{car.size1}.JPG'></td>");
                                    sb.Append($"<td style='border: solid black 2px;'>{car.vin}</td>");
                                    sb.Append($"<td style='border: solid black 2px;'>Added {DateTime.Parse(car.dateAdded).ToString("MM/dd/yyyy")}</td>");
                                    // sb.Append($"<br/>");
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
                                    // sb.Append($"<br/>");
                                    sb.Append($"<td style='border: solid black 2px;'>{car.locationName}</td>");
                                    // sb.Append($"<br/>");
                                    sb.Append($"<td style='border: solid black 2px;'><img src='https://cdn.row52.com/images/{car.size1}.JPG'></td>");
                                    sb.Append($"<td style='border: solid black 2px;'>{car.vin}</td>");
                                    sb.Append($"<td style='border: solid black 2px;'>Added {DateTime.Parse(car.dateAdded).ToString("MM/dd/yyyy")}</td>");
                                    // sb.Append($"<br/>");
                                    sb.Append("</tr>");
                                }
                            }
                            sb.Append("</tbody>");
                        sb.Append("</table>");
                    sb.Append("</div>");
                sb.Append("</body>");
            sb.Append("</html>");

            var mailMessage = new MimeMessage();
            mailMessage.From.Add(new MailboxAddress("***REMOVED***", "***REMOVED***"));
            mailMessage.To.Add(new MailboxAddress("Isam Hashmi", "***REMOVED***" ));
            // mailMessage.To.Add(new MailboxAddress("Isam Hashmi", "***REMOVED***@txt.att.net" ));

            mailMessage.To.Add(new MailboxAddress("***REMOVED***", "***REMOVED***" ));
            // mailMessage.To.Add(new MailboxAddress("***REMOVED***", "***REMOVED***@txt.att.net" ));

            mailMessage.Subject = "Isam's Junkard " + DateTime.Now.ToString("MM/dd/yyyy");
            mailMessage.Body = new TextPart("html")
            {
                Text = sb.ToString()
            };

            if (foundNewCar)
            {
                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Connect("smtp.gmail.com", 465, true);
                    smtpClient.Authenticate("***REMOVED***", "***REMOVED***");
                    smtpClient.Send(mailMessage);
                    smtpClient.Disconnect(true);
                }
            }
            else
            {
                Console.WriteLine("No Cars Found! " + DateTimeOffset.Now.ToString());
            }
        }
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

    [Serializable]
    class PickNPullResponse 
    {
        public Location location { get; set; }
        public List<CarInfo> vehicles { get; set; }
    }
    [Serializable]
    class Location 
    {
        public string name { get; set; }
    }

    [Serializable]
    class CarInfo 
    {
        public long id {get; set;}
        public string vin {get; set;}
        public string locationName {get; set;}
        public int locationId {get; set;}
        public string row {get; set;}
        public string dateAdded {get; set;}
        public int year { get; set;}
        public string make {get; set;}
        public string model {get; set;}
        public string thumbNail {get; set;}
        // public string LocationCode {get; set;}
        // public string City {get; set;}
        // public string State {get; set;}
        // public string Zip {get; set;}
        public string engine {get; set;}
        public string transmission {get; set;}
        public string trim {get; set;}
        public string color {get; set;}
        public string size1 {get; set;}
        public string size2 {get; set;}
        public string size3 {get; set;}
        public string size4 {get; set;}
        public string barCodeNumber {get; set;}
        public string imageDate {get; set;}

        [JsonIgnore]
        public Location location{ get; set; }

        public bool isFiveDaysOld()
        {
            return DateTime.Parse(this.dateAdded).AddDays(5) > DateTime.Now;
        }
        public bool isTenDaysOld()
        {
            return DateTime.Parse(this.dateAdded).AddDays(10) > DateTime.Now;
        }
    }
}
