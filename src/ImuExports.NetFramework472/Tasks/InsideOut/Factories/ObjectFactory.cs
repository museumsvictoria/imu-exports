using System;
using System.Collections.Generic;
using System.Linq;
using IMu;
using ImuExports.NetFramework472.Extensions;
using ImuExports.NetFramework472.Infrastructure;
using ImuExports.NetFramework472.Tasks.InsideOut.Models;
using Object = ImuExports.NetFramework472.Tasks.InsideOut.Models.Object;

namespace ImuExports.NetFramework472.Tasks.InsideOut.Factories
{
    public class ObjectFactory : IFactory<Models.Object>
    {
        private readonly IFactory<Thumbnail> thumbnailFactory;
        private readonly IFactory<Image> imageFactory;
        private readonly IFactory<Audio> audioFactory;

        public ObjectFactory(IFactory<Thumbnail> thumbnailFactory,
            IFactory<Image> imageFactory,
            IFactory<Audio> audioFactory)
        {
            this.thumbnailFactory = thumbnailFactory;
            this.imageFactory = imageFactory;
            this.audioFactory = audioFactory;
        }

        public Object Make(Map map)
        {
            var @object = new Object
            {
                Irn = map.GetLong("irn"),
                Scene = map.GetTrimString("DetNarrativeIdentifier"),
                Priority = map.GetInt("DetVersion"),
                Title = map.GetTrimString("NarTitle"),
                Description = map.GetTrimString("NarNarrative")
            };

            var linkedObject = map.GetMaps("emv")?.FirstOrDefault();
            if (linkedObject != null)
            {
                if (string.Equals(linkedObject.GetTrimString("ColCategory"), "Natural Sciences", StringComparison.OrdinalIgnoreCase))
                {
                    @object.ExternalUrl = $"https://collections.museumvictoria.com.au/specimens/{linkedObject.GetLong("irn")}";
                }
                else
                {
                    @object.ExternalUrl = $"https://collections.museumvictoria.com.au/items/{linkedObject.GetLong("irn")}";
                }
            }

            @object.Thumbnail = thumbnailFactory.Make(map.GetMaps("media")).FirstOrDefault();
            @object.Image = imageFactory.Make(map.GetMaps("media")).FirstOrDefault();
            @object.Audio = audioFactory.Make(map.GetMaps("media")).ToList();

            return @object;
        }

        public IEnumerable<Object> Make(IEnumerable<Map> maps)
        {
            return maps.Select(Make);
        }
    }
}