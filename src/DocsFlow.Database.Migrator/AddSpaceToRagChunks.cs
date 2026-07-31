using FluentMigrator;

namespace DocsFlow.Database.Migrator;

/// <summary>
/// Привязывает фрагменты индекса к space. До этой миграции <c>rag_chunks</c> не знала о владении,
/// и поиск по смыслу шёл по всей таблице — то есть по чужим документам тоже.
/// </summary>
[Migration(20260731152000)]
public sealed class AddSpaceToRagChunks : Migration
{
    public override void Up()
    {
        // Существующие фрагменты сносим, а не додумываем им space: чанки и вектора — производные
        // данные, они восстанавливаются переиндексацией оригиналов. Раздать их наугад какому-нибудь
        // space значило бы отдать чужой текст в чужую выдачу, а nullable-колонка оставила бы
        // строки, которые не принадлежат никому и потому видны всем.
        Delete.FromTable("rag_chunks").AllRows();

        Alter.Table("rag_chunks")
            .AddColumn("space_id").AsGuid().NotNullable();

        Create.ForeignKey("fk_rag_chunks_space")
            .FromTable("rag_chunks").ForeignColumn("space_id")
            .ToTable("spaces").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // Локатор источника уникален внутри space, а не глобально: два space могут независимо
        // хранить документ с одинаковым ключом, и это не конфликт.
        Delete.UniqueConstraint("ux_rag_chunks_source_ordinal").FromTable("rag_chunks");

        Create.UniqueConstraint("ux_rag_chunks_space_source_ordinal")
            .OnTable("rag_chunks")
            .Columns("space_id", "source_key", "ordinal");

        // Индекс по одному source_key больше не нужен: все обращения к фрагментам идут с известным
        // space, а составной индекс обслуживает и их, и удаление всего индекса space.
        Delete.Index("ix_rag_chunks_source_key").OnTable("rag_chunks");

        Create.Index("ix_rag_chunks_space_source")
            .OnTable("rag_chunks")
            .OnColumn("space_id").Ascending()
            .OnColumn("source_key").Ascending();
    }

    public override void Down()
    {
        Delete.ForeignKey("fk_rag_chunks_space").OnTable("rag_chunks");
        Delete.UniqueConstraint("ux_rag_chunks_space_source_ordinal").FromTable("rag_chunks");
        Delete.Index("ix_rag_chunks_space_source").OnTable("rag_chunks");
        Delete.Column("space_id").FromTable("rag_chunks");

        Create.UniqueConstraint("ux_rag_chunks_source_ordinal")
            .OnTable("rag_chunks")
            .Columns("source_key", "ordinal");

        Create.Index("ix_rag_chunks_source_key")
            .OnTable("rag_chunks")
            .OnColumn("source_key").Ascending();
    }
}
