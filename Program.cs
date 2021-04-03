using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using System.Collections.Generic;

using MimeKit;
using HtmlAgilityPack;
using MailKit.Net.Smtp;

namespace Junkyard
{
    class Program
    {
        static async Task Main(string[] args)
        {
            HttpClient client = new HttpClient();
            var stringTask = client.GetStringAsync("https://www.picknpull.com/check_inventory.aspx?Zip=76120&Make=182&Model=3608&Distance=50");
            
            HtmlDocument pageDocument = new HtmlDocument();
            var pageContents = "";
            // try
            // {
            //     // Open the text file using a stream reader.
            //     using (var sr = new StreamReader("PickNPull2.txt"))
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

            pageDocument.LoadHtml(pageContents);

            var body = pageDocument.DocumentNode.ChildNodes.Elements().Where(rec => rec.Name.Contains("body")).FirstOrDefault();
            body.SelectNodes("//div[@id='ctl00_ctl00_MasterHolder_MainContent_resultsDiv']");
            var cars = new List<CarInfo>();
            var temp = new List<CarInfo>();
            int i = 1;
            try 
            {
                while (temp != null)
                {
                    var js = body.CreateNavigator().Evaluate($"substring((//div[@id='ctl00_ctl00_MasterHolder_MainContent_resultsDiv']//span//script)[{i}], 0)").ToString();
                    if (js == null || js == "")
                    {
                        break;
                    }
                    var openArray = js.IndexOf("[");
                    var closeArray = js.IndexOf("]");
                    var array = js.Substring(openArray, closeArray - (openArray - 1));
                    temp = Deserialize(array);
                    if (temp != null)
                    {
                        cars.AddRange(temp);
                    }
                    i++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
                        
            cars.Sort((car1, car2) => int.Parse(car2.Year) - int.Parse(car1.Year));
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            List<string> vins = new List<string>();
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
            bool foundNewCar = false;
            // Write the string array to a new file named "WriteLines.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "SEEN_VIN.txt"), true))
            {
                foreach (var car in cars)
                {
                    if (!vins.Contains(car.VIN) || car.isFiveDaysOld() || car.isTenDaysOld())
                    {
                        outputFile.WriteLine(car.VIN);
                        foundNewCar = true;
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
                                if (!vins.Contains(car.VIN) || car.isFiveDaysOld())
                                {
                                    sb.Append("<tr>");
                                    sb.Append($"<td style='border: solid black 2px;'>{car.Make} - {car.Model} ({car.Year})</td>");
                                    // sb.Append($"<br/>");
                                    sb.Append($"<td style='border: solid black 2px;'>{car.LocationName}</td>");
                                    // sb.Append($"<br/>");
                                    sb.Append($"<td style='border: solid black 2px;'><img src='https://cdn.row52.com/images/{car.Size1}.JPG'></td>");
                                    sb.Append($"<td style='border: solid black 2px;'>{car.VIN}</td>");
                                    sb.Append($"<td style='border: solid black 2px;'>Added {DateTime.Parse(car.DateAdded).ToString("MM/dd/yyyy")}</td>");
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
                                if (!vins.Contains(car.VIN) || car.isTenDaysOld())
                                {
                                    sb.Append("<tr>");
                                    sb.Append($"<td style='border: solid black 2px;'>{car.Make} - {car.Model} ({car.Year})</td>");
                                    // sb.Append($"<br/>");
                                    sb.Append($"<td style='border: solid black 2px;'>{car.LocationName}</td>");
                                    // sb.Append($"<br/>");
                                    sb.Append($"<td style='border: solid black 2px;'><img src='https://cdn.row52.com/images/{car.Size1}.JPG'></td>");
                                    sb.Append($"<td style='border: solid black 2px;'>{car.VIN}</td>");
                                    sb.Append($"<td style='border: solid black 2px;'>Added {DateTime.Parse(car.DateAdded).ToString("MM/dd/yyyy")}</td>");
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
            // smtpClient.Send("***REMOVED***", "***REMOVED***", "Isam's Junkyard 3/20/2021", sb.ToString());
        }
        public static List<CarInfo> Deserialize(string json)
        {
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping

            };

            return JsonSerializer.Deserialize<List<CarInfo>>(json, options);
        }
    }

    [Serializable]
    class CarInfo 
    {
        public long Id {get; set;}
        public string VIN {get; set;}
        public string LocationName {get; set;}
        public string LocationId {get; set;}
        public string Row {get; set;}
        public string DateAdded {get; set;}
        public string Year { get; set;}
        public string Make {get; set;}
        public string Model {get; set;}
        public string ThumbNail {get; set;}
        public string LocationCode {get; set;}
        public string City {get; set;}
        public string State {get; set;}
        public string Zip {get; set;}
        public string Engine {get; set;}
        public string Transmission {get; set;}
        public string Trim {get; set;}
        public string Color {get; set;}
        public string Size1 {get; set;}
        public string BarCodeNumber {get; set;}
        public string ImageDate {get; set;}

        public bool isFiveDaysOld()
        {
            return DateTime.Parse(this.DateAdded).AddDays(5) > DateTime.Now;
        }
        public bool isTenDaysOld()
        {
            return DateTime.Parse(this.DateAdded).AddDays(10) > DateTime.Now;
        }
    }
}
