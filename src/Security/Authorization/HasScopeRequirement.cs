using System;
using Microsoft.AspNetCore.Authorization;

namespace Foundation.ObjectService.Security
{
    /// <summary>
    /// Class representing a set of scopes that an HTTP request must be authorized for
    /// </summary>
    public class HasScopeRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// The issuer
        /// </summary>
        public string Issuer { get; }

        /// <summary>
        /// The scope(s)
        /// </summary>
        public string Scope { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="scope">The scope(s) to use</param>
        /// <param name="issuer">The issuer</param>
        public HasScopeRequirement(string scope, string issuer)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        }
    }
}