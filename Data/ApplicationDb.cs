using Microsoft.EntityFrameworkCore;
using Pixlmint.Nemestrix.Model;

namespace Pixlmint.Nemestrix.Data;

public class ApplicationDb : DbContext
{
    public ApplicationDb(DbContextOptions<ApplicationDb> options)
        :base(options) {}

    public DbSet<Tree> Trees => Set<Tree>();
}

