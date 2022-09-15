using System.Globalization;
using IMu;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.Factories;

public class SampleFactory : IImuFactory<Sample>
{
    private readonly IImuFactory<Image> _imageFactory;
    
    public SampleFactory(
        IImuFactory<Image> imageFactory)
    {
        _imageFactory = imageFactory;
    }
    
    public Sample Make(Map map, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var sample = new Sample();

        sample.Irn = map.GetTrimString("irn");
        sample.Name = new[]
        {
            "NMV",
            string.IsNullOrWhiteSpace(map.GetTrimString("ColRegPart"))
                ? $"{map.GetTrimString("ColRegPrefix")} {map.GetTrimString("ColRegNumber")}"
                : $"{map.GetTrimString("ColRegPrefix")} {map.GetTrimString("ColRegNumber")}-{map.GetTrimString("ColRegPart")}"
        }.Concatenate(" ");

        if (map.GetTrimStrings("ColCollectionName_tab").Any())
            sample.ArchiveNotes = map.GetMaps("prevno")
                .Where(x => !string.IsNullOrWhiteSpace(x.GetTrimString("ManPreviousCollectionName_tab")))
                .Select(x =>
                    new[]
                    {
                        x.GetTrimString("ManPreviousCollectionName_tab"),
                        x.GetTrimString("ManPreviousNumbers_tab")
                    }.Concatenate(": ")
                ).Concatenate(" | ");

        var site = map.GetMap("site");
        if (site != null)
        {
            var latlong = site.GetMaps("latlong").FirstOrDefault(x => x.GetTrimString("LatPreferred_tab") == "Yes");
            string latlongLocationNotes = null;
            string georeferenceAssignedNotes = null;
            string determinationMethod = null;

            if (latlong != null)
            {
                var decimalLatitude = (object[])latlong["LatLatitudeDecimal_nesttab"];
                if (decimalLatitude != null && decimalLatitude.Any(x => x != null))
                    sample.Latitude = decimalLatitude.FirstOrDefault(x => x != null)?.ToString();

                var decimalLongitude = (object[])latlong["LatLongitudeDecimal_nesttab"];
                if (decimalLongitude != null && decimalLongitude.Any(x => x != null))
                    sample.Longitude = decimalLongitude.FirstOrDefault(x => x != null)?.ToString();

                sample.LatLongPrecision = latlong.GetTrimString("LatRadiusNumeric_tab");

                if (DateTime.TryParseExact(latlong.GetTrimString("LatDetDate0"), "dd/MM/yyyy",
                        new CultureInfo("en-AU"), DateTimeStyles.None, out var dateGeoreferenced))
                    sample.DateGeoreferenced = dateGeoreferenced.ToString("yyyy-MM-dd");

                georeferenceAssignedNotes = new[]
                {
                    MakePartyName(latlong.GetMap("determinedBy")),
                    latlong.GetTrimString("LatDetDate0")
                }.Concatenate(", ");

                latlongLocationNotes = string.IsNullOrWhiteSpace(latlong.GetTrimString("LatDatum_tab"))
                    ? "datum unknown"
                    : latlong.GetTrimString("LatDatum_tab");

                determinationMethod = latlong.GetTrimString("LatLatLongDetermination_tab");
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
            }

            // Properties:DescriptiveLocality
            if (!string.IsNullOrWhiteSpace(geoLocationNotes))
                sample.Properties.Add(new SampleProperty
                {
                    Property = new KeyValuePair<string, string>("DescriptiveLocality", geoLocationNotes),
                    Order = 3
                });
            
            // Properties:GeoreferenceDetails
            var georeferenceDetails = new[]
            {
                georeferenceAssignedNotes,
                determinationMethod,
                latlongLocationNotes,
                
            }.Concatenate(" | ");
            if (!string.IsNullOrWhiteSpace(georeferenceDetails))
                sample.Properties.Add(new SampleProperty()
                {
                    Property = new KeyValuePair<string, string>("GeoreferenceDetails", georeferenceDetails),
                    Order = 4
                });

            sample.LocationDescription = new[]
            {
                latlongLocationNotes,
                string.IsNullOrWhiteSpace(georeferenceAssignedNotes)
                    ? null
                    : $"Georeference assigned: {georeferenceAssignedNotes}",
                geoLocationNotes
            }.Concatenate(" | ");

            sample.UnitName = site.GetTrimStrings("EraMvRockUnit_tab").Concatenate(", ");
            
            // TODO: Not used
            sample.UnitAge = new[]
            {
                site.GetTrimString("EraEra"),
                site.GetTrimString("EraAge1"),
                site.GetTrimString("EraAge2"),
                site.GetTrimString("EraMvStage")
            }.Concatenate(", ");

            if (site.GetTrimString("EraDepthDeterminationMethod") == "Subsurface - mine/quarry, unknown depth" ||
                site.GetTrimString("EraDepthDeterminationMethod") ==
                "Subsurface - mine/quarry, depth from locality data")
                sample.LocationKind = "Mine";
            else if (site.GetTrimString("EraDepthDeterminationMethod") ==
                     "Subsurface - borehole/well, depth unknown" || site.GetTrimString("EraDepthDeterminationMethod") ==
                     "Subsurface - borehole/well, depth from locality data")
                sample.LocationKind = "Borehole/well";
            else if (string.IsNullOrWhiteSpace(site.GetTrimString("EraDepthDeterminationMethod")))
                sample.LocationKind = "Unknown";

            sample.DepthMin = site.GetTrimString("EraDepthFromMt");
            sample.DepthMax = site.GetTrimString("EraDepthToMt");
        }

        if (DateTime.TryParseExact(map.GetTrimString("LocDateCollectedFrom"), "dd/MM/yyyy",
                new CultureInfo("en-AU"), DateTimeStyles.None, out var dateCollectedMin))
            sample.DateCollectedMin = dateCollectedMin.ToString("yyyy-MM-dd");

        if (DateTime.TryParseExact(map.GetTrimString("LocDateCollectedTo"), "dd/MM/yyyy",
                new CultureInfo("en-AU"), DateTimeStyles.None, out var dateCollectedMax))
            sample.DateCollectedMax = dateCollectedMax.ToString("yyyy-MM-dd");

        if (map.GetTrimString("ColDiscipline") == "Mineralogy")
            sample.SampleKind = "Mineral";
        if (map.GetTrimString("ColDiscipline") == "Petrology")
            sample.SampleKind = "Rock";

        // Mineralogy specific
        if (map.GetTrimString("ColDiscipline") == "Mineralogy")
        {
            sample.MineralId = map.GetTrimString("MinSpecies");

            string typeSpecimen = null;
            if (map.GetTrimString("MinType") == "Yes")
                typeSpecimen = map.GetTrimString("MinTypeType");

            sample.Comment = new[]
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
            
            // Properties:MineralVariety
            if (!string.IsNullOrWhiteSpace(map.GetTrimString("MinVariety")))
                sample.Properties.Add(new SampleProperty()
                {
                    Property = new KeyValuePair<string, string>("MineralVariety", map.GetTrimString("MinVariety")),
                    Order = 1
                });
        }

        // Petrology specific
        if (map.GetTrimString("ColDiscipline") == "Petrology")
        {
            sample.MineralId = map.GetTrimString("RocRockName");

            sample.Comment = new[]
            {
                string.IsNullOrWhiteSpace(map.GetTrimString("RocRockName"))
                    ? null
                    : $"Name: {map.GetTrimString("RocRockName")}",
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
            
            // Properties:ExtendedRockName
            if (!string.IsNullOrWhiteSpace(map.GetTrimString("RocRockName")))
                sample.Properties.Add(new SampleProperty()
                {
                    Property = new KeyValuePair<string, string>("ExtendedRockName", map.GetTrimString("RocRockName")),
                    Order = 1
                });
        }

        sample.LastKnownLocation = "Museums Victoria";
        
        sample.Deleted = string.Equals(map.GetTrimString("AdmPublishWebNoPassword"), "no", StringComparison.OrdinalIgnoreCase);
        
        // Images
        sample.Images = _imageFactory.Make(map.GetMaps("media"), stoppingToken).ToList();
        
        // Properties:SpecimenForm
        var specimenForm = map.GetMaps("preparations")
            .Select(x => new[]
            {
                x.GetString("StrSpecimenForm_tab")
            })
            .SelectMany(x => x)
            .Distinct()
            .Concatenate(",");
        if (!string.IsNullOrWhiteSpace(specimenForm))
            sample.Properties.Add(new SampleProperty()
            {
                Property = new KeyValuePair<string, string>("SpecimenForm", specimenForm),
                Order = 5
            });
        
        // Properties:GeologicalDetails
        var geologicalDetails = new[]
        {
            map.GetTrimString("MinAssociatedMatrix"),
            map.GetTrimString("RocRockDescription"),
            map.GetTrimString("RocMainMineralsPresent")
        }.Concatenate(" | ");
        if (!string.IsNullOrWhiteSpace(geologicalDetails))
            sample.Properties.Add(new SampleProperty()
            {
                Property = new KeyValuePair<string, string>("GeologicalDetails", geologicalDetails),
                Order = 2
            });

        // Sort properties
        sample.Properties = sample.Properties.OrderBy(x => x.Order).ToList();

        return sample;
    }

    public IEnumerable<Sample> Make(IEnumerable<Map> maps, CancellationToken stoppingToken)
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