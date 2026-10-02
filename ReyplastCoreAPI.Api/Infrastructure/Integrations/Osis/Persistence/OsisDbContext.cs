using Microsoft.EntityFrameworkCore;

namespace ReyplastCoreAPI.Infrastructure.Integrations.Osis.Persistence;

/// <summary>Contexto de solo lectura conectado directamente a la base XIMESA.</summary>
internal sealed class OsisDbContext : DbContext
{
    public OsisDbContext(DbContextOptions<OsisDbContext> options) : base(options) { }

    public DbSet<OsisProductEntity> Products => Set<OsisProductEntity>();
    public DbSet<OsisProductColorEntity> ProductColors => Set<OsisProductColorEntity>();
    public DbSet<OsisColorEntity> Colors => Set<OsisColorEntity>();
    public DbSet<OsisOrderEntity> Orders => Set<OsisOrderEntity>();
    public DbSet<OsisOrderDetailEntity> OrderDetails => Set<OsisOrderDetailEntity>();
    public DbSet<OsisAuxiliaryEntity> Auxiliaries => Set<OsisAuxiliaryEntity>();
    public DbSet<OsisPriceListEntity> PriceLists => Set<OsisPriceListEntity>();
    public DbSet<OsisPaymentConditionEntity> PaymentConditions => Set<OsisPaymentConditionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OsisProductEntity>(entity =>
        {
            entity.ToTable("PRODUCTOS_PRD", "dbo");
            entity.HasKey(x => new { x.CompanyCode, x.ProductCode });
            entity.Property(x => x.CompanyCode).HasColumnName("cia_codcia");
            entity.Property(x => x.ProductCode).HasColumnName("prd_codprd");
            entity.Property(x => x.ProductDescription).HasColumnName("prd_desesp");
            entity.Property(x => x.CatalogTypeCode).HasColumnName("tin_codtin");
            entity.Property(x => x.ProductTypeCode).HasColumnName("prd_indset");
            entity.Property(x => x.ProductStatusCode).HasColumnName("prd_indest");
            entity.Property(x => x.IsUnitary).HasColumnName("prd_prduni");
            entity.Property(x => x.CreatedAt).HasColumnName("prd_feccre");
        });

        modelBuilder.Entity<OsisProductColorEntity>(entity =>
        {
            entity.ToTable("PRODUCTOS_COLORES_PCO", "dbo");
            entity.HasKey(x => new { x.CompanyCode, x.ProductCode, x.ColorCode });
            entity.Property(x => x.CompanyCode).HasColumnName("cia_codcia");
            entity.Property(x => x.ProductCode).HasColumnName("prd_codprd");
            entity.Property(x => x.ColorCode).HasColumnName("col_codcol");
            entity.Property(x => x.StatusCode).HasColumnName("pco_indest");
            entity.Property(x => x.SaleCode).HasColumnName("pco_venta");
        });

        modelBuilder.Entity<OsisColorEntity>(entity =>
        {
            entity.ToTable("COLORES_COL", "dbo");
            entity.HasKey(x => x.ColorCode);
            entity.Property(x => x.ColorCode).HasColumnName("col_codcol");
            entity.Property(x => x.ColorDescription).HasColumnName("col_desesp");
            entity.Property(x => x.StatusCode).HasColumnName("col_indest");
        });

        modelBuilder.Entity<OsisOrderEntity>(entity =>
        {
            entity.ToTable("PEDIDOS_VENTA_PPC", "dbo");
            entity.HasKey(x => new { x.CompanyCode, x.BranchCode, x.DocNum });
            entity.Property(x => x.CompanyCode).HasColumnName("CIA_CODCIA");
            entity.Property(x => x.BranchCode).HasColumnName("SUC_CODSUC");
            entity.Property(x => x.DocNum).HasColumnName("PPC_NUMPPC");
            entity.Property(x => x.CreatedAt).HasColumnName("PPC_FECCRE");
            entity.Property(x => x.CardCode).HasColumnName("AUX_CODAUX");
            entity.Property(x => x.StatusCode).HasColumnName("PPC_SITPPC");
            entity.Property(x => x.DocTotal).HasColumnName("PPC_IMPTOT").HasPrecision(16, 2);
            entity.Property(x => x.ActiveCode).HasColumnName("PPC_INDEST");
            entity.Property(x => x.PaymentConditionCode).HasColumnName("CPA_CODCPA");
            entity.Property(x => x.TlpCode).HasColumnName("TLP_CODTLP");
            entity.Property(x => x.ModifiedAt).HasColumnName("PPC_FECACT");
            entity.Property(x => x.ApprovalDate).HasColumnName("PPC_FECAPR");
        });

        modelBuilder.Entity<OsisOrderDetailEntity>(entity =>
        {
            entity.ToTable("PEDIDOS_VENTA_PPD", "dbo");
            entity.HasKey(x => new { x.CompanyCode, x.BranchCode, x.DocNum, x.ItemCode });
            entity.Property(x => x.CompanyCode).HasColumnName("CIA_CODCIA");
            entity.Property(x => x.BranchCode).HasColumnName("SUC_CODSUC");
            entity.Property(x => x.DocNum).HasColumnName("PPC_NUMPPC");
            entity.Property(x => x.ItemCode).HasColumnName("PRD_CODPRD");
            entity.Property(x => x.LineNumber).HasColumnName("PPD_CORPPD");
            entity.Property(x => x.Quantity).HasColumnName("PPD_CANSOL").HasPrecision(10, 2);
            entity.Property(x => x.SuspendedQuantity).HasColumnName("PPD_CANSUS").HasPrecision(10, 2);
            entity.Property(x => x.Price).HasColumnName("PPD_PREUNI").HasPrecision(10, 4);
            // LineTotal sigue la semántica comercial de SAP: cantidad por precio sin IGV.
            entity.Property(x => x.LineTotal).HasColumnName("PPD_IMPBRU").HasPrecision(10, 2);
            entity.Property(x => x.ActiveCode).HasColumnName("PPD_INDEST");
            entity.Property(x => x.ModifiedAt).HasColumnName("PPD_fECACT");
        });

        modelBuilder.Entity<OsisAuxiliaryEntity>(entity =>
        {
            entity.ToTable("AUXILIARES_AUX", "dbo");
            entity.HasKey(x => new { x.CompanyCode, x.CardCode });
            entity.Property(x => x.CompanyCode).HasColumnName("CIA_CODCIA");
            entity.Property(x => x.CardCode).HasColumnName("AUX_CODAUX");
            entity.Property(x => x.CardName).HasColumnName("AUX_NOMAUX");
        });

        modelBuilder.Entity<OsisPriceListEntity>(entity =>
        {
            entity.ToTable("tipo_lista_precio_tlp", "dbo");
            entity.HasKey(x => new { x.CompanyCode, x.Code });
            entity.Property(x => x.CompanyCode).HasColumnName("cia_codcia");
            entity.Property(x => x.Code).HasColumnName("tlp_codtlp");
            entity.Property(x => x.Name).HasColumnName("tlp_destlp");
        });

        modelBuilder.Entity<OsisPaymentConditionEntity>(entity =>
        {
            entity.ToTable("cond_pago_cpa", "dbo");
            entity.HasKey(x => x.Code);
            entity.Property(x => x.Code).HasColumnName("cpa_codcpa");
            entity.Property(x => x.Name).HasColumnName("cpa_deslar");
        });
    }
}
