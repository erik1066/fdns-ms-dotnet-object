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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Core;
using Mongo2Go;
using Moq;
using Foundation.ObjectService.Data;
using Foundation.ObjectService.WebUI.Controllers;
using Foundation.ObjectService.ViewModel;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;

namespace Foundation.ObjectService.WebUI.Tests
{
    public class ObjectHealthCheckTests
    {
        [Theory]
        [InlineData("description", 1000, 2000)]
        [InlineData("unit-test", 1, 2)]
        public void Construct_Success(string description, int degredationThreshold, int cancellationThreshold)
        {
            // arrange
            Mock<IObjectService> mockObjectService = new Mock<IObjectService>();

            // act
            var check = new ObjectDatabaseHealthCheck(description, mockObjectService.Object, degredationThreshold, cancellationThreshold);

            // assert
            Assert.True(true);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Construct_Fail_Invalid_Description(string description)
        {
            // arrange
            Mock<IObjectService> mockObjectService = new Mock<IObjectService>();

            // act
            Action act = () => new ObjectDatabaseHealthCheck(description, mockObjectService.Object, 100, 200);

            // assert
            Assert.Throws<ArgumentNullException>(act);
        }

        [Fact]
        public void Construct_Fail_Null_Service()
        {
            // act
            Action act = () => new ObjectDatabaseHealthCheck("unit-tests", null, 100, 200);

            // assert
            Assert.Throws<ArgumentNullException>(act);
        }

        [Fact]
        public void Construct_Fail_Degradation_Threshold_Less_than_zero()
        {
            // arrange
            Mock<IObjectService> mockObjectService = new Mock<IObjectService>();

            // act
            Action act = () => new ObjectDatabaseHealthCheck("unit-tests", mockObjectService.Object, -1, 200);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void Construct_Fail_Cancellation_Threshold_Less_than_zero()
        {
            // arrange
            Mock<IObjectService> mockObjectService = new Mock<IObjectService>();

            // act
            Action act = () => new ObjectDatabaseHealthCheck("unit-tests", mockObjectService.Object, 100, -5);

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Fact]
        public void Construct_Fail_Cancellation_Threshold_Less_than_Degradation_Threshold()
        {
            // arrange
            Mock<IObjectService> mockObjectService = new Mock<IObjectService>();

            // act
            Action act = () => new ObjectDatabaseHealthCheck("unit-tests", mockObjectService.Object, 100, 50);

            // assert
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void Test_Service_Ready()
        {
            // arrange
            Mock<IObjectService> mockObjectService = new Mock<IObjectService>();
            mockObjectService.Setup(o => o.DeleteAsync(ObjectDatabaseHealthCheck.DummyDatabaseName, ObjectDatabaseHealthCheck.DummyCollectionName, 1)).ReturnsAsync(true);
            mockObjectService.Setup(o => o.InsertAsync(ObjectDatabaseHealthCheck.DummyDatabaseName, ObjectDatabaseHealthCheck.DummyCollectionName, 1, "{ 'name' : 'the nameless ones' }")).ReturnsAsync(string.Empty);
            mockObjectService.Setup(o => o.GetAsync(ObjectDatabaseHealthCheck.DummyDatabaseName, ObjectDatabaseHealthCheck.DummyCollectionName, 1)).ReturnsAsync(string.Empty);

            var check = new ObjectDatabaseHealthCheck("unittests-1", mockObjectService.Object, 120_000, 150_000);
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
            Mock<IObjectService> mockObjectService = new Mock<IObjectService>();
            mockObjectService.Setup(o => o.DeleteAsync(ObjectDatabaseHealthCheck.DummyDatabaseName, ObjectDatabaseHealthCheck.DummyCollectionName, 1))
            .Returns( async () => 
            { 
                await Task.Run(() => System.Threading.Thread.Sleep(100));
                return true;
            });

            mockObjectService.Setup(o => o.InsertAsync(ObjectDatabaseHealthCheck.DummyDatabaseName, ObjectDatabaseHealthCheck.DummyCollectionName, 1, "{ 'name' : 'the nameless ones' }")).ReturnsAsync(string.Empty);
            mockObjectService.Setup(o => o.GetAsync(ObjectDatabaseHealthCheck.DummyDatabaseName, ObjectDatabaseHealthCheck.DummyCollectionName, 1)).ReturnsAsync(string.Empty);

            var check = new ObjectDatabaseHealthCheck("unittests-1", mockObjectService.Object, 1, 150_000);
            var context = new HealthCheckContext();

            // act
            var checkResult = check.CheckHealthAsync(context).Result;

            // assert
            Assert.Equal(HealthStatus.Degraded, checkResult.Status);
        }

        [Fact]
        public void Test_Service_Unhealthy()
        {
            // arrange
            Mock<IObjectService> mockObjectService = new Mock<IObjectService>();
            mockObjectService.Setup(o => o.DeleteAsync(ObjectDatabaseHealthCheck.DummyDatabaseName, ObjectDatabaseHealthCheck.DummyCollectionName, 1))
            .Returns( async () => 
            { 
                await Task.Run(() => 
                {
                    System.Threading.Thread.Sleep(50);
                    for (long i = 0; i < 500_000; i++)
                    {
                        var y = i * i;
                        string x = y.ToString(); // just waste some time
                    }
                });
                return true;
            });

            mockObjectService.Setup(o => o.InsertAsync(ObjectDatabaseHealthCheck.DummyDatabaseName, ObjectDatabaseHealthCheck.DummyCollectionName, 1, "{ 'name' : 'the nameless ones' }")).ReturnsAsync(string.Empty);
            mockObjectService.Setup(o => o.GetAsync(ObjectDatabaseHealthCheck.DummyDatabaseName, ObjectDatabaseHealthCheck.DummyCollectionName, 1)).ReturnsAsync(string.Empty);

            var check = new ObjectDatabaseHealthCheck("unittests-1", mockObjectService.Object, 1, 2);
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
            Mock<IObjectService> mockObjectService = new Mock<IObjectService>();
            mockObjectService.Setup(o => o.DeleteAsync(ObjectDatabaseHealthCheck.DummyDatabaseName, ObjectDatabaseHealthCheck.DummyCollectionName, 1)).ThrowsAsync(new InvalidOperationException("test-exception"));
            mockObjectService.Setup(o => o.InsertAsync(ObjectDatabaseHealthCheck.DummyDatabaseName, ObjectDatabaseHealthCheck.DummyCollectionName, 1, "{ 'name' : 'the nameless ones' }")).ReturnsAsync(string.Empty);
            mockObjectService.Setup(o => o.GetAsync(ObjectDatabaseHealthCheck.DummyDatabaseName, ObjectDatabaseHealthCheck.DummyCollectionName, 1)).ReturnsAsync(string.Empty);

            var check = new ObjectDatabaseHealthCheck("unittests-1", mockObjectService.Object, 1, 2);
            var context = new HealthCheckContext();

            // act
            var checkResult = check.CheckHealthAsync(context).Result;

            // assert
            Assert.Equal(HealthStatus.Unhealthy, checkResult.Status);
        }
    }
}