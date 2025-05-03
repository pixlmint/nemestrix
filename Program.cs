using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pixlmint.Nemestrix.Data;
using Pixlmint.Nemestrix.Helper;
using Pixlmint.Nemestrix.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDb>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DbConnection"))
);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet(
    "/trees",
    async (ApplicationDb db) =>
    {
        return await db.Nodes.ToListAsync();
    }
);

app.MapGet(
    "/tree",
    async (ApplicationDb db, string node) =>
    {
        var tNode = await db.Nodes.FirstOrDefaultAsync(n => n.Path.MatchesLQuery(node));

        if (tNode == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new { Id = tNode.Id, Path = tNode.Path.ToString() });
    }
);

app.MapGet(
    "/trees/{id}",
    async (int Id, ApplicationDb db) =>
    {
        var Tree = await db.Nodes.FirstOrDefaultAsync(t => t.Id == Id);
        if (Tree == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new { Path = Tree.Path.ToString() });
    }
);

app.MapPost(
    "/trees",
    async (TreeNodeDto treeDto, ApplicationDb db) =>
    {
        var tree = treeDto.ToTreeNode();
        db.Nodes.Add(tree);
        await db.SaveChangesAsync();

        return Results.Ok(new { Id = tree.Id, Path = tree.Path.ToString() });
    }
);

static void JsonToNodesRecursive(
    Newtonsoft.Json.Linq.JToken json,
    string? parent,
    List<LeafNode> nodes,
    int depth = 0
)
{
    if (
        json is Newtonsoft.Json.Linq.JObject
        || (json is Newtonsoft.Json.Linq.JProperty && json.HasValues)
    )
    {
        foreach (var child in json.Children())
        {
            JsonToNodesRecursive(child, parent, nodes, depth++);
        }
    }
    else if (json is Newtonsoft.Json.Linq.JArray)
    {
        var arr = ((Newtonsoft.Json.Linq.JArray)json);
        foreach (var child in arr)
        {
            JsonToNodesRecursive(child, parent, nodes, depth++);
        }
    }
    else if (json is Newtonsoft.Json.Linq.JValue)
    {
        var path = json.Path.Replace('[', '.').Replace("]", "");
        LeafNode leaf;
        TreeNode node = new TreeNode { Path = new LTree(path) };
        switch (json.Type)
        {
            case JTokenType.Float:
                leaf = new NumericLeafNode { Node = node, Value = json.Value<float>() };
                break;
            case JTokenType.Integer:
                leaf = new NumericLeafNode { Node = node, Value = json.Value<int>() };
                break;
            default:
                leaf = new StringLeafNode { Node = node, Value = json.Value<string>() };
                break;
        }

        nodes.Add(leaf);
    }
}

static List<LeafNode> JsonToNodes(dynamic json)
{
    var nodes = new List<LeafNode>();

    JsonToNodesRecursive(json, null, nodes);

    return nodes;
}

async Task<IResult> HandleEdit(ApplicationDb db, HttpRequest request, bool AllowReplace)
{
    using var reader = new StreamReader(request.Body);
    dynamic dynJson = JsonConvert.DeserializeObject(await reader.ReadToEndAsync())!;

    var nodes = JsonToNodes(dynJson);

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

    if (AllowReplace) {
        return Results.Ok(ret);
    } else {
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

