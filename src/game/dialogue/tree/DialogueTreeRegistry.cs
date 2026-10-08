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
        Register(new ReconcileTree());
        Register(new ProposeToBuyTree());
        Register(new ProposeToSellTree());
        Register(new RequestJobTree());
        Register(new WakeUpTree());
        Register(new BegForCoinTree());
        Register(new ProvokeTree());
        Register(new ProposeToJoinTree());
        Register(new IntroduceMeTree());
        Register(new GatherKnowledgeTree());
        // The settled country's conversations (SettledTrees.cs).
        Register(new AskBlessingTree());
        Register(new ConfessTree());
        Register(new PetitionTree());
        Register(new TalkSoldieringTree());
        Register(new TalkOfFarPlacesTree());
        Register(new OfferBribeTree());
        Register(new AskTeachingTree());
        Register(new SingAlongTree());
        // A dense city's trades and streets (CityTrees.cs).
        Register(new CommissionWorkTree());
        Register(new AskRemedyTree());
        Register(new GiveAlmsTree());
        Register(new HearGossipTree());
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
