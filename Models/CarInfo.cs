using System;
using System.Text.Json.Serialization;

namespace Junkyard;

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