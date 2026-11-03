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
                "Vilken är min perfekta dejt?",
                "Middag på restaurang",
                "En promenad och fika",
                "En konsert",
                "En spelkväll"),

            CreateQuestion(
                Guid.Parse("22222222-2222-4222-8222-222222222222"),
                "Hur tillbringar jag helst en ledig söndag?",
                "Tar det lugnt hemma",
                "Umgås med vänner",
                "Är ute i naturen",
                "Upptäcker en ny plats"),

            CreateQuestion(
                Guid.Parse("33333333-3333-4333-8333-333333333333"),
                "Vilken sorts resa väljer jag helst?",
                "Sol och bad",
                "En storstadshelg",
                "Vandring och äventyr",
                "En lugn stuga"),

            CreateQuestion(
                Guid.Parse("44444444-4444-4444-8444-444444444444"),
                "Vad lagar jag helst tillsammans med någon?",
                "Hemmagjord pizza",
                "Pasta",
                "En ny maträtt",
                "En efterrätt"),

            CreateQuestion(
                Guid.Parse("55555555-5555-4555-8555-555555555555"),
                "Vilken film väljer jag helst?",
                "Komedi",
                "Thriller",
                "Romantik",
                "Science fiction"),

            CreateQuestion(
                Guid.Parse("66666666-6666-4666-8666-666666666666"),
                "Vilken överraskning uppskattar jag mest?",
                "Ett personligt meddelande",
                "En planerad utflykt",
                "Min favoritmat",
                "En liten genomtänkt present"),

            CreateQuestion(
                Guid.Parse("77777777-7777-4777-8777-777777777777"),
                "Hur planerar jag helst en helg?",
                "Planerar allt i förväg",
                "Bestämmer spontant",
                "Planerar en sak och lämnar resten öppet",
                "Låter någon annan föreslå något"),

            CreateQuestion(
                Guid.Parse("88888888-8888-4888-8888-888888888888"),
                "Vad vill jag helst prova tillsammans?",
                "En danskurs",
                "En matlagningskurs",
                "Klättring",
                "En kreativ workshop"),

            CreateQuestion(
                Guid.Parse("99999999-9999-4999-8999-999999999999"),
                "Vilket sällskapsspel väljer jag helst?",
                "Frågesport",
                "Strategispel",
                "Kortspel",
                "Ett samarbetsspel"),

            CreateQuestion(
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                "Vilken miljö får mig att slappna av mest?",
                "Vid havet",
                "I skogen",
                "På ett mysigt kafé",
                "Hemma i soffan"),

            CreateQuestion(
                Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
                "Vad uppskattar jag mest i ett samtal?",
                "Humor och skratt",
                "Djupa funderingar",
                "Berättelser från vardagen",
                "Idéer och framtidsdrömmar"),

            CreateQuestion(
                Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
                "Hur börjar jag helst en ledig morgon?",
                "Med en lång frukost",
                "Med en promenad",
                "Med träning",
                "Med sovmorgon"),

            CreateQuestion(
                Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd"),
                "Vad gör jag helst en regnig dag?",
                "Läser en bok",
                "Ser på film eller en serie",
                "Bakar något",
                "Besöker ett museum"),

            CreateQuestion(
                Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee"),
                "Vilken musikupplevelse väljer jag helst?",
                "En stor konsert",
                "En liten akustisk spelning",
                "En kväll med dans",
                "Lyssna på favoritlåtar hemma"),

            CreateQuestion(
                Guid.Parse("ffffffff-ffff-4fff-8fff-ffffffffffff"),
                "Vad gör jag helst på en gemensam utflykt?",
                "Har picknick",
                "Fotograferar omgivningen",
                "Utforskar nya promenadstråk",
                "Besöker en lokal marknad")
        };

        var questionIds = questions
            .Select(question => question.Id)
            .ToArray();

        var existingIds = await context.GameQuestions
            .AsNoTracking()
            .Where(question => questionIds.Contains(question.Id))
            .Select(question => question.Id)
            .ToListAsync(cancellationToken);

        var existingIdSet = existingIds.ToHashSet();

        foreach (var question in questions)
        {
            // Befintliga frågor och deras alternativ lämnas orörda.
            if (existingIdSet.Contains(question.Id))
            {
                continue;
            }

            context.GameQuestions.Add(question);
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