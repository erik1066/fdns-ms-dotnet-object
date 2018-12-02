using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Core;
using Mongo2Go;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Foundation.ObjectService.Data;

namespace Foundation.ObjectService.WebUI.Tests
{
    public class MongoRepositoryTests  : IClassFixture<MongoFixture>
    {
        MongoFixture _mongoFixture;

        public MongoRepositoryTests(MongoFixture fixture)
        {
            this._mongoFixture = fixture;
        }

        [Fact]
        public void Construct_Null_MongoClient()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                MongoRepository repo = new MongoRepository(null, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            });
        }

        [Fact]
        public void Construct_Null_Logger()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                MongoRepository repo = new MongoRepository(_mongoFixture.MongoClient, null, new Dictionary<string, HashSet<string>>());
            });
        }

        [Fact]
        public void Construct_Null_ImmutableCollections()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                MongoRepository repo = new MongoRepository(_mongoFixture.MongoClient, _mongoFixture.Logger, null);
            });
        }

        [Fact]
        public void Construct_Success()
        {
            MongoRepository repo = new MongoRepository(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            Assert.True(true);
        }

        [Fact]
        public void Insert_Success()
        {
            MongoRepository repo = new MongoRepository(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            string json = "{ \"Name\" : \"John\" }";

            var insertResult = repo.InsertAsync("bookstore", "users", 1, json).Result;
            var getResult = repo.GetAsync("bookstore", "users", 1).Result;

            Assert.StartsWith("{ \"_id\" : { \"$oid\" : ", insertResult);
            Assert.StartsWith("{ \"_id\" : { \"$oid\" : ", getResult);

            Assert.EndsWith(" }, \"Name\" : \"John\", \"id\" : 1 }", insertResult);
            Assert.EndsWith(" }, \"Name\" : \"John\", \"id\" : 1 }", getResult);
        }

        [Fact]
        public void Insert_Does_Not_Overwrite()
        {
            MongoRepository repo = new MongoRepository(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            string json = "{ \"Name\" : \"Jane\" }";

            var insertResult = repo.InsertAsync("bookstore", "users", 2, json).Result;
            var insertResultOverwrite = repo.InsertAsync("bookstore", "users", 2, "{ \"Name\": \"John\" }").Result;
            var getResult = repo.GetAsync("bookstore", "users", 2).Result;

            Assert.StartsWith("{ \"_id\" : { \"$oid\" : ", insertResult);
            Assert.StartsWith("{ \"_id\" : { \"$oid\" : ", insertResultOverwrite);
            Assert.StartsWith("{ \"_id\" : { \"$oid\" : ", getResult);

            Assert.EndsWith(" }, \"Name\" : \"Jane\", \"id\" : 2 }", insertResult);
            Assert.EndsWith(" }, \"Name\" : \"Jane\", \"id\" : 2 }", insertResultOverwrite);
            Assert.EndsWith(" }, \"Name\" : \"Jane\", \"id\" : 2 }", getResult);
        }

        [Fact]
        public void Delete_Success()
        {
            MongoRepository repo = new MongoRepository(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            string json = "{ \"Name\" : \"Maria\" }";

            var insertResult = repo.InsertAsync("bookstore", "users", 3, json).Result;
            var getResult = repo.GetAsync("bookstore", "users", 3).Result;
            var deleteResult = repo.DeleteAsync("bookstore", "users", 3).Result;
            var getResultAfterDelete = repo.GetAsync("bookstore", "users", 3).Result;

            Assert.Null(getResultAfterDelete);
            Assert.True(deleteResult);
        }

        [Fact]
        public void Replace_Success()
        {
            MongoRepository repo = new MongoRepository(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            string json1 = "{ \"Name\" : \"Enrique\" }";
            string json2 = "{ \"Name\" : \"Enrique Hernandez\" }";

            var insertResult = repo.InsertAsync("bookstore", "users", 4, json1).Result;
            var getResult1 = repo.GetAsync("bookstore", "users", 4).Result;

            var replaceResult = repo.ReplaceAsync("bookstore", "users", 4, json2).Result;
            var getResult2 = repo.GetAsync("bookstore", "users", 4).Result;

            Assert.EndsWith(" \"Name\" : \"Enrique\", \"id\" : 4 }", getResult1);
            Assert.EndsWith(" \"Name\" : \"Enrique Hernandez\", \"id\" : 4 }", getResult2);
            Assert.Equal(insertResult, getResult1);
            Assert.Equal(replaceResult, getResult2);
        }

        [Theory]
        [InlineData("{ \"Name\": \"John\" " /* missing closing brace */)]
        [InlineData("\"Name\": \"John\" }" /* missing opening brace */)]
        [InlineData("{ \"Name: \"John\" }" /* property is missing end quote */)]
        public void Insert_Fail_Bad_Json(string badJson)
        {
            MongoRepository repo = new MongoRepository(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());

            Assert.Throws<AggregateException>(() =>
            {
                var result = repo.InsertAsync("bookstore", "users", 1, badJson).Result;
            });
        }
    }

    //similar to base class
    public class MongoFixture : IDisposable
    {
        internal static MongoDbRunner _runner;

        public ILogger<MongoRepository> Logger { get; private set; }
        public IMongoClient MongoClient { get; private set; }
        public string MongoConnectionString => $"mongodb://localhost:27018/admin";

        public MongoFixture()
        {
            Logger = new Mock<ILogger<MongoRepository>>().Object;

            _runner = MongoDbRunner.Start();

            MongoClient = new MongoClient(MongoConnectionString);
        }

        public void Dispose()
        {
            MongoClient.GetDatabase("bookstore").DropCollection("users");
            MongoClient.DropDatabase("bookstore");
            _runner.Dispose();
        }
    }
}
