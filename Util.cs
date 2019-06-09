using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace TheWoosh.HTTPServer
{
    public static class Util
    {
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private static readonly Random RNG = new Random();
        private static readonly int IdLength = 6;

        /// <summary>
        /// This way we don't have to log IP addresses, 
        /// but can track (ONLY FOR DEBUGGING) a request.
        /// </summary>
        /// <returns>A random string</returns>
        public static string GenerateId()
        {
            return new string(Enumerable.Repeat(Chars, IdLength).Select(s => s[RNG.Next(s.Length)]).ToArray());
        }

        public static Dictionary<string, string> MimeTypes = new Dictionary<string, string>
        {
		// Text based types (useful for encoding params)
			{ "html",   "*text/html" },
            { "css",    "*text/css" },
            { "js",     "*application/javascript" },
            { "json",   "*application/json" },
            { "txt",    "*text/plain" },
            { "xml",    "*text/xml" },
            { "csv",    "*text/csv" },
		// Images
			{ "png",        "image/png" },
            { "jpg",        "image/jpeg" },
            { "gif",        "image/gif" },
		// Audio
			{ "ogg",    "audio/ogg" },
            { "mp3",    "audio/mpeg" },
		// Fonts
			{ "ttf",    "font/ttf" },
            { "otf",    "font/otf" },
            { "woff",   "font/woff" },
            { "woff2",  "font/woff2" },
        };

        /// <summary>
        /// Parses the supplied text-based date, in the HTTP format.
        /// </summary>
        /// <param name="value">The HTTP formatted date.</param>
        /// <returns></returns>
        public static DateTime ParseHTTPDate(string value)
        {
            return DateTime.ParseExact(value, new string[] { "r" }, null, DateTimeStyles.AssumeUniversal);
        }

        /// <summary>
        /// A formatted date, as specified by RFC 7231, Section 7.1.1.1
        /// </summary>
        /// <param name="dt"></param>
        /// <returns>The formatted date</returns>
        public static string FormatHTTPDate(DateTime dt)
        {
            return dt.ToUniversalTime().ToString("r");
        }

        /// <summary>
        /// The formatted date and time, as specified by RFC 7231, Section 7.1.1.1
        /// </summary>
        /// <returns>The current date and time formatted for HTTP conversations.</returns>
        public static string GetHTTPDate()
        {
            return FormatHTTPDate(DateTime.Now);
        }

        /// <summary>
        /// Tries to find the mime type based on the extension.
        /// </summary>
        /// <param name="path">The location of the resource</param>
        /// <returns>The mime-type.</returns>
        public static string GetType(string path)
        {
            string[] e = path.Split('.');
            string extension = e[e.Length - 1];

            if (!MimeTypes.ContainsKey(extension))
                extension = "html";

            string mime = MimeTypes[extension];
            if (mime[0] == '*')
                mime = mime.Substring(1) + "; charset=UTF-8";

            return mime;
        }
    }
}
