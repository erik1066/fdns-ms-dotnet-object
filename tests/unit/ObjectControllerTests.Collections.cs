using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Xunit;
using Foundation.ObjectService.WebUI.Controllers;
using Foundation.ObjectService.ViewModel;
using Newtonsoft.Json.Linq;

namespace Foundation.ObjectService.WebUI.Tests
{
    public partial class ObjectControllerTests : IClassFixture<ObjectControllerFixture>
    {
        #region Collections

        [Theory]
        [InlineData("1", "{ \"title\": \"The Red Badge of Courage\" }")]
        [InlineData("2", "{ \"title\": \"Don Quixote\" }")]
        public async Task Delete_Collection(string id, string json)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);
            var collectionName = "orders1";

            // Try getting items in the collection. Collection doesn't exist yet, should get a 404
            var getFirstCollectionResult = await controller.GetAllObjectsInCollection(
                new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName },
                new PaginationQueryParameters { Start = 0, Limit = Int32.MaxValue });
            ObjectResult getFirstCollectionMvcResult = ((ObjectResult)getFirstCollectionResult);
            Assert.Equal(404, getFirstCollectionMvcResult.StatusCode);

            // Add an item to collection; Mongo auto-creates the collection            
            var insertResult = await controller.InsertObjectWithId(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id }, json, ResponseFormat.OnlyId);

            // // Try getting items in collection a 2nd time. Now it should return a 200.
            var getSecondCollectionResult = await controller.GetAllObjectsInCollection(
                new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName },
                new PaginationQueryParameters { Start = 0, Limit = Int32.MaxValue });
            OkObjectResult getSecondCollectionMvcResult = ((OkObjectResult)getSecondCollectionResult);
            Assert.Equal(200, getSecondCollectionMvcResult.StatusCode);

            // Delete the collection
            var deleteCollectionResult = await controller.DeleteCollection(new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName });
            var deleteCollectionMvcResult = ((OkResult)deleteCollectionResult);
            Assert.Equal(200, deleteCollectionMvcResult.StatusCode);

            // Try getting items in collection a 3rd time. It was just deleted so we should get a 404.
            var getThirdCollectionResult = await controller.GetAllObjectsInCollection(
                new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName },
                new PaginationQueryParameters { Start = 0, Limit = Int32.MaxValue });
            ObjectResult getThirdCollectionMvcResult = ((ObjectResult)getThirdCollectionResult);
            Assert.Equal(404, getThirdCollectionMvcResult.StatusCode);
        }

        [Fact]
        public async Task Get_Collection()
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);
            var collectionName = "orders2";

            var items = new List<string>() 
            {
                "{ \"title\": \"The Red Badge of Courage\" }",
                "{ \"title\": \"Don Quixote\" }",
                "{ \"title\": \"The Grapes of Wrath\" }",
                "{ \"title\": \"The Catcher in the Rye\" }",
                "{ \"title\": \"Slaughterhouse-Five\" }",
                "{ \"title\": \"Of Mice and Men\" }",
                "{ \"title\": \"Gone with the Wind\" }",
                "{ \"title\": \"Fahrenheit 451\" }",
                "{ \"title\": \"The Old Man and the Sea\" }",
                "{ \"title\": \"The Great Gatsby\" }"
            };

            int insertedItemsCount = 0;
            var insertedTitles = new Dictionary<string, string>();
            foreach (var item in items)
            {
                var insertResult = await controller.InsertObjectWithNoId(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName }, item, ResponseFormat.EntireObject);
                var createdResult = ((CreatedAtActionResult)insertResult);
                if (createdResult.StatusCode == 201)
                {
                    insertedItemsCount++;
                    JObject obj = JObject.Parse(createdResult.Value.ToString());
                    var id = obj["_id"].ToString();
                    var title = obj["title"].ToString();
                    insertedTitles.Add(id, title);
                }
                else
                {
                    Assert.True(false); // should not happen!
                }
            }

            Assert.Equal(items.Count, insertedItemsCount); // test that all inserts worked as expected

            // Try getting items in collection
            var getCollectionResult = await controller.GetAllObjectsInCollection(
                new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName },
                new PaginationQueryParameters { Start = 0, Limit = Int32.MaxValue });
            var getCollectionMvcResult = ((OkObjectResult)getCollectionResult);
            Assert.Equal(200, getCollectionMvcResult.StatusCode);

            var array = JArray.Parse(getCollectionMvcResult.Value.ToString());
            Assert.Equal(items.Count, array.Count);

            int i = 0;
            foreach (var item in array.Children())
            {
                var title = item["title"].ToString();
                var id = item["_id"].ToString();

                Assert.NotNull(title);
                Assert.NotNull(id);
                Assert.True(items[i].Contains(title));
                Assert.Null(item["name"]);
                Assert.Equal(insertedTitles[id], title);
                i++;
            }
        }

        [Fact]
        public async Task Insert_Multiple_Objects()
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);
            var collectionName = "orders3";

            var items = new List<string>() 
            {
                "{ \"title\": \"The Red Badge of Courage\" }",
                "{ \"title\": \"Don Quixote\" }",
                "{ \"title\": \"The Grapes of Wrath\" }",
                "{ \"title\": \"The Catcher in the Rye\" }",
                "{ \"title\": \"Slaughterhouse-Five\" }",
                "{ \"title\": \"Of Mice and Men\" }",
                "{ \"title\": \"Gone with the Wind\" }",
                "{ \"title\": \"Fahrenheit 451\" }",
                "{ \"title\": \"The Old Man and the Sea\" }",
                "{ \"title\": \"The Great Gatsby\" }"
            };

            var payload = "[" + string.Join(',', items) + "]";

            var insertManyResult = await controller.MultiInsert(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName }, payload);
            var insertManyMvcResult = ((OkObjectResult)insertManyResult);

            Assert.Equal(200, insertManyMvcResult.StatusCode);

            // Try getting items in collection
            var getCollectionResult = await controller.GetAllObjectsInCollection(
                new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName },
                new PaginationQueryParameters { Start = 0, Limit = Int32.MaxValue });
            var getCollectionMvcResult = ((OkObjectResult)getCollectionResult);
            Assert.Equal(200, getCollectionMvcResult.StatusCode);

            var array = JArray.Parse(getCollectionMvcResult.Value.ToString());
            Assert.Equal(items.Count, array.Count);

            int i = 0;
            foreach (var item in array.Children())
            {
                Assert.NotNull(item["title"]);
                Assert.NotNull(item["_id"]);
                Assert.True(items[i].Contains(item["title"].ToString()));
                Assert.Null(item["name"]);
                i++;
            }
        }

        [Fact]
        public async Task Insert_Multiple_Objects_Fail_Malformed_Json()
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);
            var collectionName = "orders4";

            var items = new List<string>() 
            {
                "{ \"title\": \"The Red Badge of Courage\" }",
                "{ \"title\": \"Don Quixote\" }",
                "{ \"title\": \"The Grapes of Wrath\" }",
                "{ \"title\": \"The Catcher in the Rye\" }",
                "{ \"title\": \"Slaughterhouse-Five\" }",
                "{ \"title\": \"Of Mice and Men\" }",
                "{ \"title\": \"Gone with the Wind\" }",
                "{ \"title\": \"Fahrenheit 451\" }",
                "{ \"title\": \"The Old Man and the Sea\" }",
                "{ \"title\": \"The Great Gatsby\" }"
            };

            var payload = "[" + string.Join(',', items); // missing end bracket!

            try 
            {
                var insertManyResult = await controller.MultiInsert(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName }, payload);
                throw new InvalidOperationException();
            }
            catch (Exception ex)
            {
                Assert.IsType<Newtonsoft.Json.JsonReaderException>(ex);
            }

            // Try getting items in collection
            var getCollectionResult = await controller.GetAllObjectsInCollection(
                new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName },
                new PaginationQueryParameters { Start = 0, Limit = Int32.MaxValue });
            var getCollectionMvcResult = ((ObjectResult)getCollectionResult);
            Assert.Equal(404, getCollectionMvcResult.StatusCode);
        }

        #endregion // Collections
    }
}