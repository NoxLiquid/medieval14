using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Respiral.Skill.Prototypes;

[Prototype]
public sealed partial class SkillPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string Description = string.Empty;

    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Texture(new("/Textures/_Respiral/Actions/submission.png"));

    /// <summary>
    ///     Размер иконки навыка
    /// </summary>
    [DataField]
    public int IconSize = 16;

    /// <summary>
    ///     Требуемые навыки для изучения этого навыка
    /// </summary>
    [DataField]
    public List<ProtoId<SkillPrototype>>? RequiredSkills;

    /// <summary>
    ///     Количество баллов требуемое для изучения этого навыка
    /// </summary>
    [DataField]
    public int PointsNeeded = 1;

    /// <summary>
    ///     Компоненты, выдаваемые владельцу навыка по порогам прогресса: ключ — порог (0..1),
    ///     значение — набор компонентов. Когда прогресс навыка достигает порога, компоненты выдаются;
    ///     если опускается ниже — снимаются. Позволяет открывать разные компоненты на разных уровнях
    ///     освоения. Проверка выполняется при инициализации <c>SkillComponent</c> и при изменении прогресса.
    /// </summary>
    [DataField]
    public Dictionary<float, ComponentRegistry>? GrantComponents;
}
