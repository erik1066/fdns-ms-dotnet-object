using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

using Xunit;

using Foundation.ObjectService.WebUI;

namespace integration
{
    public class ObjectServiceTests: IClassFixture<WebApplicationFactory<Foundation.ObjectService.WebUI.Startup>>
    {
        private readonly WebApplicationFactory<Foundation.ObjectService.WebUI.Startup> _factory;
        private const string DATABASE_NAME = "bookstore";
        private const string BOOKS_COLLECTION_NAME = "books";

        public ObjectServiceTests(WebApplicationFactory<Foundation.ObjectService.WebUI.Startup> factory)
        {
            System.Environment.SetEnvironmentVariable(
                "ASPNETCORE_ENVIRONMENT",
                "Production", 
                EnvironmentVariableTarget.Process);

            _factory = factory;
        }

        private void DeleteCollection(string collectionName)
        {
            var client = _factory.CreateClient();
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, $"/api/1.0/bookstore/{collectionName}"))
            {
                using (var response = client.SendAsync(message).Result)
                {
                }
            }
        }

        /// <summary>
        /// Runs through a series of basic CRUD operations
        /// </summary>
        /// <param name="id">id of the object to be inserted, updated, retrieved, or deleted</param>
        /// <param name="insertedJson">The Json to insert</param>
        /// <param name="expectedJson">The Json that should be returned (assuming success) on a retrieval operation</param>
        [Theory]
        [InlineData("1", "{ \"title\": \"The Red Badge of Courage\" }", "{ \"_id\" : \"1\", \"title\" : \"The Red Badge of Courage\" }")]
        [InlineData("2", "{ \"title\": \"Don Quixote\" }", "{ \"_id\" : \"2\", \"title\" : \"Don Quixote\" }")]
        [InlineData("3", "{ \"title\": \"A Connecticut Yankee in King Arthur's Court\" }", "{ \"_id\" : \"3\", \"title\" : \"A Connecticut Yankee in King Arthur's Court\" }")]
        public void Crud_Runthrough(string id, string insertedJson, string expectedJson)
        {
            // Arrange
            var client = _factory.CreateClient();
            DeleteCollection("books1");

            // Act

            // Make sure we return a 404 on a GET when the object doesn't exist (we haven't inserted it yet)
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, $"/api/1.0/bookstore/books1/{id}"))
            {
                using (var response = client.SendAsync(message).Result)
                {
                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                }
            }

            // Make sure we return a 404 on a DELETE when the object doesn't exist (we haven't inserted it yet)
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, $"/api/1.0/bookstore/books1/{id}"))
            {
                using (var response = client.SendAsync(message).Result)
                {
                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                }
            }

            // Make sure we return a 404 when trying to update an object via PUT when the object doesn't exist
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, $"/api/1.0/bookstore/books1/{id}"))
            {
                message.Content = new StringContent(insertedJson, System.Text.Encoding.UTF8, "application/json");

                using (var response = client.SendAsync(message).Result)
                {
                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                }
            }

            // Make sure we get a 201 when inserting an object, where that object didn't previously exist
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, $"/api/1.0/bookstore/books1/{id}"))
            {
                message.Content = new StringContent(insertedJson, System.Text.Encoding.UTF8, "application/json");

                using (var response = client.SendAsync(message).Result)
                {
                    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

                    var content = response.Content.ReadAsStringAsync().Result;
                    Assert.Equal(expectedJson, content);
                }
            }

            // Make sure we get a 200 and the actual Json from a GET request on the thing we just inserted
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, $"/api/1.0/bookstore/books1/{id}"))
            {
                using (var response = client.SendAsync(message).Result)
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    var content = response.Content.ReadAsStringAsync().Result;
                    Assert.Equal(expectedJson, content);
                }
            }

            // Make sure we get a 200 when deleting the thing we just inserted
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, $"/api/1.0/bookstore/books1/{id}"))
            {
                using (var response = client.SendAsync(message).Result)
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }

            // Make sure that the thing we just deleted is actually gone by trying to retrieve it again - we should get a 404 Not Found
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, $"/api/1.0/bookstore/books1/{id}"))
            {
                using (var response = client.SendAsync(message).Result)
                {
                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                }
            }

            // And just for kicks, let's try and delete the thing again and make sure we get a 404 Not Found
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, $"/api/1.0/bookstore/books1/{id}"))
            {
                using (var response = client.SendAsync(message).Result)
                {
                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                }
            }
        }
    }
}
