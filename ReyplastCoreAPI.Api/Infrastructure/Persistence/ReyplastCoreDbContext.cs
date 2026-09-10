using Microsoft.EntityFrameworkCore;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Domain.Common;
using ReyplastCoreAPI.Domain.Orders.Entities;

namespace ReyplastCoreAPI.Infrastructure.Persistence;

public class ReyplastCoreDbContext : DbContext
{
    public ReyplastCoreDbContext(DbContextOptions<ReyplastCoreDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Color> Colors => Set<Color>();
    public DbSet<SalesOrder> Orders => Set<SalesOrder>();
    public DbSet<SalesOrderDetail> OrderDetails => Set<SalesOrderDetail>();
    public DbSet<OrderSituation> OrderSituations => Set<OrderSituation>();
    /// <summary>Historial común de sincronizaciones, identificado por sistema de origen.</summary>
    public DbSet<SyncExecution> SyncExecutions => Set<SyncExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Id).ValueGeneratedNever();
            entity.Property(product => product.Type).HasMaxLength(20).IsRequired();
            entity.Property(product => product.GenericCode).HasMaxLength(50).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(500);
            entity.Property(product => product.SourceSystem).HasMaxLength(20).IsRequired();
            entity.Property(product => product.CreatedBy).HasMaxLength(100);
            entity.Property(product => product.ModifiedBy).HasMaxLength(100);

