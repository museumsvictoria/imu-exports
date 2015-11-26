using System.Collections.Generic;
using System.Linq;
using IMu;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Factories
{
    public class PartiesFactory : IFactory<Party>
    {
        public Party Make(Map map)
        {
            var party = new Party();

            if (map != null)
            {
                switch (map.GetEncodedString("NamPartyType"))
                {
                    case "Collaboration":
                        party.Name = new[]
                        {
                            map.GetEncodedString("ColCollaborationName")
                        }.Concatenate(", ");
                        break;
                    case "Cutter Number":
                        party.Name = new[]
                        {
                            map.GetEncodedString("NamBranch"),
                            map.GetEncodedString("NamDepartment"),
                            map.GetEncodedString("NamOrganisation"),
                            map.GetEncodedString("AddPhysStreet"),
                            map.GetEncodedString("AddPhysCity"),
                            map.GetEncodedString("AddPhysState"),
                            map.GetEncodedString("AddPhysCountry")
                        }.Concatenate(", ");
                        break;
                    case "Organisation":
                        party.Name = new[]
                        {
                            map.GetEncodedString("NamBranch"),
                            map.GetEncodedString("NamDepartment"),
                            map.GetEncodedString("NamOrganisation")
                        }.Concatenate(", ");
                        break;
                    case "Person":
                        party.Name = new[]
                        {
                            map.GetEncodedString("NamFullName"),
                            map.GetEncodedString("NamOrganisation")
                        }.Concatenate(" - ");
                        break;
                    case "Position":
                        break;
                    case "Transport":
                        var name = string.Empty;
                        var organisationOtherName = map.GetEncodedStrings("NamOrganisationOtherNames_tab").FirstOrDefault();
                        var source = map.GetEncodedString("NamSource");

                        if (string.IsNullOrWhiteSpace(organisationOtherName) && !string.IsNullOrWhiteSpace(source))
                        {
                            name = source;
                        }
                        else if (!string.IsNullOrWhiteSpace(organisationOtherName) && string.IsNullOrWhiteSpace(source))
                        {
                            name = organisationOtherName;
                        }
                        else if (!string.IsNullOrWhiteSpace(organisationOtherName) && !string.IsNullOrWhiteSpace(source))
                        {
                            name = string.Format("{0} ({1})", organisationOtherName, source);
                        }

                        party.Name = new[]
                        {
                            name,
                            map.GetEncodedString("NamFullName"),
                            map.GetEncodedString("NamOrganisation")
                        }.Concatenate(", ");
                        break;
                }
            }

            return party;
        }

        public IEnumerable<Party> Make(IEnumerable<Map> maps)
        {
            return maps.Select(Make);
        }
    }
}
