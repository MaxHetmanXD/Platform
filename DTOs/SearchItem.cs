using System;

namespace Platform.DTOs
{
    public class SearchItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}