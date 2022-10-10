using UnityEngine;

namespace Hybrid.Components
{
    public interface IMagicEffect
    {
        public void OnEffectStart(SpellInstanceData spellInstanceData);
        public void OnEffectUpdate(SpellInstanceData spellInstanceData);
        public void OnEffectFinish(SpellInstanceData spellInstanceData);
    }
}
