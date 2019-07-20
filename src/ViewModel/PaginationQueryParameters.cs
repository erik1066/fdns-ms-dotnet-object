using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Foundation.ObjectService.ViewModel
{
    /// <summary>
    /// Class representing query parameters for simple pagination
    /// </summary>
    public sealed class PaginationQueryParameters
    {
        /// <summary>
        /// The start point for the find operation
        /// </summary>
        /// <example>10</example>
        [Range(0, Int32.MaxValue)]
        [FromQuery(Name = "start")]
        public int Start { get; set; }

        /// <summary>
        /// The size of the collection that is returned to the client
        /// </summary>
        /// <example>50</example>
        [Range(-1, Int32.MaxValue)]
        [FromQuery(Name = "size")]
        public int Limit { get; set; }
    }
}