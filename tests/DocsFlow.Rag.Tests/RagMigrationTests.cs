using Dapper;
using DocsFlow.Database;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DocsFlow.Rag.Tests;

public sealed class RagMigrationTests(PgVectorFixture fixture) : IClassFixture<PgVectorFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_vector_extension_is_installed()
        => (await ScalarAsync<int>("SELECT count(*) FROM pg_extension WHERE extname = 'vector'"))
            .ShouldBe(1);

    [Fact]
    public async Task The_embedding_column_has_the_expected_dimensions()
    {
        // Размерность зашита в схему, и приложение обязано просить у модели ровно её.
        var type = await ScalarAsync<string>(
            """
            SELECT format_type(a.atttypid, a.atttypmod)
            FROM pg_attribute a
            WHERE a.attrelid = 'rag_chunks'::regclass AND a.attname = 'embedding'
            """);

        type.ShouldBe("vector(1024)");
    }

    [Fact]
    public async Task The_embedding_index_is_hnsw_with_cosine_distance()
    {
        var definition = await ScalarAsync<string>(
            "SELECT indexdef FROM pg_indexes WHERE indexname = 'ix_rag_chunks_embedding'");

        // Класс операторов обязан совпадать с оператором запроса (<=>), иначе индекс не применится
        // и поиск незаметно превратится в полный скан.
        definition.ShouldNotBeNull();
        definition.ShouldContain("hnsw");
        definition.ShouldContain("vector_cosine_ops");
    }

    [Fact]
    public async Task A_source_cannot_have_two_chunks_with_the_same_ordinal()
    {
        var constraints = await ScalarAsync<int>(
            """
            SELECT count(*)
            FROM pg_constraint
            WHERE conrelid = 'rag_chunks'::regclass AND conname = 'ux_rag_chunks_source_ordinal'
            """);

        constraints.ShouldBe(1);
    }

    private async Task<T?> ScalarAsync<T>(string sql)
    {
        var factory = fixture.Services.GetRequiredService<IDbConnectionFactory>();

        await using var connection = await factory.OpenConnectionAsync(Ct);

        return await connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, cancellationToken: Ct));
    }
}
