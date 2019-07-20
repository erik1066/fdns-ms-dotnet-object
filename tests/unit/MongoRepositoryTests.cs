using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using Mongo2Go;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Foundation.ObjectService.Data;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Foundation.ObjectService.WebUI.Tests
{
    public class MongoRepositoryTests : IClassFixture<MongoRepositoryFixture>
    {
        MongoRepositoryFixture _mongoFixture;

        public MongoRepositoryTests(MongoRepositoryFixture fixture)
        {
            this._mongoFixture = fixture;
        }

        [Fact]
        public void Construct_Null_MongoClient()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                MongoService repo = new MongoService(null, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            });
        }

        [Fact]
        public void Construct_Null_Logger()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                MongoService repo = new MongoService(_mongoFixture.MongoClient, null, new Dictionary<string, HashSet<string>>());
            });
        }

        [Fact]
        public void Construct_Null_ImmutableCollections()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                MongoService repo = new MongoService(_mongoFixture.MongoClient, _mongoFixture.Logger, null);
            });
        }

        [Fact]
        public void Construct_Success()
        {
            MongoService repo = new MongoService(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            Assert.True(true);
        }

        [Fact]
        public async Task Insert_Object_Success()
        {
            MongoService repo = new MongoService(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            string json = "{ \"Name\" : \"John\" }";

            var insertResult = await repo.InsertAsync("bookstore", "users", 1, json);
            var getResult = await repo.GetAsync("bookstore", "users", 1);

            Assert.Equal("{ \"_id\" : \"1\", \"Name\" : \"John\" }", insertResult);
            Assert.Equal("{ \"_id\" : \"1\", \"Name\" : \"John\" }", getResult);
        }

        // Test takes too long, disabling
        
        // [Fact]
        // public async Task Insert_Object_Fail_no_Database()
        // {
        //     MongoClientSettings settings = new MongoClientSettings();
        //     settings.ConnectTimeout = new TimeSpan(0, 0, 2);
        //     settings.Server = new MongoServerAddress("localhost", 5555); // wrong port, no DB there

        //     var mongoClient = new MongoClient(settings);             
        //     MongoService repo = new MongoRepository(mongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            
        //     string json = "{ \"Name\" : \"John\" }";

        //     try 
        //     {
        //         var insertResult = await repo.InsertAsync("bookstore", "users", 1, json);
        //     }
        //     catch (Exception ex)
        //     {
        //         Assert.IsType<System.TimeoutException>(ex);
        //     }
        // }

        [Fact]
        public async Task Insert_Object_Does_Not_Overwrite()
        {
            MongoService repo = new MongoService(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            string json = "{ \"Name\" : \"Jane\" }";

            var insertResult = await repo.InsertAsync("bookstore", "users", "2", json);
            
            try 
            {
                var secondInsertResult = await repo.InsertAsync("bookstore", "users", "2", "{ \"Name\": \"John\" }");
            }
            catch (Exception ex)
            {
                Assert.IsType<MongoDB.Driver.MongoWriteException>(ex);
            }

            var getResult = repo.GetAsync("bookstore", "users", "2").Result;

            Assert.Equal("{ \"_id\" : \"2\", \"Name\" : \"Jane\" }", insertResult);
            Assert.Equal("{ \"_id\" : \"2\", \"Name\" : \"Jane\" }", getResult);
        }

        [Fact]
        public async Task Delete_Object_Success()
        {
            MongoService repo = new MongoService(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            string json = "{ \"Name\" : \"Maria\" }";

            var insertResult = await repo.InsertAsync("bookstore", "users", "3", json);
            var getResult = await repo.GetAsync("bookstore", "users", "3");
            var deleteResult = await repo.DeleteAsync("bookstore", "users", "3");
            var getResultAfterDelete = await repo.GetAsync("bookstore", "users", "3");

            Assert.Null(getResultAfterDelete);
            Assert.True(deleteResult);
        }

        [Fact]
        public async Task Replace_Object_Success()
        {
            MongoService repo = new MongoService(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            string json1 = "{ \"Name\" : \"Enrique\" }";
            string json2 = "{ \"Name\" : \"Enrique Hernandez\" }";

            var insertResult = await repo.InsertAsync("bookstore", "users", "4", json1);
            var getResult1 = await repo.GetAsync("bookstore", "users", "4");

            var replaceResult = await repo.ReplaceAsync("bookstore", "users", "4", json2);
            var getResult2 = await repo.GetAsync("bookstore", "users", "4");
            
            Assert.Equal("{ \"_id\" : \"4\", \"Name\" : \"Enrique\" }", getResult1);
            Assert.Equal("{ \"_id\" : \"4\", \"Name\" : \"Enrique Hernandez\" }", getResult2);

            Assert.Equal(insertResult, getResult1);
            Assert.Equal(replaceResult, getResult2);
        }

        [Fact]
        public async Task Replace_Object_Fail_Immutable()
        {
            var immutables = new Dictionary<string, HashSet<string>>();
            immutables.Add("bookstore", new HashSet<string>() { "accounts" });

            MongoService repo = new MongoService(_mongoFixture.MongoClient, _mongoFixture.Logger, immutables);
            string json = "{ \"Name\" : \"Enrique\" }";

            try 
            {
                var insertResult = await repo.InsertAsync("bookstore", "accounts", 1, json);
            }
            catch (Exception ex)
            {
                Assert.IsType<Foundation.ObjectService.Exceptions.ImmutableCollectionException>(ex);
            }
        }

        [Theory]
        [InlineData("{ \"Name\": \"John\" " /* missing closing brace */)]
        [InlineData("\"Name\": \"John\" }" /* missing opening brace */)]
        [InlineData("{ \"Name: \"John\" }" /* property is missing end quote */)]
        public async Task Insert_Object_Fail_Bad_Json(string badJson)
        {
            MongoService repo = new MongoService(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());
            try 
            {
                var result = await repo.InsertAsync("bookstore", "users", 1, badJson);
            }
            catch (Exception ex)
            {
                Assert.IsAssignableFrom<Exception>(ex);
            }
        }

        [Theory]
        [InlineData(0, -15, 5)]
        [InlineData(0, -1, 5)]
        [InlineData(0, 0, 5)]
        [InlineData(0, 1, 1)]
        [InlineData(0, 3, 3)]
        [InlineData(0, 4, 4)]
        [InlineData(0, 5, 5)]
        [InlineData(0, 7, 5)]
        [InlineData(3, 1, 1)]
        [InlineData(2, 2, 2)]
        [InlineData(3, 5, 2)]
        [InlineData(4, 5, 1)]
        [InlineData(5, 5, 0)]
        public async Task Get_All_Objects_Success(int start, int size, int expectedCount)
        {
            MongoService repo = new MongoService(_mongoFixture.MongoClient, _mongoFixture.Logger, new Dictionary<string, HashSet<string>>());

            var collection = "users" + System.Guid.NewGuid().ToString().Replace("-", string.Empty);
            
            var items = new List<string> 
            { 
                "{ \"Name\" : \"Mary\" }", 
                "{ \"Name\" : \"John\" }", 
                "{ \"Name\" : \"Kate\" }", 
                "{ \"Name\" : \"Mark\" }", 
                "{ \"Name\" : \"Sara\" }" 
            };
            
            // insert all five items
            foreach (var item in items)
            {
                var insertResult = await repo.InsertAsync("bookstore", collection, null, item);
            }

            // get all the items just inserted
            var getAllResult = await repo.GetAllAsync("bookstore", collection, start, size);

            var o = JArray.Parse(getAllResult);

            var jArray = JArray.Parse(getAllResult);
            var users = jArray.Select(jt => jt.ToObject<User>()).ToList();

            Assert.Equal(expectedCount, users.Count);
        }
    }

    public class User
    {
        public string Name { get; set; }
    }

    public class MongoRepositoryFixture : IDisposable
    {
        internal static MongoDbRunner _runner;

        public ILogger<MongoService> Logger { get; private set; }
        public IMongoClient MongoClient { get; private set; }
        public IObjectService MongoRepository { get; private set; }

        public MongoRepositoryFixture()
        {
            Logger = new Mock<ILogger<MongoService>>().Object;
            _runner = MongoDbRunner.Start();
            MongoClient = new MongoClient(_runner.ConnectionString);

            var immutables = new Dictionary<string, HashSet<string>>();
            immutables.Add("immutabledatabase", new HashSet<string>() { "immutablecollection" });

            MongoRepository = new MongoService(MongoClient, Logger, immutables);
        }

        public void Dispose()
        {
            _runner.Dispose();
        }
    }
}
