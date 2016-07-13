using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Poseidon.Models
{
    public class ImageInfo
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string FileId { get; set; }
        public string Title { get; set; }
        public DateTime CreateDate { get; set; }
        public string UserID { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}