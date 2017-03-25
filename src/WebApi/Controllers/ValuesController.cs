using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private static readonly Dictionary<string, string> repository = new Dictionary<string, string>();
        private readonly ILogger logger = Log.ForContext<ValuesController>();

        // GET api/values
        [HttpGet]
        public IEnumerable<IdValuePairType> Get()
        {
            logger.Information("GET /api/values");
            foreach(var kvp in repository)
            {
                yield return new IdValuePairType(kvp.Key, kvp.Value);
            }
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IdValuePairType Get(string id)
        {
            logger.Information($"GET /api/values/{id}");
            return repository.ContainsKey(id) ? new IdValuePairType(id, repository[id]) : null;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]ValueType valueEnvelope)
        {
            var v = valueEnvelope?.value;
            logger.Information($"POST /api/values {v}");
            var id = Guid.NewGuid().ToString();
            repository.Add(id, valueEnvelope.value);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(string id, [FromBody]ValueType valueEnvelope)
        {
            var v = valueEnvelope?.value;
            logger.Information($"PUT /api/values {v}");
            if (repository.ContainsKey(id))
                repository[id] = valueEnvelope.value;
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            logger.Information($"DELETE /api/values/{id}");
            if (repository.ContainsKey(id))
                repository.Remove(id);
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
