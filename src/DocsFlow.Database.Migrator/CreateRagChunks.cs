using FluentMigrator;

namespace DocsFlow.Database.Migrator;

/// <summary>
/// Фрагменты текста с эмбеддингами — хранилище для поиска по смыслу. Источник обозначается
/// строковым локатором (<c>source_key</c>): доменной модели документов ещё нет, а пайплайн
/// от неё и не зависит.
/// </summary>
[Migration(20260731120000)]
public sealed class CreateRagChunks : Migration
{
    /// <summary>
    /// Размерность вектора — часть схемы, а не настройка: pgvector требует фиксированную длину
    /// колонки, иначе по ней нельзя построить ANN-индекс. Смена модели эмбеддингов на другую
    /// размерность — отдельная миграция и переиндексация корпуса.
    /// </summary>
    /// <remarks>
    /// 1024 работает в обе стороны: столько отдают распространённые локальные модели (bge-m3,
    /// mxbai-embed-large), а облачные text-embedding-3-* умеют усекаться до заданной длины.
    /// Привязка к 1536 сделала бы локальный запуск невозможным без миграции.
    /// </remarks>
    private const int EmbeddingDimensions = 1024;

    public override void Up()
    {
        // Расширение включает мигратор, а не образ: образ приносит бинарники, схему настраивает
        // тот же процесс, что и таблицы. Приложению права на это по-прежнему не нужны.
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS vector");

        Create.Table("rag_chunks")
            // Идентификатор назначает приложение (UUIDv7), как и для users.
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("source_key").AsCustom("text").NotNullable()
            // Номер фрагмента внутри источника, 0-based: по нему собирается ссылка на первоисточник.
            .WithColumn("ordinal").AsInt32().NotNullable()
            .WithColumn("content").AsCustom("text").NotNullable()
            .WithColumn("embedding").AsCustom($"vector({EmbeddingDimensions})").NotNullable()
            // Чем считали вектор: пространства разных моделей несравнимы, и при смене модели
            // видно, какие строки ещё не переиндексированы.
            .WithColumn("embedding_model").AsCustom("text").NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
                .WithDefault(SystemMethods.CurrentDateTimeOffset);

        // Пара (источник, номер) уникальна — переиндексация источника остаётся идемпотентной.
        Create.UniqueConstraint("ux_rag_chunks_source_ordinal")
            .OnTable("rag_chunks")
            .Columns("source_key", "ordinal");

        Create.Index("ix_rag_chunks_source_key")
            .OnTable("rag_chunks")
            .OnColumn("source_key").Ascending();

        // HNSW ищет приблизительно, зато не сканирует таблицу целиком. Класс операторов обязан
        // совпадать с оператором запроса: vector_cosine_ops ↔ <=>, иначе индекс не применится.
        Execute.Sql("CREATE INDEX ix_rag_chunks_embedding ON rag_chunks USING hnsw (embedding vector_cosine_ops)");
    }

    // Расширение не удаляем: оно общее для базы, а не собственность этой таблицы.
    public override void Down() =>
        Delete.Table("rag_chunks");
}
