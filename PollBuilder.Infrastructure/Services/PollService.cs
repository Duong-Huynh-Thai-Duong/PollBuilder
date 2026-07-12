using Microsoft.EntityFrameworkCore;
using PollBuilder.Application.DTOs.Polls;
using PollBuilder.Application.Interfaces;
using PollBuilder.Domain.Entities;
using PollBuilder.Domain.Enums;
using PollBuilder.Infrastructure.Data;

namespace PollBuilder.Infrastructure.Services
{
    public class PollService : IPollService
    {
        private readonly ApplicationDbContext _context;

        // "Inject" the database context so we can talk to SQL Server
        public PollService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreatePollAsync(CreatePollDTO createPollDto)
        {
            // 1. Generate a unique 5-character short code
            string shortCode = GenerateShortCode();

            // Safety check: ensure the code doesn't already exist in the database
            while (await _context.Polls.AnyAsync(p => p.Code == shortCode))
            {
                shortCode = GenerateShortCode();
            }

            // 2. Map the DTO to our pure Domain Entities
            var poll = new Poll
            {
                Title = createPollDto.Title,
                Description = createPollDto.Description,
                CreatorId = createPollDto.CreatorId,
                Code = shortCode,
                LaunchDate = DateTime.UtcNow,
                Status = PollStatus.Created,
                IsActive = true
            };

            // Loop through the questions provided by the user
            foreach (var qDto in createPollDto.Questions)
            {
                var question = new Question
                {
                    Text = qDto.Text,
                    Type = (QuestionType)qDto.Type,
                    Position = qDto.Position
                };

                // Only add predefined options if it's a MultipleChoice or YesNo question
                if (question.Type == QuestionType.MultipleChoice || question.Type == QuestionType.YesNo)
                {
                    int optionPosition = 1;
                    foreach (var optText in qDto.Options)
                    {
                        question.Options.Add(new Option
                        {
                            Text = optText,
                            Position = optionPosition++
                        });
                    }
                }

                poll.Questions.Add(question);
            }

            // 3. Save everything to the database
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            return shortCode;
        }

        public async Task<PollResponseDTO?> GetPollByCodeAsync(string code)
        {
            // 1. The EF Core Query
            var poll = await _context.Polls
                .Include(p => p.Questions)           // Join the Questions table
                    .ThenInclude(q => q.Options)     // Join the Options table attached to those Questions
                .FirstOrDefaultAsync(p => p.Code == code);

            // 2. Failsafe: Did we find it?
            if (poll == null)
            {
                return null;
            }

            // 3. Map the raw database entities into our safe Response DTOs
            var response = new PollResponseDTO
            {
                Code = poll.Code,
                Title = poll.Title,
                Description = poll.Description,
                LaunchDate = poll.LaunchDate,
                Status = poll.Status.ToString(), // Converts the Enum (0, 1) into readable text ("Created", "Active")

                // Map Questions and sort them by Position
                Questions = poll.Questions.OrderBy(q => q.Position).Select(q => new QuestionResponseDTO
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = (int)q.Type,
                    Position = q.Position,

                    // Map Options and sort them by Position
                    Options = q.Options.OrderBy(o => o.Position).Select(o => new OptionResponseDTO
                    {
                        Id = o.Id,
                        Text = o.Text,
                        Position = o.Position
                    }).ToList()
                }).ToList()
            };

            return response;
        }

        // Helper method to generate the random 5-character string
        private string GenerateShortCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();

            return new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}