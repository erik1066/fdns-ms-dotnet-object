#pragma warning disable 1591 // disables the warnings about missing Xml code comments

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.Swagger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MongoDB.Driver;
using Polly;
using Polly.Extensions.Http;
using Foundation.ObjectService.Data;
using Foundation.Sdk.Security;

namespace Foundation.ObjectService.WebUI
{
    public class Startup
    {
        //private readonly ILogger _logger;

        public Startup(IConfiguration configuration/*, ILoggerFactory loggerFactory*/)
        {
            Configuration = configuration;
            //_logger = loggerFactory.CreateLogger<Startup>();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string systemName = Common.GetConfigurationVariable(Configuration, "SYSTEM_NAME", "SystemName", string.Empty);

            string authorizationDomain = Common.GetConfigurationVariable(Configuration, "OAUTH2_AUTH_DOMAIN", "Auth:Domain", string.Empty);
            string introspectionUri = Common.GetConfigurationVariable(Configuration, "OAUTH2_ACCESS_TOKEN_URI", "Auth:IntrospectUrl", string.Empty);
            string apiGatewayReadinessCheckUri = Common.GetConfigurationVariable(Configuration, "OAUTH2_READINESS_CHECK_URI", "Auth:ReadinessCheckUrl", string.Empty);

            string objectHealthCheckDatabaseName = Common.GetConfigurationVariable(Configuration, "OBJECT_HEALTH_CHECK_DATABASE_NAME", "Health:DatabaseName", "_healthcheckdatabase_");
            string objectHealthCheckCollectionName = Common.GetConfigurationVariable(Configuration, "OBJECT_HEALTH_CHECK_COLLECTION_NAME", "Health:CollectionName", "_healthcheckcollection_");
            
            var tokenType = TokenType.None;

            if (!string.IsNullOrEmpty(introspectionUri))
            {
                tokenType = TokenType.Bearer;
            }
            else if (!string.IsNullOrEmpty(authorizationDomain))
            {
                tokenType = TokenType.Jwt;
            }
            
            AddSwaggerServices(services, tokenType);

            services.AddControllers(options =>
            {
                options.InputFormatters.Insert(0, new TextPlainInputFormatter());
                options.InputFormatters.Insert(0, new JsonRawInputFormatter());
                options.OutputFormatters.Insert(0, new JsonRawOutputFormatter());
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.WriteIndented = true;
            });

            services.AddCors(options =>
            {
            options.AddPolicy("CorsPolicy",
                builder => builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
                    //.AllowCredentials());
            });

            string mongoConnectionString = Common.GetConfigurationVariable(Configuration, "OBJECT_MONGO_CONNECTION_STRING", "MongoDB:ConnectionString", "mongodb://localhost:27017");
            string mongoUseSsl = Common.GetConfigurationVariable(Configuration, "OBJECT_MONGO_USE_SSL", "MongoDB:UseSsl", "false");

            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(mongoConnectionString));

            if (mongoUseSsl.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            }
            else
            {
                //_logger.LogWarning("MongoDB connection is not using SSL");
            }

            services.AddSingleton<IMongoClient>(provider => new MongoClient(settings));
            services.AddSingleton<IObjectService>(provider => new MongoService(provider.GetService<IMongoClient>(), provider.GetService<ILogger<MongoService>>(), GetImmutableCollections()));

            //services.AddSingleton<ObjectDatabaseHealthCheck>(provider => new ObjectDatabaseHealthCheck(
            //    description: "Database", 
            //    service: provider.GetService<IObjectService>(), 
            //    databaseName: objectHealthCheckDatabaseName, 
            //    collectionName: objectHealthCheckCollectionName,
            //    shouldCreateFakeObject: true,
            //    id: "1"));

            //IHealthChecksBuilder healthCheckStatusBuilder = services.AddHealthChecks()
            //    .AddCheck<ObjectDatabaseHealthCheck>("database", null, new List<string> { "ready", "mongo", "db" });

            /* These policy names match the names in the [Authorize] attribute(s) in the Controller classes.
             * The HasScopeHandler class is used (see below) to pass/fail the authorization check if authorization
             * has been enabled via the microservice's configuration.
             */
            services.AddAuthorization(options =>
            {
                options.AddPolicy($"fdns.object.*.*.read", policy => policy.Requirements.Add(new HasScopeRequirement($"fdns.object.*.*.read", authorizationDomain)));
                options.AddPolicy($"fdns.object.*.*.insert", policy => policy.Requirements.Add(new HasScopeRequirement($"fdns.object.*.*.insert", authorizationDomain)));
                options.AddPolicy($"fdns.object.*.*.update", policy => policy.Requirements.Add(new HasScopeRequirement($"fdns.object.*.*.update", authorizationDomain)));
                options.AddPolicy($"fdns.object.*.*.delete", policy => policy.Requirements.Add(new HasScopeRequirement($"fdns.object.*.*.delete", authorizationDomain)));
            });

            if (tokenType == TokenType.Jwt)
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = authorizationDomain;
                    options.Audience = Common.GetConfigurationVariable(Configuration, "OAUTH2_CLIENT_ID", "Auth:ApiIdentifier", string.Empty);
                });

                services.AddSingleton<IAuthorizationHandler>(provider => new JwtHasScopeHandler(systemName));

