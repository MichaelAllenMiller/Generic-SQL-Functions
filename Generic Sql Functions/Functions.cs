//------------------------------------------------------------------------------
// <copyright file="CSSqlFunction.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;


namespace ElevenSquared {
    public partial class SqlFunctions {
        [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true)]
        public static SqlString ProperCase(String value) {
            if (String.IsNullOrEmpty(value)) { return new SqlString(string.Empty); }
            String result = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(value);
            return new SqlString(result);
        }

        [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true)]
        public static SqlString TitleCase(String value) {
            return ProperCase(value);
        }

        [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true)]
        public static SqlString Trim(String value) {
            return new SqlString(value.Trim());
        }

        [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true)]
        public static SqlString RegexReplace(String input, String pattern, String replacement) {
            if (String.IsNullOrEmpty(input)) { return new SqlString(string.Empty); }
            return new SqlString(System.Text.RegularExpressions.Regex.Replace(input ?? "", pattern ?? "", replacement ?? "", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }

        [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true)]
        public static SqlString RegexGetMatch(String input, String pattern, String groupName) {
            if (String.IsNullOrEmpty(input)) { return new SqlString(string.Empty); }
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Groups[groupName] == null || !match.Groups[groupName].Success) { return new SqlString(String.Empty); }
            return new SqlString(match.Groups[groupName].Value);
        }

        [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true)]
        public static SqlBoolean RegexIsMatch(String input, String pattern) {
            return new SqlBoolean(System.Text.RegularExpressions.Regex.IsMatch(input ?? "", pattern ?? "", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }
        
        [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true)]
        public static SqlString GetMd5Hash(String input) {
            byte[] byteInput = System.Text.Encoding.UTF8.GetBytes(input);

            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] byteHashedPassword = md5.ComputeHash(byteInput);
                System.Text.StringBuilder result = new System.Text.StringBuilder(byteHashedPassword.Length * 2);

                for (int i = 0; i < byteHashedPassword.Length; i++) {
                    result.Append(byteHashedPassword[i].ToString("x2"));
                }
                return new SqlString(result.ToString());
            }
        }
        
        [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true)]
        public static SqlString GetJson(String input, String key) {
            System.Collections.Generic.Dictionary<String, Object> result = new System.Collections.Generic.Dictionary<String, Object>();

            try {
                PopulateJsonDictionary(result, Procurios.Public.JSON.JsonDecode(input), "");
                if (key == ">") {
                    System.Text.StringBuilder b = new System.Text.StringBuilder();
                    foreach (String a in result.Keys) {
                        if (b.Length > 0) { b.Append(", "); }
                        b.Append("\"" + a + "\"");
                    }
                    return new SqlString("[" + b.ToString() + "]");
                } else if (result.ContainsKey(key)) {
                    return new SqlString(result[key].ToString());
                } else {
                    return SqlString.Null;
                }
            } catch (Exception e) {
                return SqlString.Null;
            }
        }
        
        [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true)]
        public static SqlDateTime GetJsonDate(String input, String key) {
            try {
                return SqlDateTime.Parse(GetJson(input, key).ToString());
            } catch (System.Exception e) {
                return SqlDateTime.Null;
            }

        }

        [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true)]
        public static SqlDateTime ParseDate(String input) {
            try {
                return new SqlDateTime(DateTime.Parse(input));
            } catch (Exception e) {
                return SqlDateTime.Null;
            }
        }

        [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true)]
        public static SqlInt32 LevenshteinDistance(String a, String b) {
            return new SqlInt32(GenericSqlFunctions.DamerauLevenshtein.DamerauLevenshteinDistance(a, b));
        }

        [Microsoft.SqlServer.Server.SqlFunction(IsDeterministic = true)]
        public static SqlDouble LevenshteinConfidence(String a, String b) {
            SqlInt32 distance = LevenshteinDistance(a, b);
            if (distance.Value == 0) { return new SqlDouble(1); }
            if (a.Length == 0) { return new SqlDouble(0); }
            return new SqlDouble(1 - (Double)distance / (Double)a.Length);
        }


        static void PopulateJsonDictionary(System.Collections.Generic.Dictionary<String, Object> result, Object json, String prefix) {
            if (json == null) { return; }
            if (json.GetType().FullName == "System.Collections.Hashtable") {
                System.Collections.Hashtable o = (System.Collections.Hashtable)json;
                foreach (String key in o.Keys) {
                    result.Add(prefix + key, o[key]);
                    PopulateJsonDictionary(result, o[key], prefix + key + ".");
                }
            } else if (json.GetType().FullName == "System.Collections.ArrayList") {
                System.Collections.ArrayList o = (System.Collections.ArrayList)json;
                if (prefix.EndsWith(".")) { prefix = prefix.Substring(0, prefix.Length - 1); }
                result.Add(prefix + ".length", o.Count);
                for (int index = 0; index < o.Count; index++) {
                    result.Add(prefix + "[" + index + "]", o[index]);
                    PopulateJsonDictionary(result, o[index], prefix + "[" + index + "].");
                }
            } else {
                result.Add(prefix, json);
            }
        }
    }
}