            entity.HasIndex(product => product.GenericCode)
                .IsUnique()
                .HasDatabaseName("UX_Products_GenericCode");
            entity.HasIndex(product => product.SyncExecutionId)
                .HasDatabaseName("IX_Products_SyncExecutionId");
            entity.HasIndex(product => new { product.SourceSystem, product.Type, product.IsActive })
                .HasDatabaseName("IX_Products_SourceSystem_Type_IsActive");
            entity.HasOne<SyncExecution>()
                .WithMany()
                .HasForeignKey(product => product.SyncExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Color>(entity =>
        {
            entity.ToTable("Colors");
            entity.HasKey(color => color.Id);
            entity.Property(color => color.Id).ValueGeneratedNever();
            entity.Property(color => color.Code).HasMaxLength(4).IsRequired();
            entity.Property(color => color.ColorDescription).HasMaxLength(300);
            entity.Property(color => color.CreatedBy).HasMaxLength(100);
            entity.Property(color => color.ModifiedBy).HasMaxLength(100);

            entity.HasOne(color => color.Product)
                .WithMany(product => product.Colors)
                .HasForeignKey(color => color.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(color => color.ProductId)
                .HasDatabaseName("IX_Colors_ProductId");
            entity.HasIndex(color => new { color.ProductId, color.Code })
                .IsUnique()
                .HasDatabaseName("UX_Colors_ProductId_Code");
            entity.HasIndex(color => color.SyncExecutionId)
                .HasDatabaseName("IX_Colors_SyncExecutionId");
            entity.HasOne<SyncExecution>()
                .WithMany()
                .HasForeignKey(color => color.SyncExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SyncExecution>(entity =>
        {
            entity.ToTable("SyncExecutions");
            entity.HasKey(execution => execution.Id);
            entity.Property(execution => execution.Id).ValueGeneratedNever();
            entity.Property(execution => execution.SourceSystem).HasMaxLength(20).IsRequired();
            entity.Property(execution => execution.ResourceType).HasMaxLength(20).IsRequired();
            entity.Property(execution => execution.Status).HasMaxLength(20).IsRequired();
            entity.Property(execution => execution.LastCursor).HasMaxLength(100);
            entity.Property(execution => execution.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(execution => new { execution.SourceSystem, execution.ResourceType, execution.Status, execution.StartedAt })
                .HasDatabaseName("IX_SyncExecutions_Source_Resource_Status_StartedAt");
        });

        modelBuilder.Entity<OrderSituation>(entity =>
        {
            entity.ToTable("OrderSituations");
            entity.HasKey(situation => new { situation.SourceSystem, situation.Code });
            entity.Property(situation => situation.SourceSystem).HasMaxLength(20).ValueGeneratedNever();
            entity.Property(situation => situation.Code).HasMaxLength(20).ValueGeneratedNever();
            entity.Property(situation => situation.Description).HasMaxLength(100).IsRequired();
            entity.HasData(
                new OrderSituation { SourceSystem = SourceSystems.Osis, Code = "0", Description = "PEDIDO ANULADO" },
                new OrderSituation { SourceSystem = SourceSystems.Osis, Code = "1", Description = "PEDIDO POR CALIFICAR" },
                new OrderSituation { SourceSystem = SourceSystems.Osis, Code = "2", Description = "PEDIDO APROBADO" },
                new OrderSituation { SourceSystem = SourceSystems.Osis, Code = "3", Description = "PEDIDO ATENDIDO" },
                new OrderSituation { SourceSystem = SourceSystems.Osis, Code = "4", Description = "PEDIDO RECHAZADO POR CTAS. POR COBRAR" },
                new OrderSituation { SourceSystem = SourceSystems.Osis, Code = "5", Description = "PEDIDO RECHAZADO POR GERENCIA" },
                new OrderSituation { SourceSystem = SourceSystems.Osis, Code = "6", Description = "PEDIDO DESAPROBADO" },
                new OrderSituation { SourceSystem = SourceSystems.Osis, Code = "7", Description = "PEDIDO RETENIDO" },
                new OrderSituation { SourceSystem = SourceSystems.Sap, Code = "O", Description = "ABIERTO" },
                new OrderSituation { SourceSystem = SourceSystems.Sap, Code = "C", Description = "CERRADO" });
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Id).ValueGeneratedNever();
            entity.Property(order => order.SourceDocumentId).HasMaxLength(100);
            entity.Property(order => order.BranchCode).HasMaxLength(2);
            entity.Property(order => order.DocNum).HasMaxLength(20).IsRequired();
            entity.Property(order => order.CardCode).HasMaxLength(20).IsRequired();
            entity.Property(order => order.CardName).HasMaxLength(200).IsRequired();
            entity.Property(order => order.SituationCode).HasMaxLength(20).IsRequired();
            entity.Property(order => order.DocTotal).HasPrecision(18, 4);
            entity.Property(order => order.ChannelCode).HasMaxLength(10);
            entity.Property(order => order.ChannelName).HasMaxLength(100);
            entity.Property(order => order.SourceSystem).HasMaxLength(20).IsRequired();
            entity.Ignore(order => order.TotalItems);
            entity.Property(order => order.CreatedBy).HasMaxLength(100);
            entity.Property(order => order.ModifiedBy).HasMaxLength(100);
            entity.HasIndex(order => new { order.SourceSystem, order.BranchCode, order.DocNum })
                .IsUnique().HasDatabaseName("UX_Orders_Source_Branch_DocNum");
            entity.HasIndex(order => new { order.SourceSystem, order.SourceDocumentId })
                .IsUnique().HasFilter("[SourceDocumentId] IS NOT NULL")
                .HasDatabaseName("UX_Orders_Source_SourceDocumentId");
            entity.HasIndex(order => new { order.DocDate, order.SituationCode })
                .HasDatabaseName("IX_Orders_DocDate_SituationCode");
            entity.HasIndex(order => order.ChannelCode).HasDatabaseName("IX_Orders_ChannelCode");
            entity.HasOne<SyncExecution>().WithMany().HasForeignKey(order => order.SyncExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(order => order.Situation).WithMany(situation => situation.Orders)
                .HasForeignKey(order => new { order.SourceSystem, order.SituationCode })
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesOrderDetail>(entity =>
        {
            entity.ToTable("OrderDetails");
            entity.HasKey(detail => detail.Id);
            entity.Property(detail => detail.Id).ValueGeneratedNever();
            entity.Property(detail => detail.SourceDetailId).HasMaxLength(100).IsRequired();
            entity.Property(detail => detail.LineNumber).HasMaxLength(10);
            entity.Property(detail => detail.ItemCode).HasMaxLength(50).IsRequired();
            entity.Property(detail => detail.Description).HasMaxLength(500);
            entity.Property(detail => detail.Quantity).HasPrecision(18, 4);
            entity.Property(detail => detail.Price).HasPrecision(18, 4);
            entity.Property(detail => detail.LineTotal).HasPrecision(18, 4);
            entity.Property(detail => detail.CreatedBy).HasMaxLength(100);
            entity.Property(detail => detail.ModifiedBy).HasMaxLength(100);
            entity.HasOne(detail => detail.Order).WithMany(order => order.Details)
                .HasForeignKey(detail => detail.OrderId).OnDelete(DeleteBehavior.Cascade);
            // La identidad técnica depende del origen: ItemCode en OSIS y LineNum/ordinal en SAP.
            entity.HasIndex(detail => new { detail.OrderId, detail.SourceDetailId })
                .IsUnique().HasDatabaseName("UX_OrderDetails_Order_SourceDetailId");
            entity.HasIndex(detail => detail.SyncExecutionId).HasDatabaseName("IX_OrderDetails_SyncExecutionId");
            entity.HasOne<SyncExecution>().WithMany().HasForeignKey(detail => detail.SyncExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow.AddHours(-5);
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = now;
            if (entry.State == EntityState.Modified) entry.Entity.ModifiedAt = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
