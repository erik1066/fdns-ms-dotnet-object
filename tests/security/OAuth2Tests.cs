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
        private readonly string _tokenBookstoreAllAll = string.Empty;

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

            var bookstoreAllAllPath = Path.Combine(basePath, "resources", "token-bookstore-all-all");
            _tokenBookstoreAllAll = File.ReadAllText(bookstoreAllAllPath).Trim();
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

        #region explicit tests

        /// <summary>
        /// Checks to see whether unprotected endpoints are accessible with no authorization
        /// token in the HTTP header
        /// </summary>
        /// <param name="url">URL to be accessed</param>
        [Theory]
        [InlineData("/api/1.0")]
        public void No_Auth_Needed_Success(string url)
        {
            try 
            {
                InsertOneRecord();
            }
            catch (Exception ex) when (ex.Message.Contains("E11000"))
            {
            }

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

        #endregion
        
        #region Star tests

        /// <summary>
        /// Checks to see whether an endpoint can be accessed on READ with the appropriate *.* scopes
        /// </summary>
        /// <param name="url">URL to be accessed</param>
        [Theory]
        [InlineData("/api/1.0/bookstore/books/1")]
        public void Valid_Bookstore_All_All_Token_Read(string url)
        {
            InsertOneRecord();

            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"{_tokenBookstoreAllAll}");

            // Act
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var response = client.SendAsync(message).Result;

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        /// <summary>
        /// Checks to see whether an endpoint can be accessed on INSERT with the appropriate *.* scopes
        /// </summary>
        /// <param name="url">URL to be accessed</param>
        [Theory]
        [InlineData("/api/1.0/bookstore/orders6535")]
        public void Valid_Bookstore_All_All_Token_Insert(string url)
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"{_tokenBookstoreAllAll}");

            // Act
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, url))
            {
                message.Content = new StringContent("{ \"identifier\": 32 }", System.Text.Encoding.UTF8, "application/json");

                var response = client.SendAsync(message).Result;

                // Assert
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            }
        }

        /// <summary>
        /// Checks to see whether an endpoint can be accessed on UPDATE with the appropriate *.* scopes
        /// </summary>
        /// <param name="url">URL to be accessed</param>
        [Theory]
        [InlineData("/api/1.0/bookstore/books/1")]
        public void Valid_Bookstore_All_All_Token_Update(string url)
        {
            InsertOneRecord();

            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"{_tokenBookstoreAllAll}");

            // Act
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, url))
            {
                message.Content = new StringContent("{ \"identifier\": 32 }", System.Text.Encoding.UTF8, "application/json");

                var response = client.SendAsync(message).Result;

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

                /// <summary>
        /// Checks to see whether an endpoint can be accessed on UPDATE with the appropriate *.* scopes
        /// </summary>
        /// <param name="url">URL to be accessed</param>
        [Theory]
        [InlineData("/api/1.0/bookstore/books/1")]
        public void Valid_Bookstore_All_All_Token_Delete(string url)
        {
            InsertOneRecord();

            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"{_tokenBookstoreAllAll}");

            // Act
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Delete, url))
            {
                var response = client.SendAsync(message).Result;

                // Assert
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        /// <summary>
        /// Checks to see whether an endpoint cannot be accessed on READ with *.* scopes due to a mismatched database name
        /// </summary>
        /// <param name="url">URL to be accessed</param>
        [Theory]
        [InlineData("/api/1.0/coffeeshop/orders/1")]
        [InlineData("/api/1.0/hardwarestore/orders/abcd")]
        [InlineData("/api/1.0/postoffice/orders/431")]
        public void Valid_All_All_Read_Token_with_invalid_database(string url)
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"{_tokenBookstoreAllAll}");

            // Act
            using (HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, url))
            {
                var response = client.SendAsync(message).Result;

                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }
        
        #endregion

        // void IDisposable.Dispose()
        // {
        //     _factory.Dispose();
        //     _client.Dispose();
        // }
    }
}
