using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Driver;
using Poseidon.Models;
using System.Xml.Linq;
using System.Configuration;

namespace Poseidon.Controllers
{
    public class FolderController : Controller
    {
        private MongoClient _client;
        private MongoServer _server;
        private MongoDatabase _database;

        public FolderController()
        {
            _client = new MongoClient(ConfigurationManager.ConnectionStrings["Mongo"].ConnectionString);
            _server = _client.GetServer();
            _database = _server.GetDatabase(ConfigurationManager.AppSettings["DbName"]);
        }

        public ContentResult Index(string dir = "")
        {
            var imagesDB = _database.GetCollection<ImageInfo>("images");
            var foldersDB = _database.GetCollection<Folder>("folders");
            var folderItemsDB = _database.GetCollection<FolderItem>("folderItems");

            Dictionary<Folder, List<ImageInfo>> folderImages = new Dictionary<Folder, List<ImageInfo>>();
            List<FolderItem> folderItems = folderItemsDB.FindAll().ToList();
            Dictionary<string, ImageInfo> images = imagesDB.FindAll().ToDictionary(o => o.Id, o => o);
            Dictionary<string, Folder> folders = foldersDB.FindAll().ToDictionary(o => o.Id, o => o);

            foreach (var item in folderItems)
            {
                Folder folder = folders[item.FolderId];
                if (!folderImages.ContainsKey(folder))
                    folderImages[folder] = new List<ImageInfo>();
                if (images.ContainsKey(item.ImageInfoId))
                {
                    ImageInfo info = images[item.ImageInfoId];
                    folderImages[folder].Add(info);
                    images.Remove(item.ImageInfoId);
                }
            }

            XElement result;
            if (string.IsNullOrEmpty(dir))
            {
                result = new XElement("ul",
                    new XAttribute("class", "jqueryFileTree"),
                    folders.Values.OrderBy(o=>o.Name).Select(o =>
                    {
                        return new XElement("li",
                            new XAttribute("class", "directory collapsed"),
                            new XElement("a",
                                new XAttribute("href", "#"),
                                new XAttribute("rel", o.Id),
                                o.Name));
                    }),
                    images.Values.OrderBy(o=>o.Title).Select(o =>
                    {
                        return new XElement("li",
                            new XAttribute("class", "file ext_jpg"),
                            new XElement("a",
                                new XAttribute("href", "#"),
                                new XAttribute("rel", o.Id),
                                o.Title));
                    }));
            }
            else
            {
                if (!folders.ContainsKey(dir))
                    result = new XElement("ul", new XAttribute("class", "jqueryFileTree"));
                Folder folder = folders[dir];
                if (!folderImages.ContainsKey(folder))
                    result = new XElement("ul", new XAttribute("class", "jqueryFileTree"));
                else result = new XElement("ul",
                    new XAttribute("class", "jqueryFileTree"),
                    folderImages[folder].OrderBy(o=>o.Title).Select(o =>
                    {
                        return new XElement("li",
                            new XAttribute("class", "file ext_jpg"),
                            new XElement("a",
                                new XAttribute("href", "#"),
                                new XAttribute("rel", o.Id),
                                o.Title));
                    }));

            }
            return Content(result.ToString(), "text/html");
        }

        public void Add(string name)
        {
            name = name.Trim();
            if (string.IsNullOrEmpty(name))
                return;

            var foldersDB = _database.GetCollection<Folder>("folders");

            Folder folder = new Folder { Name = name };
            foldersDB.Insert(folder);
        }
    }
}
