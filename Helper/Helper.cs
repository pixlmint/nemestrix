namespace Pixlmint.Nemestrix.Helper;

using System.Collections.Generic;
using System.Linq;

public static class DictionaryConverter
{
    public static Dictionary<string, object> ConvertToNestedJson(
        Dictionary<string, object> flatDictionary
    )
    {
        // Step 1: Build the initial nested structure
        var root = BuildNestedStructure(flatDictionary);

        // Step 2: Convert objects with numeric children to arrays
        ConvertNumericChildrenToArrays(root);

        return root;
    }

    private static Dictionary<string, object> BuildNestedStructure(
        Dictionary<string, object> flatDictionary
    )
    {
        var root = new Dictionary<string, object>();

        foreach (var kvp in flatDictionary)
        {
            string[] path = kvp.Key.Split('.');
            var current = root;

            // Navigate to the parent node
            for (int i = 0; i < path.Length - 1; i++)
            {
                if (!current.ContainsKey(path[i]))
                {
                    current[path[i]] = new Dictionary<string, object>();
                }

                current = (Dictionary<string, object>)current[path[i]];
            }

            // Add the leaf value
            current[path[path.Length - 1]] = kvp.Value;
        }

        return root;
    }

    /*private static Dictionary<string, object> ReflattenJson(
        Dictionary<string, object> nested,
        string currentParent
    )
    {
        var root = new Dictionary<string, object>();

        foreach (var kvp in nested)
        {
            if (kvp.Value is Dictionary<string, object> child)
            {
                if (child.Values.Count > 1)
                {
                    foreach (var childKvp in child.Values) { }
                }
                else
                {
                    string childKey = currentParent;
                    if (currentParent.Length > 0)
                    {
                        childKey += ".";
                    }
                    childKey += kvp.Key;
                    var newChild = ReflattenJson(child, childKey);
                    root[childKey] = newChild;
                }
            }
            else if (kvp.Value is List<object> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is Dictionary<string, object> listDict)
                    {
                        string childKey;
                        if (list.Count == 1)
                        {
                            childKey = currentParent + "." + i;
                        }
                        else
                        {
                            childKey = "" + i;
                        }
                        ReflattenJson(listDict, childKey);
                    }
                }
            }
            else
            {
                string childKey = currentParent;
                if (currentParent.Length > 0)
                {
                    childKey += ".";
                }
                childKey += kvp.Key;
                root[childKey] = kvp.Value;
            }
        }

        return root;
    }*/

    private static void ConvertNumericChildrenToArrays(Dictionary<string, object> token)
    {
        // First, recursively process all child objects
        foreach (var kvp in token)
        {
            if (kvp.Value is Dictionary<string, object> child)
            {
                ConvertNumericChildrenToArrays(child);
            }
        }

        // Now check for objects with all-numeric keys
        foreach (var kvp in token)
        {
            if (kvp.Value is Dictionary<string, object> childObj)
            {
                var childProperties = childObj.Keys.ToList();

                // Check if all children have numeric keys
                bool allNumeric =
                    childProperties.Count > 0 && childProperties.All(p => int.TryParse(p, out _));

                if (allNumeric)
                {
                    // Check if indices form a consecutive sequence starting from 0
                    var indices = childProperties
                        .Select(p => int.Parse(p))
                        .OrderBy(i => i)
                        .ToList();

                    bool isConsecutiveFromZero = true;
                    for (int i = 0; i < indices.Count; i++)
                    {
                        if (indices[i] != i)
                        {
                            isConsecutiveFromZero = false;
                            break;
                        }
                    }

                    if (isConsecutiveFromZero)
                    {
                        // Convert to array
                        var array = new List<object>();
                        for (int i = 0; i < indices.Count; i++)
                        {
                            array.Add(childObj[i.ToString()]);
                        }

                        // Replace object with array
                        token[kvp.Key] = array;
                    }
                }
            }
        }
    }
}
