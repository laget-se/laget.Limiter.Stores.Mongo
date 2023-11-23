using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace laget.Limiter.Stores
{
    public class Call
    {
        [BsonElement("id"), BsonId, BsonIgnoreIfDefault, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("createdAt"), BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonElement("value")]
        public long Value { get; set; }
    }

    public class MongoStore : IStore
    {
        private readonly IMongoCollection<Call> _store;
        private readonly TimeSpan _ttl;

        public MongoStore(MongoUrl url)
            : this(url, TimeSpan.FromHours(1), "calls")
        {
        }

        public MongoStore(MongoUrl url, TimeSpan ttl)
            : this(url, ttl, "calls")
        {
        }

        public MongoStore(MongoUrl url, TimeSpan ttl, string collection = "calls")
        {
            var client = new MongoClient(url);

            var database = client.GetDatabase(url.DatabaseName, new MongoDatabaseSettings
            {
                ReadConcern = ReadConcern.Default,
                ReadPreference = ReadPreference.SecondaryPreferred,
                WriteConcern = WriteConcern.Acknowledged
            });

            _store = database.GetCollection<Call>(collection);
            _ttl = ttl;

            EnsureIndexes();
        }

        public void Add(DateTime item)
        {
            _store.InsertOne(new Call { Value = item.ToBinary() });
        }

        public IList<DateTime> Get() => _store.Find(new BsonDocument()).ToList()
                .Select(x => DateTime.FromBinary(x.Value))
                .ToList();

        public void Trim(int amount)
        {
            var items = Get()
                .OrderByDescending(x => x)
                .Take(amount)
                .Select(x => x.ToBinary())
                .ToList();

            var builder = Builders<Call>.Filter;
            var filter = builder.Nin(x => x.Value, items);

            _store.DeleteMany(filter);
        }

        private void EnsureIndexes()
        {
            var builder = Builders<Call>.IndexKeys;
            var indexes = new List<CreateIndexModel<Call>>
            {
                new CreateIndexModel<Call>(builder.Ascending(_ => _.CreatedAt), new CreateIndexOptions { Background = true, ExpireAfter = _ttl }),
                new CreateIndexModel<Call>(builder.Ascending(_ => _.Value), new CreateIndexOptions { Background = true, Unique = true })
            };
            _store.Indexes.CreateMany(indexes);
        }
    }
}
