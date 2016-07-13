using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Poseidon.Models
{
    public class ImageTag
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string ImageInfoId { get; set; }

        public string Key { get; set; }
        public string Value { get; set; }
    }
}