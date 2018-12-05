using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Foundation.ObjectService
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments    
    public class SearchStringConverter // converted to C# from https://github.com/CDCgov/fdns-ms-object/blob/master/src/main/java/gov/cdc/foundation/helper/QueryHelper.java
    {
        // check if a number
        private static bool IsNumber(string str) => System.Text.RegularExpressions.Regex.Match(str, "-?\\d+(\\.\\d+)?").Success;

        // check if a boolean
        private static bool IsBoolean(string str) => System.Text.RegularExpressions.Regex.Match(str, "true|false").Success;

        // build a comparison
        private static JObject BuildComparison(string op, object value) => new JObject( new JProperty("$" + op, value) );

        // get the names of a JObject
        private static IList<string> GetNames(JObject source) => source.Count == 0 ? new List<string>() : source.Properties().Select(p => p.Name).ToList();

        // merge two JObjects
	    private static JObject MergeObjects(JToken source, JObject target)
        {
            target.Merge(source, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union });
            return target;
        }

        // build and merge a comparison
        private static JObject BuildAndMergeComparison(string op, string field, object raw, JObject json) 
        {
            JObject comparison = BuildComparison(op, raw);

            // if the field exists then merge it first
            if (json.ContainsKey(field)) 
            {
                JObject merged = MergeObjects(json[field], comparison);
                return merged;
            } 
            else 
            {
                return comparison;
            }
        }

        private static (string fieldName, string rawValue) Split(string term, string splitValue) 
        {
            string[] parts = term.Split(splitValue);
            string field = parts[0];
            string raw = parts[1];
            return (field, raw);
        }

        private static void AddNumberComparisonProperty(JObject json, string fieldName, string rawValue, string op)
        {
            if (IsNumber(rawValue))
            {
                json.Add(fieldName, new JObject(BuildAndMergeComparison(op, fieldName, Double.Parse(rawValue), json)));
            }
        }

        // build the query for MongoDB
	    public static string BuildQuery(string qs) 
        {
            if (string.IsNullOrEmpty(qs))
            {
                return string.Empty;
            }

		    JObject json = new JObject();
		    // build search terms
		    string[] terms = qs.Split(" ");
            foreach (var term in terms)
            {			
                if (term.Contains(">=")) 
                {
                    (string fieldName, string rawValue) = Split(term, ">=");
                    AddNumberComparisonProperty(json, fieldName, rawValue, "gte");
                }
                else if (term.Contains("<=")) 
                {
                    (string fieldName, string rawValue) = Split(term, "<=");
                    AddNumberComparisonProperty(json, fieldName, rawValue, "lte");
                }
                else if (term.Contains(">")) 
                {
                    (string fieldName, string rawValue) = Split(term, ">");
                    AddNumberComparisonProperty(json, fieldName, rawValue, "gt");
                }
                else if (term.Contains("<")) 
                {
                    (string fieldName, string rawValue) = Split(term, "<");
                    AddNumberComparisonProperty(json, fieldName, rawValue, "lt");
                }
                else if (term.Contains("!:")) 
                {
                    (string fieldName, string rawValue) = Split(term, "!:");
                    if (IsNumber(rawValue))
                    {
                        AddNumberComparisonProperty(json, fieldName, rawValue, "ne");
                    }
                    else
                    {
                        json.Add(fieldName, new JObject(BuildAndMergeComparison("ne", fieldName, rawValue, json)));
                    }
                }
                else if (term.Contains(":")) 
                {
                    (string fieldName, string rawValue) = Split(term, ":");
                    
                    // convert types
                    if (IsNumber(rawValue))
                    {
                        json.Add(fieldName, new JValue(Double.Parse(rawValue)));                    
                    }
                    else if (IsBoolean(rawValue))
                    {
                        json.Add(fieldName, new JValue(bool.Parse(rawValue)));                    
                    }
                    else
                    {
                        json.Add(fieldName, new JValue(rawValue));
                    }
                }
            }

            return json.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
#pragma warning restore 1591
}