                //_logger.LogInformation("Configured authorization: JWT validation");
            }
            else if (tokenType == TokenType.Bearer)
            {
                // create HTTP client for token introspection
                services.AddHttpClient($"oauth2-provider", client =>
                {
                })
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromMilliseconds(20),
                    TimeSpan.FromMilliseconds(40),
                    TimeSpan.FromMilliseconds(120),
                }))
                .AddPolicyHandler(GetCircuitBreakerPolicy()); // sets a circuit breaker so that after several failed requests, we just stop sending those requests

                // add health checks for the OAuth2 API gateway
                //if (!string.IsNullOrEmpty(apiGatewayReadinessCheckUri)) 
                //{
                //    services.AddSingleton<HttpHealthCheck>(provider => new HttpHealthCheck("oauth2-provider", apiGatewayReadinessCheckUri, provider.GetService<IHttpClientFactory>(), 100, 500));
                //    healthCheckStatusBuilder.AddCheck<HttpHealthCheck>("oauth2-provider", null, new List<string> { "ready", "oauth2", "api-gateway" });
                //}
                //else
                //{
                //    _logger.LogWarning("OAuth2 token introspection has been configured, but there is no health check for the OAuth2 token service");
                //}

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "FDNS Token Scheme";
                    options.DefaultChallengeScheme = "FDNS Token Scheme";
                })
                .AddTokenAuth(o => { });

                // If we're using bearer tokens, let's use introspection to validate the tokens and their scopes
                services.AddSingleton<IAuthorizationHandler>(provider => new TokenHasScopeHandler(systemName, introspectionUri, provider.GetService<IHttpClientFactory>()));

                //_logger.LogInformation("Configured authorization: OAuth2 bearer tokens with token introspection");
            }
            else
            {
                // If the developer has not configured OAuth2, then disable authentication and authorization
                services.AddSingleton<IAuthorizationHandler, AlwaysAllowHandler>();

                // Log a warning about this
                //_logger.LogWarning("No authorization has been configured, all APIs are open");
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseMiddleware(typeof(ErrorHandlingMiddleware));
            
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                await next();
            });

            app.UseCors("CorsPolicy");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint(Common.SWAGGER_FILE, "Object Microservice API V1");
            });

            //app.UseHealthChecks(Common.HEALTH_LIVENESS_ENDPOINT, new HealthCheckOptions
            //{
            //    // Exclude all checks, just return a 200.
            //    Predicate = (check) => false,
            //    AllowCachingResponses = false
            //});

            //app.UseHealthChecks(Common.HEALTH_READINESS_ENDPOINT, new HealthCheckOptions
            //{
            //    Predicate = (check) => check.Tags.Contains("ready"),
            //    ResponseWriter = WriteResponse,
            //    AllowCachingResponses = false,

            //    ResultStatusCodes =
            //    {
            //        [HealthStatus.Healthy] = StatusCodes.Status200OK,
            //        [HealthStatus.Degraded] = StatusCodes.Status200OK,
            //        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            //    }
            //});

            app.UseAuthentication();

            //app.UseMvc();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        //private static Task WriteResponse(HttpContext httpContext, HealthReport result)
        //{
        //    httpContext.Response.ContentType = "application/json";

        //    var json = new JObject(
        //        new JProperty("status", result.Status.ToString()),
        //        new JProperty("results", new JObject(result.Entries.Select(pair =>
        //            new JProperty(pair.Key, new JObject(
        //                new JProperty("status", pair.Value.Status.ToString()),
        //                new JProperty("description", pair.Value.Description),
        //                new JProperty("data", new JObject(pair.Value.Data.Select(p => new JProperty(p.Key, p.Value))))))))));
        //    return httpContext.Response.WriteAsync(json.ToString(Formatting.Indented));
        //}

        private Dictionary<string, HashSet<string>> GetImmutableCollections()
        {
            var immutableCollection = new Dictionary<string, HashSet<string>>();

            var immutableCollectionsStr = Common.GetConfigurationVariable(Configuration, "OBJECT_IMMUTABLE", "MongoDB:Immutable", string.Empty);
            if (!string.IsNullOrEmpty(immutableCollectionsStr))
            {
                string [] immutableCollections = immutableCollectionsStr.Split(';');
                foreach (var entry in immutableCollections)
                {
                    var parts = entry.Split('/');
                    if (parts.Length == 2)
                    {
                        var databaseName = parts[0];
                        var collectionName = parts[1];

                        if (!immutableCollection.ContainsKey(databaseName))
                        {
                            immutableCollection.Add(databaseName, new HashSet<string>());
                        }
                        if (!immutableCollection[databaseName].Contains(collectionName))
                        {
                            immutableCollection[databaseName].Add(collectionName);
                        }
                    }
                }
            }
            return immutableCollection;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(2));
        }

        private void AddSwaggerServices(IServiceCollection services, TokenType tokenType)
        {
            services.AddSwaggerGen(c =>
            {
                #region Swagger generation
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                        Title = "Object Microservice API",
                        Version = "v1",
                        Description = "A microservice for providing an abstraction layer to a database engine, where HTTP actions are mapped to CRUD operations. Clients of the object service and the underlying database technology may thus change independent of one another provided the API remains consistent.",
                        Contact = new OpenApiContact
                        {
                            Name = "Erik Knudsen",
                            Email = string.Empty,
                            Url = new Uri("https://github.com/erik1066")
                        },
                        License = new OpenApiLicense
                        {
                            Name = "Apache 2.0",
                            Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0")
                        }
                    }
                );

                //if (tokenType != TokenType.None)
                //{
                //    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme 
                //    { 
                //        In = "header", 
                //        Description = "Please enter Token into field", 
                //        Name = "Authorization", 
                //        Type = "apiKey" 
                //    });

                //    c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>> {
                //        { "Bearer", Enumerable.Empty<string>() },
                //    });
                //}

                // These two lines are necessary for Swagger to pick up the C# XML comments and show them in the Swagger UI. See https://github.com/domaindrivendev/Swashbuckle.AspNetCore for more details.
                var filePath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "api.xml");
                c.IncludeXmlComments(filePath);
                #endregion
            });
        }
    }
}

#pragma warning restore 1591