using System;
using System.Text;
using System.Text.Json;

namespace VALUM.Application.Common.Pagination
{
    public enum CursorScope
    {
        Projects = 1,
        ProjectTasks = 2
    }

    public class CursorToken
    {
        public int Version { get; set; } = 1;
        public CursorScope Scope { get; set; }
        public Guid? ProjectId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid Id { get; set; }
        public int PageSize { get; set; }
        public string? StatusFilter { get; set; }
        public string? PriorityFilter { get; set; }

        public string Encode()
        {
            var json = JsonSerializer.Serialize(this);
            var bytes = Encoding.UTF8.GetBytes(json);
            return Base64UrlEncode(bytes);
        }

        public static CursorToken? Decode(string cursor)
        {
            try
            {
                var bytes = Base64UrlDecode(cursor);
                var json = Encoding.UTF8.GetString(bytes);
                return JsonSerializer.Deserialize<CursorToken>(json);
            }
            catch
            {
                return null;
            }
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var str = input.Replace('-', '+').Replace('_', '/');
            switch (str.Length % 4)
            {
                case 2: str += "=="; break;
                case 3: str += "="; break;
            }
            return Convert.FromBase64String(str);
        }
    }
}
