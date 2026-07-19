using Xunit;
using Microsoft.EntityFrameworkCore;
using PollBuilder.Infrastructure.Services;
using PollBuilder.Infrastructure.Data;
using PollBuilder.Domain.Entities;
using PollBuilder.Domain.Enums;
using PollBuilder.Application.DTOs.Polls;

namespace PollBuilder.UnitTests
{
    public class PollServiceTests
    {
        // Helper utility to generate an isolated database context instance per test
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Completely isolated DB instance
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task SubmitVoteAsync_ShouldRecordVote_WhenUserVotesForFirstTime()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new PollService(context);

            var pollCode = "XYZ12";
            var targetQuestionId = Guid.NewGuid();
            var targetOptionId = Guid.NewGuid();

            // Seed an active poll layout into the setup context
            var poll = new Poll
            {
                Code = pollCode,
                Title = "Test Poll",
                IsActive = true,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Id = targetQuestionId,
                        Text = "Choose one:",
                        Type = QuestionType.MultipleChoice,
                        Options = new List<Option>
                        {
                            new Option { Id = targetOptionId, Text = "Option A" }
                        }
                    }
                }
            };
            context.Polls.Add(poll);
            await context.SaveChangesAsync();

            var voteDto = new SubmitVoteDTO
            {
                VoterName = "SecureToken_UserA",
                Answers = new List<QuestionAnswerDTO> // <-- FIXED: Using the correct QuestionAnswerDTO class name
                {
                    new QuestionAnswerDTO { QuestionId = targetQuestionId, OptionId = targetOptionId }
                }
            };

            // Act
            bool result = await service.SubmitVoteAsync(pollCode, voteDto);

            // Assert
            Assert.True(result);

            // Query memory database state to ensure persistence was logged accurately
            var loggedVote = await context.Set<Vote>().FirstOrDefaultAsync(v => v.VoterToken == "SecureToken_UserA");
            Assert.NotNull(loggedVote);
            Assert.Equal(targetOptionId, loggedVote.OptionId);
        }

        [Fact]
        public async Task SubmitVoteAsync_ShouldRejectVote_WhenUserTriesToVoteTwice()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new PollService(context);

            var pollCode = "VOTE1";
            var questionId = Guid.NewGuid();
            var optionId = Guid.NewGuid();
            var duplicateVoterToken = "FingerprintToken_12345";

            var poll = new Poll
            {
                Code = pollCode,
                Title = "Anti-Cheat Verification Poll",
                IsActive = true,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Id = questionId,
                        Type = QuestionType.MultipleChoice,
                        Options = new List<Option> { new Option { Id = optionId } }
                    }
                }
            };
            context.Polls.Add(poll);

            // Simulate that this token already has an entry in the Vote ledger
            var historicalVote = new Vote
            {
                QuestionId = questionId,
                OptionId = optionId,
                VoterToken = duplicateVoterToken
            };
            context.Set<Vote>().Add(historicalVote);
            await context.SaveChangesAsync();

            var maliciousVoteDto = new SubmitVoteDTO
            {
                VoterName = duplicateVoterToken,
                Answers = new List<QuestionAnswerDTO> // <-- FIXED: Using the correct QuestionAnswerDTO class name
                {
                    new QuestionAnswerDTO { QuestionId = questionId, OptionId = optionId }
                }
            };

            // Act
            bool result = await service.SubmitVoteAsync(pollCode, maliciousVoteDto);

            // Assert
            Assert.False(result); // The system must return false and safely throw out the duplication exploit
        }

        [Fact]
        public async Task ClosePollAsync_ShouldDeactivatePollAndSetEndedStatus_WhenCreatorInvokes()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new PollService(context);

            var pollCode = "CLOSE";
            var creatorId = "Creator_99";

            var activePoll = new Poll
            {
                Code = pollCode,
                CreatorId = creatorId,
                Title = "Temporary Poll",
                IsActive = true,
                Status = PollStatus.Created
            };
            context.Polls.Add(activePoll);
            await context.SaveChangesAsync();

            // Act
            bool result = await service.ClosePollAsync(pollCode, creatorId);

            // Assert
            Assert.True(result);

            var updatedPoll = await context.Polls.FirstAsync(p => p.Code == pollCode);
            Assert.False(updatedPoll.IsActive);
            Assert.Equal(PollStatus.Ended, updatedPoll.Status);
        }

        [Fact]
        public async Task CreatePollAsync_ShouldSaveFiveStarOptions_WhenQuestionTypeIsRating()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new PollService(context);

            var newPollDto = new CreatePollDTO
            {
                Title = "Product Feedback",
                Description = "Rate our new UI layout",
                CreatorId = "Mun_Dev",
                Questions = new List<CreateQuestionDTO>
        {
            new CreateQuestionDTO
            {
                Text = "How many stars for the ThruSub theme?",
                Type = 2, // 2 maps to QuestionType.Rating in your Domain Enum
                Position = 1,
                
                // our Service unit test must explicitly provide these strings to verify mapping!
                Options = new List<string> { "1 Star", "2 Stars", "3 Stars", "4 Stars", "5 Stars" }
            }
        }
            };

            // Act
            string generatedCode = await service.CreatePollAsync(newPollDto);

            // Assert
            Assert.NotNull(generatedCode);
            Assert.Equal(5, generatedCode.Length); // Verifies your 5-character string generator code works

            // Fetch the poll from the in-memory DB to verify internal entity mapping state
            var savedPoll = await context.Polls
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .FirstAsync(p => p.Code == generatedCode);

            var ratingQuestion = savedPoll.Questions.First();
            Assert.Equal(PollBuilder.Domain.Enums.QuestionType.Rating, ratingQuestion.Type);

            // Verification: Prove your data persistence mapping logic saves all 5 options cleanly!
            Assert.Equal(5, ratingQuestion.Options.Count);
            Assert.Contains(ratingQuestion.Options, o => o.Text == "1 Star");
            Assert.Contains(ratingQuestion.Options, o => o.Text == "5 Stars");
        }

        [Fact]
        public async Task GetPollResultsAsync_ShouldTallyVotesAndFilterOpenTextResponsesCorrectly()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new PollService(context);

            var pollCode = "RSLT1";
            var mcQuestionId = Guid.NewGuid();
            var textQuestionId = Guid.NewGuid();
            var optionIdA = Guid.NewGuid();

            // Seed a complex multi-type poll structure into the database context
            var poll = new Poll
            {
                Code = pollCode,
                Title = "Final Evaluation",
                IsActive = true,
                Questions = new List<Question>
        {
            new Question
            {
                Id = mcQuestionId,
                Type = QuestionType.MultipleChoice,
                Options = new List<Option> { new Option { Id = optionIdA, Text = "Option A" } }
            },
            new Question
            {
                Id = textQuestionId,
                Type = QuestionType.OpenText
            }
        }
            };
            context.Polls.Add(poll);

            // Seed test votes into the ledger (2 for Option A, 1 valid OpenText, 1 empty OpenText)
            context.Set<Vote>().AddRange(
                new Vote { QuestionId = mcQuestionId, OptionId = optionIdA, VoterToken = "User1" },
                new Vote { QuestionId = mcQuestionId, OptionId = optionIdA, VoterToken = "User2" },
                new Vote { QuestionId = textQuestionId, OpinionText = "Great Pinterest UI!", VoterToken = "User3" },
                new Vote { QuestionId = textQuestionId, OpinionText = "", VoterToken = "User4" } // Should be filtered out
            );
            await context.SaveChangesAsync();

            // Act
            var results = await service.GetPollResultsAsync(pollCode);

            // Assert
            Assert.NotNull(results);
            Assert.Equal("Final Evaluation", results.Title);

            // Verify choice tally logic
            var mcResult = results.Questions.First(q => q.Id == mcQuestionId);
            Assert.Equal(2, mcResult.Options.First(o => o.Id == optionIdA).VoteCount);

            // Verify your text filter fix: Empty strings must be ignored by the result object array
            var textResult = results.Questions.First(q => q.Id == textQuestionId);
            Assert.Single(textResult.TextResponses);
            Assert.Equal("Great Pinterest UI!", textResult.TextResponses.First());
        }
    }
}