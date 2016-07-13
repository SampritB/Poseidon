using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Poseidon.Models
{
    public class Descriptor
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string LabelId { get; set; }

        public byte[] Values { get; set; }

        public string Algorithm { get; set; }
    }
}