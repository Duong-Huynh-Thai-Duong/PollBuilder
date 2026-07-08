using System;
using System.Collections.Generic;
using System.Text;

namespace PollBuilder.Domain.Entities
{
    public class Poll
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Question { get; set; } = string.Empty;
        public string ShortUrl { get; set; } = string.Empty;
        public bool IsClosed { get; set; } = false;
        public string? CreatorId { get; set; }
        public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    }
}
