using IMu;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.Factories;

public class SpecimenFactory : IFactory<Specimen>
{
    public Specimen Make(Map map, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var specimen = new Specimen();

        specimen.SampleId = new[]
        {
            "NMV",
            string.IsNullOrWhiteSpace(map.GetTrimString("ColRegPart"))
                ? $"{map.GetTrimString("ColRegPrefix")} {map.GetTrimString("ColRegNumber")}"
                : $"{map.GetTrimString("ColRegPrefix")} {map.GetTrimString("ColRegNumber")}-{map.GetTrimString("ColRegPart")}"
        }.Concatenate(" ");

        specimen.LithologyTypeId = map.GetTrimString("RocRockName");

        if (map.GetTrimString("ColDiscipline") == "Petrology")
            specimen.LithologyComment = new[]
            {
                string.IsNullOrWhiteSpace(map.GetTrimString("RocRockDescription"))
                    ? null
                    : $"Description: {map.GetTrimString("RocRockDescription")}",
                string.IsNullOrWhiteSpace(map.GetTrimString("RocMainMineralsPresent"))
                    ? null
                    : $"Main Minerals: {map.GetTrimString("RocMainMineralsPresent")}",
                string.IsNullOrWhiteSpace(map.GetTrimString("RocThinSection")) ||
                map.GetTrimString("RocThinSection") != "Yes"
                    ? null
                    : $"Thin Section: {map.GetTrimString("RocThinSection")}",
                string.IsNullOrWhiteSpace(map.GetTrimString("MinChemicalAnalysis")) ||
                map.GetTrimString("MinChemicalAnalysis") != "Yes"
                    ? null
                    : $"Chemical Analysis: {map.GetTrimString("MinChemicalAnalysis")}"
            }.Concatenate(" | ");

        specimen.MineralId = map.GetTrimString("MinSpecies");

        string typeSpecimen = null;
        if (map.GetTrimString("MinType") == "Yes")
            typeSpecimen = map.GetTrimString("MinTypeType");

        if (map.GetTrimString("ColDiscipline") == "Mineralogy")
            specimen.MineralComment = new[]
            {
                string.IsNullOrWhiteSpace(map.GetTrimString("MinSpecies"))
                    ? null
                    : $"Mineral Species: {map.GetTrimString("MinSpecies")}",
                string.IsNullOrWhiteSpace(map.GetTrimString("MinVariety"))
                    ? null
                    : $"Mineral Variety: {map.GetTrimString("MinVariety")}",
                string.IsNullOrWhiteSpace(map.GetTrimString("MinAssociatedMatrix"))
                    ? null
                    : $"Associated Matrix: {map.GetTrimString("MinAssociatedMatrix")}",
                string.IsNullOrWhiteSpace(map.GetTrimString("MinXrayed")) || map.GetTrimString("MinXrayed") != "Yes"
                    ? null
                    : $"X-rayed: {map.GetTrimString("MinXrayed")}",
                string.IsNullOrWhiteSpace(map.GetTrimString("MinChemicalAnalysis")) ||
                map.GetTrimString("MinChemicalAnalysis") != "Yes"
                    ? null
                    : $"Chemical Analysis: {map.GetTrimString("MinChemicalAnalysis")}",
                typeSpecimen == null ? null : $"Type specimen: {typeSpecimen}"
            }.Concatenate(" | ");

        var site = map.GetMap("site");
        if (site != null)
        {
            var latlong = site.GetMaps("latlong").FirstOrDefault(x => x.GetTrimString("LatPreferred_tab") == "Yes");
            string latlongLocationNotes = null;
            string georeferenceAssignedNotes = null;

            if (latlong != null)
            {
                var decimalLatitude = (object[])latlong["LatLatitudeDecimal_nesttab"];
                if (decimalLatitude != null && decimalLatitude.Any(x => x != null))
                    specimen.DecimalLatitude = decimalLatitude.FirstOrDefault(x => x != null)?.ToString();

                var decimalLongitude = (object[])latlong["LatLongitudeDecimal_nesttab"];
                if (decimalLongitude != null && decimalLongitude.Any(x => x != null))
                    specimen.DecimalLongitude = decimalLongitude.FirstOrDefault(x => x != null)?.ToString();

                specimen.LatLongPrecision = new[]
                {
                    latlong.GetTrimString("LatRadiusNumeric_tab"),
                    latlong.GetTrimString("LatRadiusUnit_tab")
                }.Concatenate(" ");

                specimen.GeoreferencedBy = MakePartyName(latlong.GetMap("determinedBy"));
                specimen.DateGeoreferenced = latlong.GetTrimString("LatDetDate0");

                georeferenceAssignedNotes = new[]
                {
                    specimen.GeoreferencedBy,
                    specimen.DateGeoreferenced
                }.Concatenate(", ");

                latlongLocationNotes = new[]
                {
                    specimen.DecimalLatitude,
                    specimen.DecimalLongitude,
                    string.IsNullOrWhiteSpace(latlong.GetTrimString("LatDatum_tab"))
                        ? "datum unknown"
                        : latlong.GetTrimString("LatDatum_tab")
                }.Concatenate(", ");
            }

            var geo = site.GetMaps("geo").FirstOrDefault();
            string geoLocationNotes = null;
            if (geo != null)
            {
                geoLocationNotes = new[]
                {
                    geo.GetTrimString("LocCountry_tab"),
                    geo.GetTrimString("LocProvinceStateTerritory_tab"),
                    geo.GetTrimString("LocDistrictCountyShire_tab"),
                    geo.GetTrimString("LocTownship_tab"),
                    geo.GetTrimString("LocNearestNamedPlace_tab"),
                    geo.GetTrimString("LocPreciseLocation")
                }.Concatenate(", ");

                specimen.LocationName = new[]
                {
                    geo.GetTrimString("LocDistrictCountyShire_tab"),
                    geo.GetTrimString("LocTownship_tab"),
                    geo.GetTrimString("LocNearestNamedPlace_tab"),
                    geo.GetTrimString("LocPreciseLocation")
                }.Concatenate(", ");
            }

            if (site.GetTrimString("EraDepthDeterminationMethod") == "Subsurface - mine/quarry, unknown depth" ||
                site.GetTrimString("EraDepthDeterminationMethod") ==
                "Subsurface - mine/quarry, depth from locality data")
                specimen.LocationKindId = "Mine";
            else if (site.GetTrimString("EraDepthDeterminationMethod") ==
                     "Subsurface - borehole/well, depth unknown" || site.GetTrimString("EraDepthDeterminationMethod") ==
                     "Subsurface - borehole/well, depth from locality data")
                specimen.LocationKindId = "Borehole/well";
            else if (string.IsNullOrWhiteSpace(site.GetTrimString("EraDepthDeterminationMethod")))
                specimen.LocationKindId = "Unknown";

            specimen.LocationNotes = new[]
            {
                latlongLocationNotes,
                string.IsNullOrWhiteSpace(georeferenceAssignedNotes)
                    ? null
                    : $"Georeference assigned: {georeferenceAssignedNotes}",
                geoLocationNotes
            }.Concatenate(" | ");

            specimen.UnitName = site.GetTrimStrings("EraMvRockUnit_tab").Concatenate(", ");

            specimen.UnitAge = new[]
            {
                site.GetTrimString("EraEra"),
                site.GetTrimString("EraAge1"),
                site.GetTrimString("EraAge2"),
                site.GetTrimString("EraMvStage")
            }.Concatenate(", ");

            specimen.DepthMin = site.GetTrimString("EraDepthFromMt");
            specimen.DepthMax = site.GetTrimString("EraDepthToMt");
        }

        if (map.GetTrimString("ColDiscipline") == "Mineralogy")
            specimen.SampleKind = "mineral";
        else if (map.GetTrimString("ColDiscipline") == "Petrology")
            specimen.SampleKind = "rock";

        specimen.SampleTypeId = map.GetMaps("preparations")
            .Select(x => new[]
            {
                x.GetString("StrSpecimenForm_tab")
            })
            .SelectMany(x => x)
            .Distinct()
            .Concatenate(",");

        specimen.ArchiveLocation = "Museums Victoria";
        specimen.ArchiveContact = "AskUs@museum.vic.gov.au";
        specimen.DateCollectedMin = map.GetTrimString("LocDateCollectedFrom");
        specimen.DateCollectedMax = map.GetTrimString("LocDateCollectedTo");

        specimen.Collector = map.GetMaps("collectors").Select(MakePartyName).Concatenate(", ");

        var prevno = map.GetMaps("prevno");

        specimen.PreviousNumber = prevno
            .Where(x => !string.IsNullOrWhiteSpace(x.GetTrimString("ManPreviousCollectionName_tab")))
            .Select(x =>
                new[]
                {
                    x.GetTrimString("ManPreviousCollectionName_tab"),
                    x.GetTrimString("ManPreviousNumbers_tab")
                }.Concatenate(": ")
            ).Concatenate(" | ");

        return specimen;
    }

