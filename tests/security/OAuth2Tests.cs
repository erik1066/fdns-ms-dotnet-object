using System;
using System.IO;
using System.Net;
using System.Net.Http;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

using Xunit;

using Foundation.ObjectService.WebUI;

namespace Foundation.ObjectService.SecurityTests
{
    public class OAuth2Tests : IClassFixture<WebApplicationFactory<Foundation.ObjectService.WebUI.Startup>>
    {
        private readonly WebApplicationFactory<Foundation.ObjectService.WebUI.Startup> _factory;
        private readonly string _tokenReadInsert = string.Empty;
        private readonly string _tokenUpdateDelete = string.Empty;

        public OAuth2Tests(WebApplicationFactory<Foundation.ObjectService.WebUI.Startup> factory)
        {
            System.Environment.SetEnvironmentVariable(
                "OAUTH2_ACCESS_TOKEN_URI", 
                "http://localhost:4445/oauth2/introspect", 
                EnvironmentVariableTarget.Process);

            _factory = factory;

            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            var readInsertPath = Path.Combine(basePath, "resources", "token-read-insert");
            _tokenReadInsert = File.ReadAllText(readInsertPath).Trim();

            var updateDeletePath = Path.Combine(basePath, "resources", "token-update-delete");
            _tokenUpdateDelete = File.ReadAllText(updateDeletePath).Trim();
        }

        private void InsertOneRecord()
        {
            var client = _factory.CreateClient();

            // Act
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, "/api/1.0/bookstore/books/1"))
            {
                message.Headers.Add("Authorization", $"{_tokenReadInsert}");
                message.Content = new StringContent("{ \"title\": \"War and Peace\", \"author\": \"Leo Tolstoy\", \"year\": 1869, \"weight\": 28.8 }", System.Text.Encoding.UTF8, "application/json");

                using (var response = client.SendAsync(message).Result)
                {
                }
            }
        }

        /// <summary>
        /// Checks to see whether unprotected endpoints are accessible with no authorization
        /// token in the HTTP header
        /// </summary>
        /// <param name="url">URL to be accessed</param>
        [Theory]
        [InlineData("/api/1.0")]
        [InlineData("/health/live")]
        [InlineData("/health/ready")]
        public void No_Auth_Needed_Success(string url)
        {
            InsertOneRecord();

            // No auth needed for health checks and the version # endpoint

            // Arrange
            var client = _factory.CreateClient();

            // Act
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var response = client.SendAsync(message).Result;

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        /// <summary>
        /// Checks to see whether an endpoint requiring a READ scope can be accessed when the
        /// token has the matching READ scope
        /// </summary>
        /// <param name="url">URL to be accessed</param>
        [Theory]
        [InlineData("/api/1.0")]
        [InlineData("/api/1.0/bookstore/books/1")]
        [InlineData("/health/live")]
        [InlineData("/health/ready")]
        public void Valid_Read_Token(string url)
        {
            InsertOneRecord();

            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"{_tokenReadInsert}");

            // Act
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var response = client.SendAsync(message).Result;

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        /// <summary>
        /// Checks to see whether an endpoint requiring a READ scope returns an UNAUTHORIZED
        /// response when the token is invalid
        /// </summary>
        /// <param name="url">URL to be accessed</param>
        [Theory]
        [InlineData("/api/1.0/bookstore/books/1")]
        [InlineData("/api/1.0/bookstore/books/2")]
        [InlineData("/api/1.0/bookstore/books/3")]
        [InlineData("/api/1.0/coffeeshop/orders/1")]
        [InlineData("/api/1.0/hardwarestore/customers/1")]
        [InlineData("/api/1.0/grocery/inventory")]
        [InlineData("/api/1.0/grocery/inventory/search?qs=weight>50")]
        public void Invalid_Token_Read_Unauthorized(string url)
        {
            InsertOneRecord();

            var client = _factory.CreateClient();
            var token = "not-a-valid-token";
            client.DefaultRequestHeaders.Add("Authorization", $"{token}");

            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var response = client.SendAsync(message).Result;
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        /// <summary>
        /// Checks to see whether an endpoint requiring a READ scope returns an UNAUTHORIZED
        /// response when the token is valid, but lacks the matching READ scope
        /// </summary>
        /// <param name="url">URL to be accessed</param>
        [Theory]
        [InlineData("/api/1.0/bookstore/books/1")]
        public void Valid_Token_Missing_Scope_Read_Unauthorized(string url)
        {
            InsertOneRecord();

            var client = _factory.CreateClient();

            // this is a valid token, but the token does not have the READ scope
            client.DefaultRequestHeaders.Add("Authorization", $"{_tokenUpdateDelete}"); 

            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var response = client.SendAsync(message).Result;
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        // void IDisposable.Dispose()
        // {
        //     _factory.Dispose();
        //     _client.Dispose();
        // }
    }
}
