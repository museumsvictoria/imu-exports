﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ImuExports.Config;
using ImuExports.Infrastructure;
using ImuExports.Tasks.WikimediaCommons.Models;
using RestSharp;
using Serilog;

namespace ImuExports.Tasks.WikimediaCommons
{
    public class WikimediaCommonsTask : ImuTaskBase, ITask
    {
        public void Run()
        {
            using (Log.Logger.BeginTimedOperation(string.Format("{0} starting", GetType().Name), string.Format("{0}.Run", GetType().Name)))
            {
                var items = new List<Item>();

                // Fetch Data
                var client = new RestClient("http://collections.museumvictoria.com.au/api/");
                var request = new RestRequest("search", Method.GET);
                request.AddQueryParameter("collection", "Tangyes Lantern Slide Collection");
                request.AddQueryParameter("perpage", "100");
                request.AddQueryParameter("page", "1");

                var page = 1;
                while (true)
                {
                    var results = client.Execute<List<Item>>(request);

                    items.AddRange(results.Data);

                    if (page < FetchTotalPages(results.Headers))
                    {
                        page++;
                        request.Parameters.First(x => x.Name == "page").Value = page;
                    }                        
                    else
                        break;
                }

                // Build and export Data
                var utf8WithoutBom = new System.Text.UTF8Encoding(false);
                var metadataElement = new XElement("metadata");

                var titleHash = new Dictionary<string, int>();

                foreach (var item in items)
                {
                    var itemElement = new XElement("record");

                    itemElement.Add(new XElement("photographer", "{{unknown|author}}"));
                    itemElement.Add(new XElement("description", item.IsdDescriptionOfContent));
                    itemElement.Add(new XElement("depictedpeople", item.IsdPeopleDepicted));
                    itemElement.Add(new XElement("medium", item.IsdFormat));
                    itemElement.Add(new XElement("dimensions", "{{Size|unit=mm|width=83|height=83|depth=3}}"));
                    itemElement.Add(new XElement("institution", "{{Institution:Museums Victoria}}"));
                    itemElement.Add(new XElement("department", "[http://collections.museumvictoria.com.au/search?collection=Tangyes+Lantern+Slide+Collection Tangyes Lantern Slide Collection]"));
                    itemElement.Add(new XElement("objecthistory", item.ObjectSummary));
                    itemElement.Add(new XElement("creditline", item.AcquisitionInformation));
                    itemElement.Add(new XElement("inscriptions", item.Inscription));
                    itemElement.Add(new XElement("accessionnumber", item.RegistrationNumber));

                    var sourceElement = new XElement("source", string.Format("[http://collections.museumvictoria.com.au/{0} Info]", item.Id));
                    var image = item.Media.FirstOrDefault();
                    if (image != null)
                    {
                        sourceElement.Value += string.Format(" [{0} Image]", image.Original.Uri);
                        itemElement.Add(new XElement("toolseturl", image.Original.Uri));
                    }
                    itemElement.Add(sourceElement);

                    itemElement.Add(new XElement("permission", "{{PD-scan|PD-Australia}}"));

                    if (titleHash.ContainsKey(item.ObjectName))
                    {
                        titleHash[item.ObjectName] += 1;
                        itemElement.Add(new XElement("toolsettitle", string.Format("{0} ({1})", item.ObjectName, titleHash[item.ObjectName])));
                    }
                    else
                    {
                        titleHash.Add(item.ObjectName, 1);
                        itemElement.Add(new XElement("toolsettitle", item.ObjectName));
                    }
                    
                    metadataElement.Add(itemElement);
                }

                // Save xml
                using (var fileWriter = new StreamWriter(string.Format("{0}export.xml", GlobalOptions.Options.Wc.Destination), false, utf8WithoutBom))
                {
                    metadataElement.Save(fileWriter);
                }
            }
        }

        private int FetchTotalPages(IEnumerable<Parameter> headers)
        {
            var header = headers.FirstOrDefault(x => x.Name == "Total-Pages");

            if (header != null)
                return int.Parse((string)header.Value);

            return default(int);
        }
    }
}