using Microsoft.EntityFrameworkCore;
using Pixlmint.Nemestrix.Model;

namespace Pixlmint.Nemestrix.Data;

public class ApplicationDb : DbContext
{
    public ApplicationDb(DbContextOptions<ApplicationDb> options)
        : base(options) { }

    public DbSet<TreeNode> Nodes => Set<TreeNode>();
    public DbSet<NumericLeafNode> NumericLeafs => Set<NumericLeafNode>();
    public DbSet<BooleanLeafNode> BooleanLeafs => Set<BooleanLeafNode>();
    public DbSet<StringLeafNode> StringLeafs => Set<StringLeafNode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeafNode>().ToTable("Leafs");

        modelBuilder
            .Entity<TreeNode>()
            .HasOne(t => t.Leaf)
            .WithOne(l => l.Node)
            .HasForeignKey<TreeNode>(t => t.LeafId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<NumericLeafNode>()
            .Property(entity => entity.Value)
            .HasColumnName("NumericValue");
        modelBuilder
            .Entity<BooleanLeafNode>()
            .Property(entity => entity.Value)
            .HasColumnName("BooleanValue");
        modelBuilder
            .Entity<StringLeafNode>()
            .Property(entity => entity.Value)
            .HasColumnName("String");
    }
}
