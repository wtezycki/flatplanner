namespace Flatplanner.Api.Chat;

public static class PlaceLabels
{
    private static readonly Dictionary<string, string> Tags = new()
    {
        ["amenity=school"] = "szkoła",
        ["amenity=kindergarten"] = "przedszkole",
        ["amenity=place_of_worship"] = "miejsce kultu",
        ["amenity=pharmacy"] = "apteka",
        ["amenity=clinic"] = "przychodnia",
        ["amenity=hospital"] = "szpital",
        ["amenity=doctors"] = "gabinet lekarski",
        ["amenity=university"] = "uczelnia",
        ["amenity=veterinary"] = "weterynarz",
        ["amenity=bar"] = "bar",
        ["amenity=pub"] = "pub",
        ["amenity=nightclub"] = "klub nocny",
        ["amenity=restaurant"] = "restauracja",
        ["amenity=cafe"] = "kawiarnia",
        ["amenity=charging_station"] = "stacja ładowania",
        ["amenity=bicycle_rental"] = "wypożyczalnia rowerów",
        ["leisure=park"] = "park",
        ["leisure=playground"] = "plac zabaw",
        ["leisure=dog_park"] = "wybieg dla psów",
        ["leisure=fitness_centre"] = "siłownia",
        ["leisure=sports_centre"] = "centrum sportowe",
        ["leisure=swimming_pool"] = "basen",
        ["leisure=stadium"] = "stadion",
        ["natural=wood"] = "las",
        ["natural=beach"] = "plaża",
        ["landuse=forest"] = "las",
        ["landuse=cemetery"] = "cmentarz",
        ["railway=station"] = "stacja kolejowa",
        ["railway=halt"] = "przystanek kolejowy",
        ["railway=tram_stop"] = "przystanek tramwajowy",
        ["highway=bus_stop"] = "przystanek autobusowy",
        ["shop=supermarket"] = "supermarket",
        ["shop=convenience"] = "sklep spożywczy",
        ["shop=mall"] = "centrum handlowe",
        ["highway=motorway"] = "autostrada",
        ["highway=trunk"] = "droga ekspresowa",
        ["highway=primary"] = "główna ulica",
        ["railway=rail"] = "linia kolejowa",
        ["railway=tram"] = "torowisko tramwajowe",
        ["place=city"] = "centrum miasta",
        ["place=town"] = "centrum miasta",
        ["aeroway=aerodrome"] = "lotnisko",
        ["route=bus"] = "linia autobusowa",
        ["route=trolleybus"] = "linia trolejbusowa",
        ["route=tram"] = "linia tramwajowa",
        ["route=train"] = "linia kolejowa",
        ["route=light_rail"] = "kolej miejska",
    };

    public static IReadOnlyCollection<string> AllTags => Tags.Keys;

    public static string Of(string? name, string tag, int distanceM) =>
        $"{name ?? Tags.GetValueOrDefault(tag, "miejsce")} · {distanceM} m";
}
