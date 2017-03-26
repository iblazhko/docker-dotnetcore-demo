using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Serilog;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly ILogger logger = Log.ForContext<ValuesController>();
        private readonly MongoClient mongoClient;
        private readonly IMongoDatabase db;

        public ValuesController()
        {
            logger.Verbose("ValuesController created");

            var mongoClientSettings = new MongoClientSettings();
            mongoClient = new MongoClient(mongoClientSettings);
            db = mongoClient.GetDatabase(MongoDbConfiguration.DatabaseName);

            var c = db.GetCollection<IdValuePairType>("values");
            if (c == null)
            {
                db.CreateCollection("values");
            }
        }

        // GET api/values
        [HttpGet]
        public IEnumerable<IdValuePairType> Get()
        {
            logger.Information("GET /api/values");
            var c = db.GetCollection<IdValuePairType>("values");
            return c.Find(Builders<IdValuePairType>.Filter.Empty).ToList();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IdValuePairType Get(string id)
        {
            logger.Information($"GET /api/values/{id}");
            var c = db.GetCollection<IdValuePairType>("values");
            return c.Find(Builders<IdValuePairType>.Filter.Eq(x => x.id, id)).FirstOrDefault();
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]ValueType valueEnvelope)
        {
            var v = valueEnvelope?.value;
            logger.Information($"POST /api/values {v}");
            var id = Guid.NewGuid().ToString();
            var c = db.GetCollection<IdValuePairType>("values");
            var d = new IdValuePairType(id, v);
            c.InsertOne(d);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(string id, [FromBody]ValueType valueEnvelope)
        {
            var v = valueEnvelope?.value;
            logger.Information($"PUT /api/values {v}");
            var c = db.GetCollection<IdValuePairType>("values");
            c.UpdateOne(Builders<IdValuePairType>.Filter.Eq(x => x.id, id), Builders<IdValuePairType>.Update.Set(x => x.value, v));
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            logger.Information($"DELETE /api/values/{id}");
            var c = db.GetCollection<IdValuePairType>("values");
            c.DeleteOne(Builders<IdValuePairType>.Filter.Eq(x => x.id, id));
        }

        public class ValueType
        {
            public string value { get; private set;}

            public ValueType(string value)
            {
                this.value = value;
            }
        }

        public class IdValuePairType
        {
            public string id { get; private set;}
            public string value { get; private set;}

            public IdValuePairType(string id, string value)
            {
                this.id = id;
                this.value = value;
            }
        }
    }
}
