using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Foundation.ObjectService.Exceptions;

namespace Foundation.ObjectService.Data
{
    /// <summary>
    /// Class representing a MongoDB repository for arbitrary, untyped Json objects
    /// </summary>
    public class MongoRepository : IObjectRepository
    {
        private readonly IMongoClient _client = null;
        private readonly JsonWriterSettings _jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
        private readonly ILogger<MongoRepository> _logger;
        private const string ID_PROPERTY_NAME = "_id";
        private readonly Dictionary<string, HashSet<string>> _immutableCollections;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">MongoDB client</param>
        /// <param name="logger">Logger</param>
        /// <param name="immutableCollections">List of immutable collections</param>
        public MongoRepository(IMongoClient client, ILogger<MongoRepository> logger, Dictionary<string, HashSet<string>> immutableCollections)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            if (immutableCollections == null)
            {
                throw new ArgumentNullException(nameof(immutableCollections));
            }
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
            _logger = logger;
            _immutableCollections = immutableCollections;
        }

        /// <summary>
        /// Gets a single object
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="id">The id of the object to get</param>
        /// <returns>The object matching the specified id</returns>
        public async Task<string> GetAsync(string databaseName, string collectionName, object id)
        {
            try
            {
                var database = GetDatabase(databaseName);
                var collection = GetCollection(database, collectionName);
                var filter = Builders<BsonDocument>.Filter.Eq(ID_PROPERTY_NAME, id);
                var document = await collection.Find(filter).FirstOrDefaultAsync();
                return StringifyDocument(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get failed on {databaseName}/{collectionName}/{id}");
                throw ex;
            }
        }

        /// <summary>
        /// Gets all objects in a collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <returns>All objects in the collection</returns>
        public async Task<string> GetAllAsync(string databaseName, string collectionName)
        {
            try
            {
                var database = GetDatabase(databaseName);
                var collection = GetCollection(database, collectionName);
                var documents = await collection.Find(_ => true).ToListAsync();
                return StringifyDocuments(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Get all failed on {databaseName}/{collectionName}");
                throw ex;
            }
        }

        /// <summary>
        /// Inserts a single object into the given database and collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="id">The id of the object</param>
        /// <param name="json">The Json that represents the object</param>
        /// <returns>The object that was inserted</returns>
        public async Task<string> InsertAsync(string databaseName, string collectionName, object id, string json)
        {
            try
            {
                if (_immutableCollections.ContainsKey(databaseName) && _immutableCollections[databaseName].Contains(collectionName))
                {
                    throw new ImmutableCollectionException($"Collection {collectionName} in database {databaseName} is immutable. No new items may be inserted.");
                }

                var database = GetDatabase(databaseName);
                var collection = GetCollection(database, collectionName);
                var document = BsonDocument.Parse(ForceAddIdToJsonObject(id, json));
                await collection.InsertOneAsync(document);
                return await GetAsync(databaseName, collectionName, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Insert failed on {databaseName}/{collectionName}/{id}");
                throw ex;
            }
        }

        /// <summary>
        /// Updates a single object in the given database and collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="id">The id of the object</param>
        /// <param name="json">The Json that represents the object</param>
        /// <returns>The object that was updated</returns>
        public async Task<string> ReplaceAsync(string databaseName, string collectionName, object id, string json)
        {
            try
            {
                if (_immutableCollections.ContainsKey(databaseName) && _immutableCollections[databaseName].Contains(collectionName))
                {
                    throw new ImmutableCollectionException($"Collection {collectionName} in database {databaseName} is immutable. No items may be replaced.");
                }

                var database = GetDatabase(databaseName);
                var collection = GetCollection(database, collectionName);
                var document = BsonDocument.Parse(ForceAddIdToJsonObject(id, json));

                var filter = Builders<BsonDocument>.Filter.Eq(ID_PROPERTY_NAME, id);
                var replaceOneResult = await collection.ReplaceOneAsync(filter, document);

                if (replaceOneResult.IsAcknowledged && replaceOneResult.ModifiedCount == 1)
                {
                    return await GetAsync(databaseName, collectionName, id);
                }
                else if (replaceOneResult.IsAcknowledged && replaceOneResult.ModifiedCount == 0)
                {
                    return string.Empty;
                }
                else
                {
                    throw new InvalidOperationException("The replace operation was not acknowledged by MongoDB");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Update failed on {databaseName}/{collectionName}/{id}");
                throw ex;
            }
        }

        /// <summary>
        /// Deletes a single object in the given database and collection
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="id">The id of the object</param>
        /// <returns>Whether the deletion was successful</returns>
        public async Task<bool> DeleteAsync(string databaseName, string collectionName, object id)
        {
            try
            {
                if (_immutableCollections.ContainsKey(databaseName) && _immutableCollections[databaseName].Contains(collectionName))
                {
                    throw new ImmutableCollectionException($"Collection {collectionName} in database {databaseName} is immutable. No items may be deleted.");
                }

                var database = GetDatabase(databaseName);
                var collection = GetCollection(database, collectionName);
                var filter = Builders<BsonDocument>.Filter.Eq(ID_PROPERTY_NAME, id);
                var deleteOneResult = await collection.DeleteOneAsync(filter);

                if (deleteOneResult.IsAcknowledged && deleteOneResult.DeletedCount == 1)
                {
                    return true;
                }
                else if (deleteOneResult.IsAcknowledged && deleteOneResult.DeletedCount == 0)
                {
                    return false;
                }
                else
                {
                    throw new InvalidOperationException("The delete operation was not acknowledged by MongoDB");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Delete failed on {databaseName}/{collectionName}/{id}");
                throw ex;
            }
        }

        /// <summary>
        /// Finds a set of objects that match the specified find criteria
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="findExpression">The MongoDB-style find syntax</param>
        /// <param name="start">The index within the find results at which to start filtering</param>
        /// <param name="size">The number of items within the find results to limit the result set to</param>
        /// <param name="sortFieldName">The Json property name of the object on which to sort</param>
        /// <param name="sortDirection">The sort direction</param>
        /// <returns>A collection of objects that match the find criteria</returns>
        public async Task<string> FindAsync(string databaseName, string collectionName, string findExpression, int start, int size, string sortFieldName, ListSortDirection sortDirection)
        {
            try
            {
                var regexFind = GetRegularExpressionQuery(databaseName, collectionName, findExpression, start, size, sortFieldName, sortDirection);
                var document = await regexFind.ToListAsync();
                var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict }; // key part
                var stringifiedDocument = document.ToJson(jsonWriterSettings);
                return stringifiedDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Find failed on {databaseName}/{collectionName} with arguments start={start}, size={size}, sortFieldName={sortFieldName}");
                throw ex;
            }
        }

        /// <summary>
        /// Counts the number of objects that match the specified count criteria
        /// </summary>
        /// <param name="databaseName">The database name</param>
        /// <param name="collectionName">The collection name</param>
        /// <param name="findExpression">The MongoDB-style find syntax</param>
        /// <returns>Number of matching objects</returns>
        public async Task<long> CountAsync(string databaseName, string collectionName, string findExpression)
        {
            try
            {
                var regexFind = GetRegularExpressionQuery(databaseName, collectionName, findExpression, 0, Int32.MaxValue, string.Empty, ListSortDirection.Ascending);
                var documentCount = await regexFind.CountDocumentsAsync();
                return documentCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Count failed on {databaseName}/{collectionName}");
                throw ex;
            }
        }

        private IFindFluent<BsonDocument, BsonDocument> GetRegularExpressionQuery(string databaseName, string collectionName, string findExpression, int start, int size, string sortFieldName, ListSortDirection sortDirection)
        {
            var database = GetDatabase(databaseName);
            var collection = GetCollection(database, collectionName);

            var regexQuery = BsonDocument.Parse(findExpression);
            var regexFind = collection
                .Find(regexQuery)
                .Skip(start)
                .Limit(size);

            if (!string.IsNullOrEmpty(sortFieldName))
            {
                if (sortDirection == ListSortDirection.Ascending)
                {
                    regexFind.SortBy(bson => bson[sortFieldName]);
                }
                else
                {
                    regexFind.SortByDescending(bson => bson[sortFieldName]);
                }
            }

            return regexFind;
        }

        /// <summary>
        /// Forces an ID property into a JSON object
        /// </summary>
        /// <param name="id">The ID value to force into the object's 'id' property</param>
        /// <param name="json">The Json that should contain the ID key and value</param>
        /// <returns>The Json object with an 'id' property and the specified id value</returns>
        private string ForceAddIdToJsonObject(object id, string json)
        {
            var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (values.ContainsKey(ID_PROPERTY_NAME))
            {
                values[ID_PROPERTY_NAME] = id;
            }
            else
            {
                values.Add(ID_PROPERTY_NAME, id);
            }
            string checkedJson = Newtonsoft.Json.JsonConvert.SerializeObject(values, Formatting.Indented);
            return checkedJson;
        }

        private IMongoDatabase GetDatabase(string databaseName)
        {
            #region Input Validation
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException(nameof(databaseName));
            }
            #endregion // Input Validation
            return _client.GetDatabase(databaseName);
        }

        private IMongoCollection<BsonDocument> GetCollection(IMongoDatabase database, string collectionName)
        {
            #region Input Validation
            if (string.IsNullOrEmpty(collectionName))
            {
                throw new ArgumentNullException(nameof(collectionName));
            }
            #endregion // Input Validation
            return database.GetCollection<BsonDocument>(collectionName);
        }

        private string StringifyDocument(BsonDocument document)
        {
            if (document == null)
            {
                return null;
            }
            return document.ToJson(_jsonWriterSettings);
        }

        private string StringifyDocuments(List<BsonDocument> documents) => documents.ToJson(_jsonWriterSettings);
    }
}