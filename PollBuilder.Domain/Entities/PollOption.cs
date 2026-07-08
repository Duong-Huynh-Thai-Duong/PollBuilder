using System;
using System.Collections.Generic;
using System.Text;

namespace PollBuilder.Domain.Entities
{
    public class PollOption
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Text { get; set; } = string.Empty;
        public Guid PollId { get; set; }
        public Poll? Poll { get; set; }
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();

    }
}