    public IEnumerable<Specimen> Make(IEnumerable<Map> maps, CancellationToken stoppingToken)
    {
        return maps.Select(map => Make(map, stoppingToken));
    }

    private string MakePartyName(Map map)
    {
        if (map == null) return null;

        switch (map.GetTrimString("NamPartyType"))
        {
            case "Collaboration":
                return new[]
                {
                    map.GetTrimString("ColCollaborationName")
                }.Concatenate(", ");
            case "Cutter Number":
                return new[]
                {
                    map.GetTrimString("NamBranch"),
                    map.GetTrimString("NamDepartment"),
                    map.GetTrimString("NamOrganisation"),
                    map.GetTrimString("AddPhysStreet"),
                    map.GetTrimString("AddPhysCity"),
                    map.GetTrimString("AddPhysState"),
                    map.GetTrimString("AddPhysCountry")
                }.Concatenate(", ");
            case "Organisation":
                return new[]
                {
                    map.GetTrimString("NamBranch"),
                    map.GetTrimString("NamDepartment"),
                    map.GetTrimString("NamOrganisation")
                }.Concatenate(", ");
            case "Person":
                return new[]
                {
                    map.GetTrimString("NamFullName"),
                    map.GetTrimString("NamOrganisation")
                }.Concatenate(" - ");
            case "Position":
                break;
            case "Transport":
                var name = string.Empty;
                var organisationOtherName = map.GetTrimStrings("NamOrganisationOtherNames_tab").FirstOrDefault();
                var source = map.GetTrimString("NamSource");

                if (string.IsNullOrWhiteSpace(organisationOtherName) && !string.IsNullOrWhiteSpace(source))
                    name = source;
                else if (!string.IsNullOrWhiteSpace(organisationOtherName) && string.IsNullOrWhiteSpace(source))
                    name = organisationOtherName;
                else if (!string.IsNullOrWhiteSpace(organisationOtherName) && !string.IsNullOrWhiteSpace(source))
                    name = $"{organisationOtherName} ({source})";

                return new[]
                {
                    name,
                    map.GetTrimString("NamFullName"),
                    map.GetTrimString("NamOrganisation")
                }.Concatenate(", ");
        }

        return null;
    }
}