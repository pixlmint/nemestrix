using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Pixlmint.Nemestrix.Auth;
using Pixlmint.Nemestrix.Data;
using Pixlmint.Nemestrix.Helper;
using Pixlmint.Nemestrix.Model;
using Pixlmint.Util;

if (ApiKeyGenerator.GenerateAndPrintKey(args))
    return;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDbContext<ApplicationDb>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DbConnection"))
);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

app.UseForwardedHeaders(
    new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    }
);

app.UseApiKeyAuthentication();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDb>();
    db.Database.Migrate();
}

async Task<IResult> HandleEdit(ApplicationDb db, HttpRequest request, bool AllowReplace)
{
    using var reader = new StreamReader(request.Body);
    object dynJson = JsonConvert.DeserializeObject(await reader.ReadToEndAsync())!;

    var nodes = Pixlmint.Nemestrix.Helper.JsonConverter.JsonToNodes(dynJson);

    var ret = new Dictionary<string, object>();

    foreach (var node in nodes)
    {
        var res = await db.Nodes.FirstOrDefaultAsync(n => n.Path.IsAncestorOf(node.Node!.Path));

        if (res != null)
        {
            if (AllowReplace)
            {
                db.Nodes.Remove(res);
            }
            else
            {
                return Results.BadRequest($"Path {node.Node!.Path.ToString()} already exists");
            }
        }
    }

    foreach (var node in nodes)
    {
        db.Nodes.Add(node.Node!);

        switch (node)
        {
            case StringLeafNode valueNode:
                db.StringLeafs.Add(valueNode);
                ret.Add(node.Node!.Path.ToString(), valueNode.Value!);
                break;
            case NumericLeafNode valueNode:
                db.NumericLeafs.Add(valueNode);
                ret.Add(node.Node!.Path.ToString(), valueNode.Value!);
                break;
            case BooleanLeafNode valueNode:
                db.BooleanLeafs.Add(valueNode);
                ret.Add(node.Node!.Path.ToString(), valueNode.Value!);
                break;
            default:
                return Results.InternalServerError($"Unsupported Leaf Type {node.GetType()}");
        }
    }

    await db.SaveChangesAsync();

    if (AllowReplace)
    {
        return Results.Ok(ret);
    }
    else
    {
        return Results.Created("/t", ret);
    }
}

app.MapPost(
    "/t",
    async (ApplicationDb db, HttpRequest request) =>
    {
        return await HandleEdit(db, request, false);
    }
);

app.MapPut(
    "/t",
    async (ApplicationDb db, HttpRequest request) =>
    {
        return await HandleEdit(db, request, true);
    }
);

app.MapDelete(
    "/t/{Path}",
    async (string Path, ApplicationDb db, HttpRequest request) =>
    {
        var nodes = await db.Nodes.Where(t => t.Path.MatchesLQuery(Path)).ToArrayAsync();

        foreach (var node in nodes)
        {
            db.Nodes.Remove(node);
        }

        await db.SaveChangesAsync();

        return Results.Ok($"Deleted {nodes.Length} nodes");
    }
);

async Task<Dictionary<string, object>> SearchNodes(string Search, ApplicationDb db)
{
    var nodes = await db
        .Nodes.Include(t => t.Leaf)
        .Where(t => t.Path.MatchesLQuery(Search))
        .OrderBy(t => t.Path)
        .ToArrayAsync();

    var ret = new Dictionary<string, object>();

    foreach (var treeNode in nodes)
    {
        if (treeNode.Leaf is StringLeafNode stringNode)
        {
            ret.Add(treeNode.Path.ToString(), stringNode.Value!);
        }
        else if (treeNode.Leaf is NumericLeafNode numericNode)
        {
            ret.Add(treeNode.Path.ToString(), numericNode.Value);
        }
        else
        {
            ret.Add(treeNode.Path.ToString(), treeNode.Leaf!);
        }
    }

    return DictionaryConverter.ConvertToNestedJson(ret);
}

app.MapGet(
    "/t",
    async (string node, ApplicationDb db) =>
    {
        return Results.Ok(await SearchNodes(node, db));
    }
);

app.MapGet(
    "/t/{Path}",
    async (string Path, ApplicationDb db) =>
    {
        var node = await db
            .Nodes.Include(t => t.Leaf)
            .FirstOrDefaultAsync(t => t.Path.MatchesLQuery(Path));

        if (node != null)
        {
            return node.Leaf switch
            {
                IHasValue<string> nodeValue => Results.Ok(nodeValue.Value),
                IHasValue<Double> nodeValue => Results.Ok(nodeValue.Value),
                IHasValue<bool> nodeValue => Results.Ok(nodeValue.Value),
                _ => Results.InternalServerError("Unsupported Leaf Type"),
            };
        }

        var SearchResults = await SearchNodes(Path + ".*", db);

        if (SearchResults.Values.Count > 0)
        {
            return Results.Ok(SearchResults);
        }

        return Results.NotFound();
    }
);

app.Run();
