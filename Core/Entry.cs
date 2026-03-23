using System;
using System.Collections.Generic;

namespace Crestkey.Core
{
    public class Entry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Url { get; set; } = "";
        public string Notes { get; set; } = "";
        public string Category { get; set; } = "General";
        public string TotpSecret { get; set; } = "";
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime Modified { get; set; } = DateTime.UtcNow;
        public List<string> Tags { get; set; } = new List<string>();
    }
}