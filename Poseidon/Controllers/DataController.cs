using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Driver;
using System.Configuration;
using Poseidon.Models;
using MongoDB.Driver.Builders;
using MongoDB.Bson;
using System.Text;
using System.IO;

namespace Poseidon.Controllers
{
    [Authorize]
    public class DataController : Controller
    {
        private MongoClient _client;
        private MongoServer _server;
        private MongoDatabase _database;

        public DataController()
        {
            _client = new MongoClient(ConfigurationManager.ConnectionStrings["Mongo"].ConnectionString);
            _server = _client.GetServer();
            _database = _server.GetDatabase(ConfigurationManager.AppSettings["DbName"]);
        }
        //
        // GET: /Data/

        public ActionResult Index()
        {
            var images = _database.GetCollection<ImageInfo>("images");

            return View(images.FindAll());
        }

        public ActionResult Labels(string id)
        {
            var images = _database.GetCollection<ImageInfo>("images");
            var labels = _database.GetCollection<Label>("labels");

            ViewBag.ImageInfo = images.FindOne(Query.EQ("_id", new ObjectId(id)));

            return View(labels.Find(Query.EQ("ImageInfoId", new ObjectId(id))));
        }

        private string csvEscape(string input)
        {
            if (input.Contains(','))
            {
                input = input.Replace("\"", "\"\"");
                return string.Format("\"{0}\"", input);
            }
            else return input;
        }

        public ActionResult CSV(string id = null)
        {
            var images = _database.GetCollection<ImageInfo>("images");
            long num_images = images.Count();
            var labels = _database.GetCollection<Label>("labels");
            long num_labels = labels.Count();
            var tags = _database.GetCollection<ImageTag>("imageTags");
            long num_tabs = tags.Count();

            // Get all the labels
            var all_labels = labels.FindAll();
            if (!string.IsNullOrEmpty(id))
            {
                all_labels = labels.Find(Query.EQ("ImageInfoId", new ObjectId(id)));
            }

            // Create a dictionary mapping from the image id to the list of labels for that image
            Dictionary<string, List<Label>> image_labels_dict = new Dictionary<string, List<Label>>();
            foreach (var label in all_labels)
            {
                if (!image_labels_dict.ContainsKey(label.ImageInfoId))
                {
                    image_labels_dict[label.ImageInfoId] = new List<Label>();
                }
                image_labels_dict[label.ImageInfoId].Add(label);
            }

            // Create a dictionary containing all the tag names in the entire database (i.e. not just in this particular image). (Should really use the MongoDB Distinct() function, but cannot figure it out...)
            Dictionary<string, int> tag_count_dict = new Dictionary<string, int>();
            foreach (var label in all_labels)
            {
                string key = label.Tag;
                // If the key doesn't already exist, then add it to the dictionary
                if (!tag_count_dict.ContainsKey(key))
                {
                    tag_count_dict[key] = 0;
                }
            }
            int num_unique_labels = tag_count_dict.Count();

            using (MemoryStream stream = new MemoryStream())
            {
                StreamWriter csv = new StreamWriter(stream);
                foreach (string image in image_labels_dict.Keys)
                {
                    var imageInfo = images.FindOne(Query.EQ("_id", new ObjectId(image)));
                    var image_labels = labels.Find(Query.EQ("ImageInfoId", new ObjectId(image))); // All tags in this image
                    var image_tags = tags.Find(Query.EQ("ImageInfoId", new ObjectId(image))); // All tags in this image

                    // Create a dictionary for the number of counts for each tag in this particular image
                    Dictionary<string, int> image_tag_count_dict = new Dictionary<string, int>();
                    foreach (var tag in tag_count_dict.Keys)
                    {
                        image_tag_count_dict[tag] = 0;
                    }
                    foreach (var label in image_labels)
                    {
                        image_tag_count_dict[label.Tag] += 1;
                    }

                    csv.WriteLine("Image Properties");
                    csv.WriteLine();
                    csv.WriteLine("Image,{0}", csvEscape(imageInfo.Title));
                    csv.WriteLine("Dimensions (width x height),{0}x{1}", imageInfo.Width, imageInfo.Height);
                    csv.WriteLine("Creator,{0}", csvEscape(imageInfo.UserID));
                    csv.WriteLine("Creation Date,{0}", imageInfo.CreateDate);
                    csv.WriteLine();

                    csv.WriteLine("Image Data");
                    csv.WriteLine();
                    var all_tags = tags.FindAll();
                    foreach (var tag in image_tags)
                    {
                        csv.WriteLine("{0},{1}", tag.Key, "value");
                    }
                    csv.WriteLine();

                    csv.WriteLine("Species Data");
                    csv.WriteLine();
                    csv.WriteLine("Tag,Count");
                    foreach (KeyValuePair<string, int> pair in image_tag_count_dict)
                    {
                        csv.WriteLine("{0},{1}", csvEscape(pair.Key), pair.Value);
                    }
                    csv.WriteLine();
                }
                csv.Close();

                return File(stream.GetBuffer(), "text/csv", id == null ? "allImages.csv" : id + ".csv");
            }
        }
    }
}