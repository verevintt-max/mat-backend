using Microsoft.EntityFrameworkCore;
using WorkshopApi.Models;

namespace WorkshopApi.Data;

public class WorkshopDbContext : DbContext
{
    public WorkshopDbContext(DbContextOptions<WorkshopDbContext> options) : base(options)
    {
    }

    // Auth & Organizations
    public DbSet<User> Users { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }
    public DbSet<Invitation> Invitations { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    // Business entities
    public DbSet<Material> Materials { get; set; }
    public DbSet<MaterialReceipt> MaterialReceipts { get; set; }
    public DbSet<MaterialWriteOff> MaterialWriteOffs { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<RecipeItem> RecipeItems { get; set; }
    public DbSet<Production> Productions { get; set; }
    public DbSet<FinishedProduct> FinishedProducts { get; set; }
    public DbSet<OperationHistory> OperationHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==================== AUTH & ORGANIZATIONS ====================

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();

            entity.HasOne(e => e.CurrentOrganization)
                .WithMany()
                .HasForeignKey(e => e.CurrentOrganizationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Organization
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasIndex(e => e.JoinCode).IsUnique();

            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrganizationMember
        modelBuilder.Entity<OrganizationMember>(entity =>
        {
            entity.HasIndex(e => new { e.OrganizationId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Members)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.OrganizationMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Invitation
        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.OrganizationId, e.Email });

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Invitations)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvitedBy)
                .WithMany(u => u.SentInvitations)
                .HasForeignKey(e => e.InvitedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==================== BUSINESS ENTITIES ====================

        // Material
        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasIndex(e => new { e.OrganizationId, e.Name, e.Color }).IsUnique();
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsArchived);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Materials)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MaterialReceipt
        modelBuilder.Entity<MaterialReceipt>(entity =>
        {
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.MaterialId);
            entity.HasIndex(e => e.ReceiptDate);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.MaterialReceipts)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Material)
                .WithMany(m => m.Receipts)
                .HasForeignKey(e => e.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MaterialWriteOff
        modelBuilder.Entity<MaterialWriteOff>(entity =>
        {
            entity.HasIndex(e => e.ProductionId);
            entity.HasIndex(e => e.MaterialReceiptId);

            entity.HasOne(e => e.Production)
                .WithMany(p => p.MaterialWriteOffs)
                .HasForeignKey(e => e.ProductionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MaterialReceipt)
                .WithMany(r => r.WriteOffs)
                .HasForeignKey(e => e.MaterialReceiptId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsArchived);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Products)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RecipeItem
        modelBuilder.Entity<RecipeItem>(entity =>
        {
            entity.HasIndex(e => new { e.ProductId, e.MaterialId }).IsUnique();

            entity.HasOne(e => e.Product)
                .WithMany(p => p.RecipeItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Material)
                .WithMany(m => m.RecipeItems)
                .HasForeignKey(e => e.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Production
        modelBuilder.Entity<Production>(entity =>
        {
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => new { e.OrganizationId, e.BatchNumber }).IsUnique();
            entity.HasIndex(e => e.ProductionDate);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Productions)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.Productions)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // FinishedProduct
        modelBuilder.Entity<FinishedProduct>(entity =>
        {
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.ProductionId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.FinishedProducts)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Production)
                .WithMany(p => p.FinishedProducts)
                .HasForeignKey(e => e.ProductionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OperationHistory
        modelBuilder.Entity<OperationHistory>(entity =>
        {
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.OperationType);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.OperationHistory)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
