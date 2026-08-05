using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Respiral.Skills;

/// <summary>
/// Name - название навыка.
/// Description - описание навыка.
/// Progress - прогресс изучения навыка.
/// IconSize - размер иконки навыка.
/// Evaluable - может ли смотрящий оценить этот навык (его знание не хуже демонстрирующего); иначе уровень скрыт.
/// </summary>
[Serializable, NetSerializable]
public record struct SkillInfo(string Name, string Description, SpriteSpecifier Icon, float Progress, int IconSize, bool Evaluable = true);
