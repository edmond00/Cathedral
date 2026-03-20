using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cathedral.Fight;

/// <summary>
/// Central registry of all available fighting skills.
/// Auto-discovered via reflection from all non-abstract <see cref="FightingSkill"/> subclasses.
/// </summary>
public class FightingSkillRegistry
{
    private readonly Dictionary<string, FightingSkill> _byId = new();

    private static FightingSkillRegistry? _instance;
    public static FightingSkillRegistry Instance => _instance ??= new FightingSkillRegistry();

    private FightingSkillRegistry()
    {
        var baseType = typeof(FightingSkill);
        var skillTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t));

        foreach (var type in skillTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is FightingSkill skill)
                    _byId[skill.SkillId] = skill;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FightingSkillRegistry: failed to register {type.Name}: {ex.Message}");
            }
        }

        Console.WriteLine($"FightingSkillRegistry: {_byId.Count} skills registered.");
    }

    public IEnumerable<FightingSkill> GetAll() => _byId.Values;

    public FightingSkill? GetById(string id) => _byId.GetValueOrDefault(id);

    public IEnumerable<FightingSkill> GetByMediumOrgan(string organId) =>
        _byId.Values.Where(s => s.Medium.Type == MediumType.OrganMedium && s.Medium.OrganId == organId);

    public IEnumerable<FightingSkill> GetByModusMentis(string modId) =>
        _byId.Values.Where(s => s.RequiredModusMentisId == modId);

    public IEnumerable<FightingSkill> GetAttackSkills() =>
        _byId.Values.Where(s => s.EffectType == FightingSkillEffect.Attack);
}
