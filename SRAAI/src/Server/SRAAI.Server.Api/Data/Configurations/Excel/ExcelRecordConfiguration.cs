namespace SRAAI.Server.Api.Data.Configurations.Excel;

public class ExcelRecordConfiguration : IEntityTypeConfiguration<Models.Excel.ExcelRecord>
{
	public void Configure(EntityTypeBuilder<Models.Excel.ExcelRecord> builder)
	{
		builder.ToTable("ExcelRecords");
		builder.HasKey(e => e.Id);
		builder.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
		builder.Property(e => e.DatasetName).IsRequired().HasMaxLength(128);
		builder.Property(e => e.BusinessKey).IsRequired().HasMaxLength(256);
		builder.Property(e => e.VersionNo).IsRequired();
		builder.Property(e => e.ChangeType).IsRequired();
		builder.Property(e => e.DataJson).HasColumnType("nvarchar(max)");
		builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

		builder.HasIndex(e => new { e.DatasetName, e.BusinessKey, e.VersionNo });
	}
}


