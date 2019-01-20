using System;
using System.Linq;
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

            var scope = $"fdns.object.{db}.{collection}";
            return scope;
        }
    }
}