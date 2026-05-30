// using Content.Shared.Damage;
// using Robust.Shared.GameStates;
// using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

// namespace Content.Server._Respiral.Hanging.Components;

// [RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
// public sealed partial class HangingComponent : Component
// {
//     [DataField, AutoNetworkedField]
//     public EntityUid? HangingEntity;

//     [DataField, AutoNetworkedField]
//     public bool IsActive;

//     [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
//     public TimeSpan NextDamageTime;

//     [DataField]
//     public float DamageInterval = 1f;

//     [DataField(required: true)]
//     public DamageSpecifier Damage = default!;
// }