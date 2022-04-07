using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hybrid.Components;

public struct WeighteChoice_CombatMovmentType
{
    public WeighteChoice_CombatMovmentType(CombatMovementType _combatMovementType, float _weight = 0.25f)
    {
        combatMovementType = _combatMovementType;
        weight = _weight;
    }
    public CombatMovementType combatMovementType { get; }
    public float weight { get; set; }
}

public class AIDecisionController : MonoBehaviour
{

    [SerializeField] float[] _decisionWeights = new float[2] { 0.1f, 1f };
    // Combat Movment
    public float advanceMult = 0.14f;
    public float holdPositionMult = 0.09f;
    public float circleMult = 0.55f;
    public float fallbackMult = 0.12f;


    public float bashMult = 0.1f;
    public float attackMult = 0.15f;

    // Attack Avoidance 
    public float blockMult = 1f;
    public float dodgeMult = 1f;

    public HashSet<WeighteChoice_CombatMovmentType> combatMovementWeights;

    public CombatMovementType lastMovmentSelected;

    void Start()
    {
        combatMovementWeights = new HashSet<WeighteChoice_CombatMovmentType>();
        combatMovementWeights.Add(new WeighteChoice_CombatMovmentType(CombatMovementType.pressAttack, advanceMult));
        combatMovementWeights.Add(new WeighteChoice_CombatMovmentType(CombatMovementType.fallBack, fallbackMult));
        combatMovementWeights.Add(new WeighteChoice_CombatMovmentType(CombatMovementType.flankLeft, circleMult));
        combatMovementWeights.Add(new WeighteChoice_CombatMovmentType(CombatMovementType.flankRight, circleMult));
        // combatMovementWeights.Add(new WeighteChoice_CombatMovmentType(CombatMovementType.holdPosition, holdPositionMult));
    }

    public CombatMovementType GetCombatMovementChoice()
    {
        float itemWeightIndex = (float)Random.Range(_decisionWeights[0], _decisionWeights[1]);
        CombatMovementType choice = CombatMovementType.pressAttack;
        float currentWeight = 0;

        foreach (var item in combatMovementWeights)
        {
            if (lastMovmentSelected == item.combatMovementType)
            {
                currentWeight += item.weight * 0.5f;
            }
            else
            {
                currentWeight += item.weight;
            }
            // Debug.Log("GetCombatMovementChoice => item " + item.combatMovementType + " currentWeight: " + currentWeight + " \n itemWeightIndex: " + itemWeightIndex);

            if (currentWeight >= itemWeightIndex)
            {
                choice = item.combatMovementType;
                // Debug.Log("GetCombatMovementChoice => choose: " + choice + " | itemWeightIndex: " + itemWeightIndex);
                break;
            }
        }
        // Default
        return choice;
    }

    // public static T RandomElementByWeight<T>(this IEnumerable<T> sequence, Func<T, float> weightSelector)
    // {
    //     float totalWeight = sequence.Sum(weightSelector);
    //     // The weight we are after...
    //     float itemWeightIndex = (float)new Random().NextDouble() * totalWeight;
    //     float currentWeightIndex = 0;

    //     foreach (var item in from weightedItem in sequence select new { Value = weightedItem, Weight = weightSelector(weightedItem) })
    //     {
    //         currentWeightIndex += item.Weight;

    //         // If we've hit or passed the weight we are after for this item then it's the one we want....
    //         if (currentWeightIndex >= itemWeightIndex)
    //             return item.Value;

    //     }

    //     return default(T);

    // }

    // Start is called before the first frame update

}
