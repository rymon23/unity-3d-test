using System.Collections.Generic;
using UnityEngine;

namespace Hybrid.Components
{
    public class ActorSpells : MonoBehaviour
    {
        [SerializeField]
        public Dictionary<SpellDeliveryType, MagicSpell[]> spellList
        { get; private set; } //= new Dictionary<SpellDeliveryType, Spell[]>();

        public MagicSpell[] spellsTemp;
    }
}