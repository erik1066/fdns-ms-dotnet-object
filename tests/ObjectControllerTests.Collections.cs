using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

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
using Foundation.ObjectService.WebUI.Controllers;
using Foundation.ObjectService.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Foundation.ObjectService.WebUI.Tests
{
    public partial class ObjectControllerTests : IClassFixture<ObjectControllerFixture>
    {
        #region Collection deletion

        [Theory]
        [InlineData("1", "{ \"title\": \"The Red Badge of Courage\" }")]
        [InlineData("2", "{ \"title\": \"Don Quixote\" }")]
        public async Task Delete_Collection(string id, string json)
        {
            // Arrange
            var controller = new ObjectController(_fixture.MongoRepository);
            var collectionName = "orders1";

            // Try getting items in the collection. Collection doesn't exist yet, should get a 404
            var getFirstCollectionResult = await controller.GetAllObjectsInCollection(new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName });
            ObjectResult getFirstCollectionMvcResult = ((ObjectResult)getFirstCollectionResult);
            Assert.Equal(404, getFirstCollectionMvcResult.StatusCode);

            // Add an item to collection; Mongo auto-creates the collection            
            var insertResult = await controller.InsertObjectWithId(new ItemRouteParameters() { DatabaseName = DATABASE_NAME, CollectionName = collectionName, Id = id }, json, ResponseFormat.OnlyId);

            // // Try getting items in collection a 2nd time. Now it should return a 200.
            var getSecondCollectionResult = await controller.GetAllObjectsInCollection(new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName });
            OkObjectResult getSecondCollectionMvcResult = ((OkObjectResult)getSecondCollectionResult);
            Assert.Equal(200, getSecondCollectionMvcResult.StatusCode);

            // Delete the collection
            var deleteCollectionResult = await controller.DeleteCollection(new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName });
            var deleteCollectionMvcResult = ((OkResult)deleteCollectionResult);
            Assert.Equal(200, deleteCollectionMvcResult.StatusCode);

            // Try getting items in collection a 3rd time. It was just deleted so we should get a 404.
            var getThirdCollectionResult = await controller.GetAllObjectsInCollection(new DatabaseRouteParameters { DatabaseName = DATABASE_NAME, CollectionName = collectionName });
            ObjectResult getThirdCollectionMvcResult = ((ObjectResult)getThirdCollectionResult);
            Assert.Equal(404, getThirdCollectionMvcResult.StatusCode);
        }

        #endregion // Collection deletion
    }
}