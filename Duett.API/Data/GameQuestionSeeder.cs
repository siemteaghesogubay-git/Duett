using Duett.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Data;

public static class GameQuestionSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var questions = new[]
        {
            CreateQuestion(
                Guid.Parse("11111111-1111-4111-8111-111111111111"),
                "What is my perfect date?",
                "Dinner at a restaurant",
                "A walk and coffee",
                "A concert",
                "A game night"),

            CreateQuestion(
                Guid.Parse("22222222-2222-4222-8222-222222222222"),
                "How do I prefer to spend a free Sunday?",
                "Relaxing at home",
                "Spending time with friends",
                "Being out in nature",
                "Discovering a new place"),

            CreateQuestion(
                Guid.Parse("33333333-3333-4333-8333-333333333333"),
                "What kind of trip would I choose?",
                "Sun and swimming",
                "A city weekend",
                "Hiking and adventure",
                "A peaceful cabin getaway"),

            CreateQuestion(
                Guid.Parse("44444444-4444-4444-8444-444444444444"),
                "What would I most like to cook with someone?",
                "Homemade pizza",
                "Pasta",
                "A new dish",
                "A dessert"),

            CreateQuestion(
                Guid.Parse("55555555-5555-4555-8555-555555555555"),
                "What kind of movie would I choose?",
                "Comedy",
                "Thriller",
                "Romance",
                "Science fiction"),

            CreateQuestion(
                Guid.Parse("66666666-6666-4666-8666-666666666666"),
                "What kind of surprise would I appreciate most?",
                "A personal message",
                "A planned outing",
                "My favourite food",
                "A small, thoughtful gift"),

            CreateQuestion(
                Guid.Parse("77777777-7777-4777-8777-777777777777"),
                "How do I prefer to plan a weekend?",
                "Plan everything in advance",
                "Decide spontaneously",
                "Plan one thing and leave the rest open",
                "Let someone else suggest something"),

            CreateQuestion(
                Guid.Parse("88888888-8888-4888-8888-888888888888"),
                "What would I most like to try together?",
                "A dance class",
                "A cooking class",
                "Climbing",
                "A creative workshop"),

            CreateQuestion(
                Guid.Parse("99999999-9999-4999-8999-999999999999"),
                "What kind of game would I choose?",
                "A trivia game",
                "A strategy game",
                "A card game",
                "A cooperative game"),

            CreateQuestion(
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                "Where do I feel most relaxed?",
                "By the sea",
                "In the forest",
                "At a cosy café",
                "On the sofa at home"),

            CreateQuestion(
                Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
                "What do I appreciate most in a conversation?",
                "Humour and laughter",
                "Deep reflections",
                "Stories from everyday life",
                "Ideas and dreams for the future"),

            CreateQuestion(
                Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
                "How do I prefer to start a morning off?",
                "With a leisurely breakfast",
                "With a walk",
                "With a workout",
                "By sleeping in"),

            CreateQuestion(
                Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd"),
                "What do I prefer to do on a rainy day?",
                "Read a book",
                "Watch a movie or a series",
                "Bake something",
                "Visit a museum"),

            CreateQuestion(
                Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee"),
                "What kind of music experience would I choose?",
                "A big concert",
                "A small acoustic gig",
                "A night of dancing",
                "Listening to favourite songs at home"),

            CreateQuestion(
                Guid.Parse("ffffffff-ffff-4fff-8fff-ffffffffffff"),
                "What do I most enjoy doing on an outing together?",
                "Having a picnic",
                "Taking photos of the surroundings",
                "Exploring new walking routes",
                "Visiting a local market")
        };

        var questionIds = questions
            .Select(question => question.Id)
            .ToArray();

        // Hämta befintliga frågor med deras alternativ.
        // Spårning behövs för att spara textändringarna.
        var existingQuestions = await context.GameQuestions
            .AsTracking()
            .Include(question => question.Options)
            .Where(question => questionIds.Contains(question.Id))
            .ToDictionaryAsync(
                question => question.Id,
                cancellationToken);

        foreach (var seedQuestion in questions)
        {
            if (!existingQuestions.TryGetValue(
                    seedQuestion.Id,
                    out var existingQuestion))
            {
                // Endast nya frågor får nya alternativ-ID:n.
                context.GameQuestions.Add(seedQuestion);
                continue;
            }

            var existingOptions = existingQuestion.Options
                .OrderBy(option => option.SortOrder)
                .ToList();

            var seedOptions = seedQuestion.Options
                .OrderBy(option => option.SortOrder)
                .ToList();

            // Den ursprungliga seedern använder ordningen 1–4.
            // Stoppa om strukturen avviker så att vi inte kopplar
            // en översättning till fel svarsalternativ.
            var expectedSortOrders = Enumerable
                .Range(1, seedOptions.Count)
                .ToArray();

            var actualSortOrders = existingOptions
                .Select(option => option.SortOrder)
                .ToArray();

            if (!actualSortOrders.SequenceEqual(expectedSortOrders))
            {
                throw new InvalidOperationException(
                    $"Frågan {existingQuestion.Id} har oväntade " +
                    "svarsalternativ. Kontrollera antal och SortOrder " +
                    "innan frågan översätts.");
            }

            existingQuestion.Text = seedQuestion.Text;

            for (var index = 0; index < seedOptions.Count; index++)
            {
                // Uppdatera endast texten.
                // Befintligt Id, QuestionId och SortOrder behålls.
                existingOptions[index].Text = seedOptions[index].Text;
            }

            // Befintligt IsActive behålls.
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static GameQuestion CreateQuestion(
        Guid id,
        string text,
        params string[] options)
    {
        var question = new GameQuestion
        {
            Id = id,
            Text = text,
            IsActive = true
        };

        for (var index = 0; index < options.Length; index++)
        {
            question.Options.Add(new GameQuestionOption
            {
                Id = Guid.NewGuid(),
                QuestionId = id,
                Text = options[index],
                SortOrder = index + 1
            });
        }

        return question;
    }
}