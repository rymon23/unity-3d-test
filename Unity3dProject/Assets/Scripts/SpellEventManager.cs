using System;
using UnityEngine;

namespace Hybrid.Components
{
    public class SpellEventManager : MonoBehaviour
    {
        public event Action<SpellInstanceData> onInit;

        public event Action<SpellInstanceData> onEffectStart;

        public event Action<SpellInstanceData> onEffectUpdate;

        public event Action<SpellInstanceData> onEffectFinish;

        public event Action<SpellInstanceData> onTargetDeath;

        public void ActorDeath(SpellInstanceData spellInstanceData) =>
            onInit?.Invoke(spellInstanceData);

        public void EffectStart(SpellInstanceData spellInstanceData) =>
            onEffectStart?.Invoke(spellInstanceData);

        public void EffectUpdate(SpellInstanceData spellInstanceData) =>
            onEffectUpdate?.Invoke(spellInstanceData);

        public void EffectFinish(SpellInstanceData spellInstanceData) =>
            onEffectFinish?.Invoke(spellInstanceData);

        public void TargetDeath(SpellInstanceData spellInstanceData) =>
            onTargetDeath?.Invoke(spellInstanceData);
    }
}
