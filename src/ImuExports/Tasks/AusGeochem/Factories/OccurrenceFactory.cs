using System.Collections.Generic;
using System.Linq;
using IMu;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.Factories
{
    public class SpecimenFactory : IFactory<Specimen>
    {
        public Specimen Make(Map map)
        {
            var specimen = new Specimen();
            
            return specimen;
        }

        public IEnumerable<Specimen> Make(IEnumerable<Map> maps)
        {
            return maps.Select(Make);
        }
    }
}