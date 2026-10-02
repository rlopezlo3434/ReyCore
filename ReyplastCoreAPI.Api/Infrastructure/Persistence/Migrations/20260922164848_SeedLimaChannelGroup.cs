using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    public partial class SeedLimaChannelGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @groupId uniqueidentifier;
SELECT @groupId = Id FROM dbo.ChannelGroups WHERE Name = N'LIMA';
IF @groupId IS NULL
BEGIN
    SET @groupId = 'A0862996-7E7D-4917-A8C9-D2E9D1B8D102';
    INSERT INTO dbo.ChannelGroups (Id, Name, IsActive, CreatedAt, CreatedBy, ModifiedBy)
    VALUES (@groupId, N'LIMA', 1, GETDATE(), N'Migration', N'Migration');
END;
UPDATE dbo.Channels SET ChannelGroupId = @groupId, ModifiedAt = GETDATE(), ModifiedBy = N'Migration'
WHERE SourceSystem = N'OSIS' AND Code IN (N'11', N'12', N'03')
  AND (ChannelGroupId IS NULL OR ChannelGroupId <> @groupId);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se revierte automáticamente una agrupación de negocio que podría haberse editado
            // después del despliegue; eliminarla aquí borraría esa decisión manual.
        }
    }
}
