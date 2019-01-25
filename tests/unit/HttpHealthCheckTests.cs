using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using Xunit;
using Moq;
using Foundation.ObjectService.Data;
using Foundation.ObjectService.WebUI.Controllers;
using Foundation.ObjectService.ViewModel;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;

namespace Foundation.ObjectService.WebUI.Tests
{
    public class HttpHealthCheckTests : IClassFixture<HttpHealthCheckFixture>
    {
        HttpHealthCheckFixture _fixture;

        public HttpHealthCheckTests(HttpHealthCheckFixture fixture)
        {
            this._fixture = fixture;
        }

        [Fact]
        public void Test_Service_Ready()
        {
            // arrange
            var factory = _fixture.GetSuccessClientFactory();
            var check = new HttpHealthCheck("unittests-1", "http://localhost/health/ready", factory, 100, 500);
            var context = new HealthCheckContext();

            // act
            var checkResult = check.CheckHealthAsync(context).Result;

            // assert
            Assert.Equal(HealthStatus.Healthy, checkResult.Status);
        }

        [Fact]
        public void Test_Service_Degraded()
        {
            // arrange
            var factory = _fixture.GetServiceDegradedClientFactory();
            var check = new HttpHealthCheck("unittests-2", "http://localhost/health/ready", factory, 100, 500);
            var context = new HealthCheckContext();

            // act
            var checkResult = check.CheckHealthAsync(context).Result;

            // assert
            Assert.Equal(HealthStatus.Degraded, checkResult.Status);
        }

        [Fact]
        public void Test_Service_Unhealthy_503()
        {
            // arrange
            var factory = _fixture.GetServiceUnavailableClientFactory();
            var check = new HttpHealthCheck("unittests-3", "http://localhost/health/ready", factory, 100, 200);
            var context = new HealthCheckContext();

            // act
            var checkResult = check.CheckHealthAsync(context).Result;

            // assert
            Assert.Equal(HealthStatus.Unhealthy, checkResult.Status);
        }

        [Fact]
        public void Test_Service_Unhealthy_Exception()
        {
            // arrange
            var factory = _fixture.GetExceptionClientFactory();
            var check = new HttpHealthCheck("unittests-4", "http://localhost/health/ready", factory, 100, 200);
            var context = new HealthCheckContext();

            // act
            var checkResult = check.CheckHealthAsync(context).Result;

            // assert
            Assert.Equal(HealthStatus.Unhealthy, checkResult.Status);
        }
    }

    public class HttpHealthCheckFixture : IDisposable
    {

        public HttpHealthCheckFixture()
        {            
        }

        public IHttpClientFactory GetSuccessClientFactory() 
        {
            var mockHttp = new MockHttpMessageHandler();

            // Setup a respond for the user api (including a wildcard in the URL)
            mockHttp.When("http://localhost/health/ready")
                .Respond(HttpStatusCode.OK, "application/json", "{}");

            var client = mockHttp.ToHttpClient();
            client.BaseAddress = new Uri("http://localhost/");
            
            var mock = new Mock<IHttpClientFactory>();
            mock.CallBase = true;
            mock.Setup(x => x.CreateClient($"unittests-1")).Returns(client);

            var clientFactory = mock.Object;

            return clientFactory;
        }

        public IHttpClientFactory GetServiceDegradedClientFactory() 
        {
            var mockHttp = new MockHttpMessageHandler();

            // Setup a respond for the user api (including a wildcard in the URL)
            mockHttp.When("http://localhost/health/ready")
            .Respond(response => {
                System.Threading.Thread.Sleep(200);
                HttpResponseMessage message = new HttpResponseMessage();
                message.StatusCode = HttpStatusCode.OK;
                return message;
            });
                //.Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

            var client = mockHttp.ToHttpClient();
            client.BaseAddress = new Uri("http://localhost/");

            var mock = new Mock<IHttpClientFactory>();
            mock.CallBase = true;
            mock.Setup(x => x.CreateClient($"unittests-2")).Returns(client);

            var clientFactory = mock.Object;

            return clientFactory;
        }

        public IHttpClientFactory GetServiceUnavailableClientFactory() 
        {
            var mockHttp = new MockHttpMessageHandler();

            // Setup a respond for the user api (including a wildcard in the URL)
            mockHttp.When("http://localhost/health/ready")
                .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{}");

            var client = mockHttp.ToHttpClient();
            client.BaseAddress = new Uri("http://localhost/");
            
            var mock = new Mock<IHttpClientFactory>();
            mock.CallBase = true;
            mock.Setup(x => x.CreateClient($"unittests-3")).Returns(client);

            var clientFactory = mock.Object;

            return clientFactory;
        }

        public IHttpClientFactory GetExceptionClientFactory() 
        {
            var mockHttp = new MockHttpMessageHandler();

            // Setup a respond for the user api (including a wildcard in the URL)
            mockHttp.When("http://localhost/health/ready")
                .Throw(new InvalidOperationException());

            var client = mockHttp.ToHttpClient();
            client.BaseAddress = new Uri("http://localhost/");
            
            var mock = new Mock<IHttpClientFactory>();
            mock.CallBase = true;
            mock.Setup(x => x.CreateClient($"unittests-4")).Returns(client);

            var clientFactory = mock.Object;

            return clientFactory;
        }

        public void Dispose()
        {
            
        }
    }
}