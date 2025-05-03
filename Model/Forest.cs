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
