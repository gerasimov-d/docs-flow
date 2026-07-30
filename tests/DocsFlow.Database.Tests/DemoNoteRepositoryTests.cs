using DocsFlow.Database;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DocsFlow.Database.Tests;

public sealed class DemoNoteRepositoryTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Add_then_Get_returns_the_note_with_snake_case_columns_mapped()
    {
        using var scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<DemoNoteRepository>();

        var id = await repository.AddAsync("медицинская справка", Ct);
        var note = await repository.GetAsync(id, Ct);

        note.ShouldNotBeNull();
        note.Id.ShouldBe(id);
        note.Title.ShouldBe("медицинская справка");
        // created_at → CreatedAt: колонка заполнилась дефолтом и смаплена по snake_case-конвенции.
        note.CreatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Get_returns_null_for_a_missing_id()
    {
        using var scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<DemoNoteRepository>();

        (await repository.GetAsync(-1, Ct)).ShouldBeNull();
    }
}
