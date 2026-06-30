using Content.Shared._Respiral.Magic.Spell;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Respiral.Magic.Spell;

/// <summary>
/// Форма «шар»: обрабатывает <see cref="ProjectileSpellEvent"/>
/// </summary>
public sealed partial class ProjectileSpellSystem : EntitySystem
{
    [Dependency] private SpellSystem _spell = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileSpellEvent>(OnProjectileSpell);
    }

    private void OnProjectileSpell(ProjectileSpellEvent args)
    {
        if (args.Handled)
            return;

        if (!_spell.TryCast(args.Action.Owner, args.Performer))
            return;

        args.Handled = true;

        var fromMap = _transform.ToMapCoordinates(Transform(args.Performer).Coordinates);
        var projectile = Spawn(args.Prototype, fromMap);

        var direction = _transform.ToMapCoordinates(args.Target).Position - fromMap.Position;
        var userVelocity = _physics.GetMapLinearVelocity(args.Performer);
        _gun.ShootProjectile(projectile, direction, userVelocity, args.Performer, args.Performer);
    }
}
