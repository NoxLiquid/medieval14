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
            var total = (int)system.Damageable.GetTotalDamage(damageable.AsNullable());

            var level = total / Step;

            if (level > _lastTriggerLevel)
            {
                _lastTriggerLevel = level;
                return true;
            }

            return false;
        }

        // Respiral: IThresholdTrigger gained IComparable/IEquatable upstream
        public int CompareTo(IThresholdTrigger? other)
        {
            if (other is RepeatDamageTrigger trigger)
                return Step.CompareTo(trigger.Step);

            return 0;
        }

        public bool Equals(IThresholdTrigger? other)
        {
            return other is RepeatDamageTrigger trigger && trigger.Step == Step;
        }
    }
}
