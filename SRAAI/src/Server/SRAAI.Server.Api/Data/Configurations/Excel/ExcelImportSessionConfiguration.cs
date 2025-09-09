namespace SRAAI.Server.Api.Data.Configurations.Excel;

public class ExcelImportSessionConfiguration : IEntityTypeConfiguration<Models.Excel.ExcelImportSession>
{
	public void Configure(EntityTypeBuilder<Models.Excel.ExcelImportSession> builder)
	{
		builder.ToTable("ExcelImportSessions");
		builder.HasKey(e => e.Id);
		builder.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
		builder.Property(e => e.DatasetName).IsRequired().HasMaxLength(128);
		builder.Property(e => e.NewVersionNo).IsRequired();
		builder.Property(e => e.PreviousVersionNo).IsRequired();
		builder.Property(e => e.InsertedCount).IsRequired();
		builder.Property(e => e.UpdatedCount).IsRequired();
		builder.Property(e => e.DeletedCount).IsRequired();
		builder.Property(e => e.AiSummaryEn).HasColumnType("nvarchar(max)");
		builder.Property(e => e.AiSummaryMr).HasColumnType("nvarchar(max)");
		builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

		builder.HasIndex(e => new { e.DatasetName, e.NewVersionNo });
	}
}


