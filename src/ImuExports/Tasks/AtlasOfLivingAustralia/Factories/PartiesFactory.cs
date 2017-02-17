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
                switch (map.GetTrimString("NamPartyType"))
                {
                    case "Collaboration":
                        party.Name = new[]
                        {
                            map.GetTrimString("ColCollaborationName")
                        }.Concatenate(", ");
                        break;
                    case "Cutter Number":
                        party.Name = new[]
                        {
                            map.GetTrimString("NamBranch"),
                            map.GetTrimString("NamDepartment"),
                            map.GetTrimString("NamOrganisation"),
                            map.GetTrimString("AddPhysStreet"),
                            map.GetTrimString("AddPhysCity"),
                            map.GetTrimString("AddPhysState"),
                            map.GetTrimString("AddPhysCountry")
                        }.Concatenate(", ");
                        break;
                    case "Organisation":
                        party.Name = new[]
                        {
                            map.GetTrimString("NamBranch"),
                            map.GetTrimString("NamDepartment"),
                            map.GetTrimString("NamOrganisation")
                        }.Concatenate(", ");
                        break;
                    case "Person":
                        party.Name = new[]
                        {
                            map.GetTrimString("NamFullName"),
                            map.GetTrimString("NamOrganisation")
                        }.Concatenate(" - ");
                        break;
                    case "Position":
                        break;
                    case "Transport":
                        var name = string.Empty;
                        var organisationOtherName = map.GetTrimStrings("NamOrganisationOtherNames_tab").FirstOrDefault();
                        var source = map.GetTrimString("NamSource");

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
                            map.GetTrimString("NamFullName"),
                            map.GetTrimString("NamOrganisation")
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
