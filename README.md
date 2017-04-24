NodaTime.NetworkClock  [![NuGet Version](https://img.shields.io/nuget/v/NodaTime.NetworkClock.svg?style=flat)](https://www.nuget.org/packages/NodaTime.NetworkClock/) 
=====================

A `NodaTime.IClock` implementation that gets the current time from an NTP server instead of the computer's local clock.

## Installation

```powershell
PM> Install-Package NodaTime.NetworkClock
```

This library is targeting .NET Standard 1.3.  
See the [.NET Standard Platform Support Matrix][1] for further details.

## Example Usage

```csharp
// Just like SystemClock, you can obtain a singleton instance
var clock = NetworkClock.Instance;

// Optionally, you can adjust some settings.
// These are the defaults, and can be omited if you aren't going to change them.
clock.NtpServer = "pool.ntp.org";              // which server to contact
clock.CacheTimeout = Duration.FromMinutes(15); // how long between calls to the server

// Call .GetCurrentInstant() to get the current time as an Instant
Instant now = clock.GetCurrentInstant();

// Like any Instant, you can then convert to a time zone
DateTimeZone tz = DateTimeZoneProviders.Tzdb["America/New_York"];
ZonedDateTime zdt = now.InZone(tz);

// Of course, you can convert this to whatever format makes sense in your application.
// You can use any of the following:
LocalDateTime ldt = zdt.LocalDateTime;
OffsetDateTime odt = zdt.ToOffsetDateTime();
DateTimeOffset dto = zdt.ToDateTimeOffset();
DateTime dt = zdt.ToDateTimeUnspecified();
```

## Notes

Note that technically, the implementation is currently just "SNTP", as it doesn't account for the delay in retrieving the time, and it only makes a single query to the server.   I will probably update it to a full NTP client at some point.  (PR's are welcome!)

[1]: https://docs.microsoft.com/en-us/dotnet/articles/standard/library
