using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Foundation.ObjectService.Security
{
    /// <summary>
    /// Class for handling scope requirements specific to the foundation services scoping authorization model
    /// </summary>
    public abstract class ScopeHandler : AuthorizationHandler<HasScopeRequirement>
    {
        /// <summary>
        /// Constant representing the word 'scope'
        /// </summary>
        protected const string SCOPE = "scope";

        private readonly string _systemName = string.Empty;
        private readonly string _serviceName = string.Empty;
        private static Regex _regex = new Regex(@"^[a-zA-Z0-9_\.]*$");

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="systemName">The name of the system to which the verifying service belongs</param>
        /// <param name="serviceName">The name of the verifying service</param>
        public ScopeHandler(string systemName, string serviceName)
        {
            #region Input validation
            if (string.IsNullOrEmpty(systemName))
            {
                throw new ArgumentNullException(nameof(systemName));
            }
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }
            if (string.IsNullOrEmpty(systemName.Trim()))
            {
                throw new ArgumentException(nameof(systemName));
            }
            if (string.IsNullOrEmpty(serviceName.Trim()))
            {
                throw new ArgumentException(nameof(serviceName));
            }
            if (!_regex.IsMatch(systemName))
            {
                throw new ArgumentException(nameof(systemName));
            }
            if (!_regex.IsMatch(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }
            #endregion // Input validation

            _systemName = systemName;
            _serviceName = serviceName;
        }

        /// <summary>
        /// Determines whether the required scope is present in an OAuth2 scope string
        /// </summary>
        /// <param name="requiredScope">The scope that is required for the request to be successful</param>
        /// <param name="tokenScopes">The space-delimited set of scopes, e.g. 'fdns.object.bookstore fdns.object.coffeshop'</param>
        /// <returns>Whether the required scope is present in the token scopes</returns>
        protected bool HasScope(string requiredScope, string[] tokenScopes)
        {
            // Succeed if the scope array contains the required scope
            if (tokenScopes.Any(s => s == requiredScope))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the required scope from the route
        /// </summary>
        /// <param name="resource">Resource from the authorization context</param>
        /// <returns>The scope associated with the specified route</returns>
        protected string GetScopeFromRoute(Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext resource)
        {
            int dbIndex = 0;
            int collectionIndex = 0;
            int i = 0;
            foreach (var key in resource.RouteData.Values.Keys)
            {
                if (key == "db")
                {
                    dbIndex = i;
                }
                else if (key == "collection")
                {
                    collectionIndex = i;
                }
                i++;
            }

            var db = string.Empty;
            var collection = string.Empty;
            i = 0;
            foreach (var value in resource.RouteData.Values.Values)
            {
                if (i == dbIndex)
                {
                    db = value.ToString();
                }
                if (i == collectionIndex)
                {
                    collection = value.ToString();
                }
                i++;
            }

            var scope = $"{_systemName}.{_serviceName}.{db}.{collection}";
            return scope;
        }
    }
}