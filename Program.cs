using Microsoft.EntityFrameworkCore;
using Pixlmint.Nemestrix.Data;
using Pixlmint.Nemestrix.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDb>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DbConnection"))
);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/trees", async (ApplicationDb db) => await db.Trees.ToListAsync());

app.MapPost("/trees", async (Tree tree, ApplicationDb db) => {
        db.Trees.Add(tree);
        await db.SaveChangesAsync();

        return Results.Ok(tree);
        });

app.Run();
