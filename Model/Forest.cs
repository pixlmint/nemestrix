using Microsoft.EntityFrameworkCore;

namespace Pixlmint.Nemestrix.Model;

public interface IHasValue<T>
{
    T Value { get; set; }
}

public class TreeNode
{
    public int Id { get; set; }
    public LTree Path { get; set; }
    public LeafNode? Leaf { get; set; }
    public int LeafId { get; set; }
}

public abstract class LeafNode
{
    public int Id { get; set; }
    public TreeNode? Node { get; set; }
}

public class NumericLeafNode : LeafNode, IHasValue<Double>
{
    public Double Value { get; set; }
}

public class BooleanLeafNode : LeafNode, IHasValue<bool>
{
    public bool Value { get; set; }
}

public class StringLeafNode : LeafNode, IHasValue<string?>
{
    public string? Value { get; set; }
}

public class TreeNodeDto
{
    public int? Id { get; set; }
    public string? Label { get; set; }

    public TreeNode ToTreeNode()
    {
        if (Id == null)
        {
            return new TreeNode { Path = new LTree(Label!) };
        }
        else
        {
            return new TreeNode { Id = (int)Id, Path = new LTree(Label!) };
        }
    }
}

public class TreeDto
{
    public string? Path { get; set; }
}
