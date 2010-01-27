﻿using System;
using System.Collections.Generic;
using System.Linq;
using libtcodWrapper;
using Magecrawl.GameEngine.Actors;
using Magecrawl.GameEngine.Level;
using Magecrawl.GameEngine.Weapons;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine
{
    internal sealed class CombatEngine : IDisposable
    {
        public delegate void DamageDoneDelegate(int damage, Character target, bool targetKilled);

        // Cared and fed by CoreGameEngine, local copy for convenience
        private Player m_player;
        private Map m_map;
        private PhysicsEngine m_physicsEngine;

        private TCODRandom m_random;

        internal CombatEngine(PhysicsEngine engine, Player player, Map map)
        {
            m_player = player;
            m_map = map;
            m_physicsEngine = engine;
            m_random = new TCODRandom();
        }

        public void Dispose()
        {
            if (m_random != null)
                m_random.Dispose();
            m_random = null;
        }

        internal void NewMapPlayerInfo(Player player, Map map)
        {
            m_player = player;
            m_map = map;        
        }

        internal bool Attack(Character attacker, Point attackTarget)
        {
            if (!attacker.CurrentWeapon.PositionInTargetablePoints(attackTarget))
                throw new ArgumentException("CombatEngine attacking something current weapon can't attack with?");

            float effectiveStrength = attacker.CurrentWeapon.EffectiveStrengthAtPoint(attackTarget);
            int damageDone = (int)Math.Round(attacker.CurrentWeapon.Damage.Roll() * effectiveStrength);

            if (attacker.CurrentWeapon.IsRanged)
            {
                if (!attacker.CurrentWeapon.IsLoaded)
                    throw new ArgumentException("CombatEngine attacking something with current weapon unloaded?");

                AttackRanged(attacker, attackTarget, damageDone, attacker.CurrentWeapon,
                    (dmg, target, killed) => CoreGameEngine.Instance.SendTextOutput(CreateDamageString(damageDone, attacker, target)));
                ((WeaponBase)attacker.CurrentWeapon).IsLoaded = false;
            }
            else
            {
                Character attackedCharacter = FindTargetAtPosition(attackTarget);
                if (attackedCharacter != null)
                {
                    CoreGameEngine.Instance.SendTextOutput(CreateDamageString(damageDone, attacker, attackedCharacter));
                    DamageTarget(damageDone, attackedCharacter);
                }
            }

            return true;
        }

        internal bool AttackRanged(Character attacker, Point attackTarget, int damageDone, object attackingMethod, DamageDoneDelegate del)
        {
            if (attacker.Position == attackTarget)
                return false;

            List<Point> attackPath = m_physicsEngine.GenerateRangedAttackListOfPoints(m_map, attacker.Position, attackTarget);
            Character attackedCharacter = FindTargetAtPosition(attackTarget);

            CoreGameEngine.Instance.ShowRangedAttack(attackingMethod, attackPath, attackedCharacter != null);

            if (attackedCharacter != null)
                DamageTarget(damageDone, attackedCharacter, del);

            return true;
        }

        public Character FindTargetAtPosition(Point attackTarget)
        {
            Monster attackedMonster = (Monster)m_map.Monsters.SingleOrDefault(x => x.Position == attackTarget);
            if (attackedMonster != null)
                return attackedMonster;
            if (attackTarget == m_player.Position)
                return m_player;
            return null;
        }

        public void DamageTarget(int damage, Character target)
        {
            DamageTarget(damage, target, null);
        }

        public void DamageTarget(int damage, Character target, DamageDoneDelegate del)
        {
            // Sometimes bouncy spells and other things can hit a creature two or more times.
            // If the creature is dead and the map agrees, return early, since the poor sob is already dead and gone.
            if (target.CurrentHP <= 0 && !m_map.Monsters.Contains(target))
                return;

            target.CurrentHP -= damage;
            bool targetKilled = target.CurrentHP <= 0;
            if (targetKilled)
            {
                if (target is Monster)
                {
                    m_map.RemoveMonster(target as Monster);
                }
                else if (target is Player)
                {
                    CoreGameEngine.Instance.PlayerDied();
                }
            }
            if (del != null)
                del(damage, target, targetKilled);
        }

        private string CreateDamageString(int damage, Character attacker, Character defender)
        {
            // "Cheat" to see if attacker or defense is the player to make text output 
            // what is expected. The's should prepend monsters, not player. 
            // If we have 'Proper' named monsters, like say Kyle the Dragon, this will have to be updated.
            bool attackerIsPlayer = attacker is Player;
            bool defenderIsPlayer = defender is Player;
            bool attackKillsTarget = defender.CurrentHP <= damage;

            string verb = ((WeaponBase)attacker.CurrentWeapon).AttackVerb;

            if (damage == -1)
            {
                if (attackerIsPlayer)
                    return string.Format("{0} misses the {1}.", attacker.Name, defender.Name);
                else if (defenderIsPlayer)
                    return string.Format("The {0} misses {1}.", attacker.Name, m_player.Name);
                else
                    return string.Format("The {0} misses the {1}.", attacker.Name, defender.Name);
            }
            else if (damage == 0)
            {
                if (attackerIsPlayer)
                    return string.Format("{0} {1} and does no damage to the {2}.", attacker.Name, verb, defender.Name);
                else if (defenderIsPlayer)
                    return string.Format("The {0} {1} and does no damage to {2}.", attacker.Name, verb, defender.Name);
                else
                    return string.Format("The {0} {1} and does no damage to the {2}.", attacker.Name, verb, defender.Name);
            }
            else if (attackKillsTarget)
            {
                if (attackerIsPlayer)
                    return string.Format("{0} {1} and kills the {2} with {3} damage.", attacker.Name, verb, defender.Name, damage.ToString());
                else if (defenderIsPlayer)
                    return string.Format("The {0} {1} and kills {2} with {3} damage.", attacker.Name, verb, defender.Name, damage.ToString());
                else
                    return string.Format("The {0} {1} and kills the {3} with {3} damage.", attacker.Name, verb, defender.Name, damage.ToString());
            }
            else
            {
                if (attackerIsPlayer)
                    return string.Format("{0} {1} the {2} for {3} damage.", attacker.Name, verb, defender.Name, damage.ToString());
                else if (defenderIsPlayer)
                    return string.Format("The {0} {1} {2} for {3} damage.", attacker.Name, verb, defender.Name, damage.ToString());
                else
                    return string.Format("The {0} {1} the {2} for {3} damage.", attacker.Name, verb, defender.Name, damage.ToString());
            }
        }
    }
}
