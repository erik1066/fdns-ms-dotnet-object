using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Foundation.ObjectService.Data;
using Foundation.ObjectService.Security;
using Microsoft.AspNetCore.Authorization;

namespace Foundation.ObjectService.WebUI
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddResponseCaching();

            string authorizationDomain = Common.GetConfigurationVariable(Configuration, "OAUTH2_ACCESS_TOKEN_URI", "Auth:Domain", string.Empty);
            bool useAuthorization = !string.IsNullOrEmpty(authorizationDomain);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                    {
                        Title = "FDNS Object Microservice API",
                        Version = "v1",
                        Description = "A microservice for providing an abstraction layer to a database engine, where HTTP actions are mapped to CRUD operations. Clients of the object service and the underlying database technology may thus change independent of one another provided the API remains consistent.",
                        Contact = new Contact
                        {
                            Name = "Erik Knudsen",
                            Email = string.Empty,
                            Url = "https://github.com/erik1066"
                        },
                        License = new License
                        {
                            Name = "Apache 2.0",
                            Url = "https://www.apache.org/licenses/LICENSE-2.0"
                        }
                    }
                );

                if (useAuthorization)
                {
                    c.AddSecurityDefinition("Bearer", new ApiKeyScheme { In = "header", Description = "Please enter JWT with Bearer into field", Name = "Authorization", Type = "apiKey" });
                    c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>> {
                        { "Bearer", Enumerable.Empty<string>() },
                    });
                }

                // These two lines are necessary for Swagger to pick up the C# XML comments and show them in the Swagger UI. See https://github.com/domaindrivendev/Swashbuckle.AspNetCore for more details.
                var filePath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "api.xml");
                c.IncludeXmlComments(filePath);
            });
            services.AddMvc(options =>
            {
               options.InputFormatters.Insert(0, new TextPlainInputFormatter());
               options.InputFormatters.Insert(0, new JsonRawInputFormatter());
               options.OutputFormatters.Insert(0, new JsonRawOutputFormatter());
            })
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            var mongoDbHostname = BuildMongoDbConnectionString();

            services.AddSingleton<MongoDB.Driver.IMongoClient>(provider => new MongoDB.Driver.MongoClient(mongoDbHostname));
            services.AddSingleton<IObjectRepository>(provider => new MongoRepository(provider.GetService<MongoDB.Driver.IMongoClient>(), provider.GetService<ILogger<MongoRepository>>(), GetImmutableCollections()));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.Authority = authorizationDomain;
                options.Audience = Common.GetConfigurationVariable(Configuration, "OAUTH2_CLIENT_ID", "Auth:ApiIdentifier", string.Empty);
            });

            /* These policy names match the names in the [Authorize] attribute(s) in the Controller classes.
             * The HasScopeHandler class is used (see below) to pass/fail the authorization check if authorization
             * has been enabled via the microservice's configuration.
             */
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Common.READ_AUTHORIZATION_NAME, policy => policy.Requirements.Add(new HasScopeRequirement(Common.READ_AUTHORIZATION_NAME, authorizationDomain)));
                options.AddPolicy(Common.INSERT_AUTHORIZATION_NAME, policy => policy.Requirements.Add(new HasScopeRequirement(Common.INSERT_AUTHORIZATION_NAME, authorizationDomain)));
                options.AddPolicy(Common.UPDATE_AUTHORIZATION_NAME, policy => policy.Requirements.Add(new HasScopeRequirement(Common.UPDATE_AUTHORIZATION_NAME, authorizationDomain)));
                options.AddPolicy(Common.DELETE_AUTHORIZATION_NAME, policy => policy.Requirements.Add(new HasScopeRequirement(Common.DELETE_AUTHORIZATION_NAME, authorizationDomain)));
            });

            // If the developer has not configured OAuth2, then disable authentication and authorization
            if (useAuthorization)
            {
                services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
            }
            else
            {
                services.AddSingleton<IAuthorizationHandler, AlwaysAllowHandler>();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
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

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FDNS Object Microservice API V1");
            });

            app.UseAuthentication();

            app.UseMvc();
        }

        private string BuildMongoDbConnectionString()
        {
            var mongoHost = Common.GetConfigurationVariable(Configuration, "OBJECT_MONGO_HOST", "MongoDB:Host", "localhost");
            var mongoPort = Common.GetConfigurationVariable(Configuration, "OBJECT_MONGO_PORT", "MongoDB:Port", "27017");
            var mongoUserDatabase = Common.GetConfigurationVariable(Configuration, "OBJECT_MONGO_USER_DATABASE", "MongoDB:UserDatabase", "admin");
            var mongoUsername = Common.GetConfigurationVariable(Configuration, "OBJECT_MONGO_USERNAME", "MongoDB:Username", string.Empty);
            var mongoPassword = Common.GetConfigurationVariable(Configuration, "OBJECT_MONGO_PASSWORD", "MongoDB:Password", string.Empty);

            var usernamePassword = string.IsNullOrEmpty(mongoUsername) ? string.Empty : $"{mongoUsername}:{mongoPassword}@";
            var userDatabase = string.IsNullOrEmpty(mongoUserDatabase) ? string.Empty : $"/{mongoUserDatabase}";

            return $"mongodb://{usernamePassword}{mongoHost}:{mongoPort}{userDatabase}";
        }

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
    }
#pragma warning restore 1591
}