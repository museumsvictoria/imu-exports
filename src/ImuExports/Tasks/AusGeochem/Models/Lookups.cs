using ImuExports.Tasks.AusGeochem.Contracts.Dtos;

namespace ImuExports.Tasks.AusGeochem.Models;

public class Lookups
{
    public IList<LocationKindDto> LocationKindDtos { get; set; }
    
    public IList<MaterialDto> MaterialDtos { get; set; }
    
    public IList<SampleKindDto> SampleKindDtos { get; set; }
    
    public IList<MaterialNamePair> MaterialNamePairs { get; set; }
}