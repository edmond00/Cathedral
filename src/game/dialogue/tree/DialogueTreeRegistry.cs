using System;
using System.Collections.Generic;
using Cathedral.Game.Dialogue.Tree.Trees;

namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Singleton registry of all <see cref="DialogueTree"/> definitions.
/// Trees are stateless data; sessions are managed by the runtime controller.
/// </summary>
public class DialogueTreeRegistry
{
    private static DialogueTreeRegistry? _instance;
    public static DialogueTreeRegistry Instance => _instance ??= new DialogueTreeRegistry();

    private readonly Dictionary<string, DialogueTree> _trees = new();

    private DialogueTreeRegistry()
    {
        Register(new MeetStrangerTree());
        Register(new StrengthenRelationshipTree());
    }

    private void Register(DialogueTree tree) => _trees[tree.TreeId] = tree;

    /// <summary>Returns the tree with the given ID, or throws if not found.</summary>
    public DialogueTree Get(string treeId)
    {
        if (_trees.TryGetValue(treeId, out var tree)) return tree;
        throw new KeyNotFoundException($"DialogueTreeRegistry: no tree with id '{treeId}'");
    }

    /// <summary>Returns the tree with the given ID, or null.</summary>
    public DialogueTree? TryGet(string treeId)
        => _trees.TryGetValue(treeId, out var tree) ? tree : null;

    /// <summary>All registered trees.</summary>
    public IEnumerable<DialogueTree> All => _trees.Values;
}
