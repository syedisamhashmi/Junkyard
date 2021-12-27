# Junkyard
## Some backstory:
I bought a beat up car and fixed it up with my brother!

Part of the process was pulling up PickNPull's website EVERY DAY to find out if a new car in the same year/make/model came into the yard. Tedious, right?

Well, as all software engineers do, we write a quick and simple program `(translation: a few all nighters on weekends because we can't put the code down)` to just do it for us and then automate it to send out the email every morning at 8am so we can hit the junkyard after work.

As a result, I was able to fix my AC, grab various parts for my car, help my brother convert his car from an *automatic* to a *manual*, fix my little-brother-in-law's car in a MYRIAD of different ways...

At 1/10th the cost of actually buying the parts brand new. :)

New AC compressor OEM: $500

Used AC compressor OEM from junkyard: $50-$70. (get the hint, yet?)

Oh, also, the money I saved, I bought tools! Now I have a garage full of more tools than I know what to do with....

</br>
</br>

### <b>TL;DR</b>: 
### Check junkyard inventory, email yourself and family *_automagically_*.
</br>
</br>

---

## To set up:
1.) Run `dotnet user-secrets init` in the project directory 

```
(I `suggest` you remove `<UserSecretsId>661bb1cf-857e-4f4e-9d31-df561db8d894</UserSecretsId>` from the .csproj file beforehand, it will autogen that line for you if you run the command, idk, run at your own risk without deleting)
```

-----

2.) Add keys for "Name", "Email", "AccessKey", "Host", and "Port" on the "OutgoingMailBox" section by running:

`dotnet user-secrets set "OutgoingMailBox:Name" "{{MAILBOX_NAME_HERE}}"` (Shows the user who sent the email.... I think?)

`dotnet user-secrets set "OutgoingMailBox:Email" "{{MAILBOX_EMAIL_HERE}}"` (The actual email for login)

`dotnet user-secrets set "OutgoingMailBox:AccessKey" "{{MAILBOX_ACCESSKEY_HERE}}"` (I don't really want to say the word, but you know... the key you use to access your email... it goes with the username...)

---
2.5) Some server settings for the email to actually communicate below, you may have you look these up depending on your email provider...

`dotnet user-secrets set "OutgoingMailBox:Host" "{{HOST_HERE}}"` (I used "smtp.gmail.com" for my gmail account)

`dotnet user-secrets set "OutgoingMailBox:Port" {{PORT_HERE}}` (I used "465" for my gmail account)

`dotnet user-secrets set "OutgoingMailBox:UseSSL" {{UseSSL_HERE}}` (I used 'true' for my gmail account)

-----

(If ALL the above doesn't work, go to `~/.microsoft/usersecrets/{{GENERATED_GUID_MATCHING_CSPROJ}}/secrets.json` and edit it to match the `appsettings.json` there manually.)-----

3.) Inside of the docs path create two files

`SUBSCRIBERS.txt` - Contains a JSON array of people subscribed to this service, ex:
```
[
    {
        "name": "Foo"
        "email": "bar@baz.com"
    },
    ...,
    {
        "name": "Syed Isam Hashmi",
        "email": "is@mhashmi.com" (feel free to contact me if you actually get a car part out of this :)
    },
    ...
]
```

and 

`URLS.txt` - NewLine delimitted file containing API URLs for PickNPull, ex:
```
https://www.picknpull.com/api/vehicle/search?&makeId=90&modelId=1150&year=2004-2006&distance=250&zip=75050
https://www.picknpull.com/api/vehicle/search?&makeId=182&modelId=3608&year=2004-2006&distance=250&zip=75050
```
(I only have two cars on my watchlist, so... two URLs)

---

# **_Appendix_**
How to get those URLS?

1.) Go to picknpull.com

`(in chrome at least, right-click and inspect, go to network tab, other browsers?, no clue)`

2.) Proceed to do a search.

3.) In the network panel, find the row starting with `search?`

4.) Click the row

5.) Click the headers tab

6.) Copy the request URL into that `URLS.txt` file as a new row.

7.) Done!


---
Automating
---

- Linux

You can run `crontab -e` to edit your cron file and add something like this at the end:

`0 7 * * * dotnet exec {{PATH_TO_PROJECT}}/Junkyard.dll`

- Windows

Windows has a scheduling assistant that can be used, pretty intituitive from some google searches.

<br>
<br>
<br>

# ***If this saves you money! Good job! :)***
# ***If it hasn't yet, it will!***
# ***Best of luck!***

If you change this to work with another Junkyard service provider or find a bug, please submit a PR.
If you just enjoy using it, send me an email, I would love to hear about any mechanic work or conquests this may have help you achieve! :) 
