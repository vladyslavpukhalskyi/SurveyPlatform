using Bogus;
using SurveyPlatform.Core.Entities;
using SurveyPlatform.Infrastructure;

namespace SurveyPlatform.Tests.DatabaseSeeder; 

public static class DatabaseSeeder
{
    public static void Seed10KRecords(SurveyDbContext dbContext)
    {
        if (dbContext.Surveys.Count() > 10) return;

        Randomizer.Seed = new Random(42);
        var faker = new Faker();

        var surveysToInsert = new List<Survey>();
        var questionsToInsert = new List<Question>();
        var optionsToInsert = new List<Option>();
        var responsesToInsert = new List<Response>();
        var answersToInsert = new List<Answer>();

        for (int s = 0; s < 50; s++)
        {
            var survey = new Survey
            {
                Id = Guid.NewGuid(),
                Title = faker.Commerce.ProductName() + " Survey",
                Description = faker.Lorem.Paragraph(),
                CreatedBy = faker.Internet.Email(),
                CreatedAt = faker.Date.Past(1).ToUniversalTime(),
                IsActive = true,
                ExpiresAt = faker.Date.Future(1).ToUniversalTime()
            };
            surveysToInsert.Add(survey);

            for (int i = 1; i <= 5; i++)
            {
                var qType = (i == 1) ? QuestionType.Text : faker.PickRandom<QuestionType>();
                
                var question = new Question
                {
                    Id = Guid.NewGuid(),
                    SurveyId = survey.Id,
                    Text = faker.Lorem.Sentence() + "?",
                    Type = qType,
                    IsRequired = true,
                    Order = i
                };
                questionsToInsert.Add(question);

                if (qType == QuestionType.SingleChoice || qType == QuestionType.MultipleChoice)
                {
                    for (int j = 1; j <= 4; j++)
                    {
                        optionsToInsert.Add(new Option
                        {
                            Id = Guid.NewGuid(),
                            QuestionId = question.Id,
                            Text = $"Option {j}",
                            Order = j
                        });
                    }
                }
            }
        }

        foreach (var survey in surveysToInsert)
        {
            var surveyQuestions = questionsToInsert.Where(q => q.SurveyId == survey.Id).ToList();

            for (int r = 0; r < 50; r++)
            {
                var response = new Response
                {
                    Id = Guid.NewGuid(),
                    SurveyId = survey.Id,
                    RespondentEmail = $"user_{Guid.NewGuid()}@test.com",
                    SubmittedAt = faker.Date.Recent(10).ToUniversalTime()
                };
                responsesToInsert.Add(response);

                foreach (var question in surveyQuestions)
                {
                    answersToInsert.Add(new Answer
                    {
                        Id = Guid.NewGuid(),
                        ResponseId = response.Id,
                        QuestionId = question.Id,
                        Value = question.Type switch
                        {
                            QuestionType.Rating => faker.Random.Int(1, 5).ToString(),
                            QuestionType.SingleChoice => "Option 1",
                            QuestionType.MultipleChoice => "Option 1",
                            _ => faker.Lorem.Word()
                        }
                    });
                }
            }
        }

        dbContext.Surveys.AddRange(surveysToInsert);
        dbContext.Questions.AddRange(questionsToInsert);
        dbContext.Options.AddRange(optionsToInsert);
        dbContext.Responses.AddRange(responsesToInsert);
        dbContext.Answers.AddRange(answersToInsert);

        dbContext.SaveChanges();
    }
}