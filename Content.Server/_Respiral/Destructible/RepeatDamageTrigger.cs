using Content.Shared.Damage.Components;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;

namespace Content.Server._Respiral.Destructible
{
    [Serializable]
    [DataDefinition]
    public sealed partial class RepeatDamageTrigger : IThresholdTrigger
    {

        [DataField("step", required: true)]
        public int Step { get; set; }

        [ViewVariables]
        private int _lastTriggerLevel = 0;

        public bool Reached(Entity<DamageableComponent> damageable, SharedDestructibleSystem system)
        {
            var total = (int)damageable.Comp.TotalDamage;

            var level = total / Step;

            if (level > _lastTriggerLevel)
            {
                _lastTriggerLevel = level;
                return true;
            }

            return false;
        }
    }
}
