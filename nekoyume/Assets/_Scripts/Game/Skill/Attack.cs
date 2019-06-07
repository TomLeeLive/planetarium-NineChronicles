using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AttackBase: SkillBase
    {
        private readonly Data.Table.Elemental.ElementalType _elemental;

        protected AttackBase(float chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental) : base(chance, effect)
        {
            this.chance = chance;
            _elemental = elemental;
        }

        protected List<Model.Skill.SkillInfo> ProcessDamage(IEnumerable<CharacterBase> targets)
        {
            var infos = new List<Model.Skill.SkillInfo>();
            var targetList = targets.ToArray();
            var elemental = Elemental.Create(_elemental);
            var multiplier = GetMultiplier(Effect.hitCount, Effect.multiplier);
            for (var i = 0; i < Effect.hitCount; i++)
            {
                foreach (var target in targetList)
                {
                    var multiply = multiplier[i];
                    var critical = caster.IsCritical();
                    var dmg = elemental.CalculateDmg(caster.atk, target.defElement);
                    // https://gamedev.stackexchange.com/questions/129319/rpg-formula-attack-and-defense
                    dmg = Math.Max((dmg * dmg) / (dmg + target.def), 1);
                    dmg = Convert.ToInt32(dmg * multiply);
                    if (critical)
                    {
                        dmg = Convert.ToInt32(dmg * CharacterBase.CriticalMultiplier);
                    }

                    target.OnDamage(dmg);

                    infos.Add(new Model.Skill.SkillInfo((CharacterBase) target.Clone(), dmg, critical, Effect.category,
                        _elemental));
                }
            }

            return infos;
        }

        private float[] GetMultiplier(int count, float total)
        {
            if (count == 1) return new[] {total};
            var multiplier = new List<float>();
            var avg = total / count;
            var last = avg * 1.3f;
            var remain = count - 1;
            var dist = (total - last) / remain;
            for (int i = 0; i < count; i++)
            {
                var result = i == remain ? last : dist;
                multiplier.Add(result);
            }

            return multiplier.ToArray();
        }

        public override Model.Skill Use()
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class Attack : AttackBase
    {
        public Attack(float chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental) : base(chance, effect, elemental)
        {
        }

        public override Model.Skill Use()
        {
            var target = GetTarget();
            var info = ProcessDamage(target);

            return new Model.Attack
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = info,
            };
        }
    }
}
