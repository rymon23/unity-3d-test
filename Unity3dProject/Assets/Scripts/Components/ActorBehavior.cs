using System.Linq;
using System;
using UnityEngine;


namespace Hybrid.Components
{
    public enum Behavior_Confidence
    {
        cowardly = 0,
        cautious = 1,
        average = 2,
        brave = 3,
        foolhardy = 4,
    }
    public enum Behavior_Aggression
    {
        unaggressive = 0,
        aggressive = 1,
        hostile = 2,
        frenzied = 3,
    }

    public enum Behavior_Assistance
    {
        nobody = 0,
        allies = 1,
        friends_and_allies = 2,
    }
    public enum Behavior_Morality
    {
        any_crime = 0,
        enemies = 1,
        no_crime = 2,
    }

    public class ActorBehavior : MonoBehaviour
    {

        [SerializeField] private float fleeHealthThreshhold = 0f;
        [SerializeField] private bool _shouldFlee = false;
        [SerializeField] private Behavior_Aggression _aggression = Behavior_Aggression.aggressive;
        public Behavior_Aggression aggression
        {
            get => _aggression;
            set => _aggression = value;
        }

        [SerializeField] private Behavior_Confidence _confidence = Behavior_Confidence.average;
        public Behavior_Confidence confidence
        {
            get => _confidence;
            set => _confidence = value;
        }

        [SerializeField] private Behavior_Morality _morality = Behavior_Morality.enemies;
        public Behavior_Morality morality
        {
            get => _morality;
            set => _morality = value;
        }
        [SerializeField] private Behavior_Assistance _assistance = Behavior_Assistance.allies;
        public Behavior_Assistance assistance
        {
            get => _assistance;
            set => _assistance = value;
        }

        ActorEventManger actorEventManger;


        private void Start()
        {
            actorEventManger = GetComponent<ActorEventManger>();
            if (actorEventManger != null)
            {
                actorEventManger.onEvaluateFleeingState += EvaluateFleeState;
            }
        }
        private void OnDestroy()
        {
            if (actorEventManger != null) actorEventManger.onEvaluateFleeingState -= EvaluateFleeState;
        }

        public void EvaluateFleeState(float currentHealthPercent)
        {

            if (confidence == Behavior_Confidence.cowardly)
            {
                _shouldFlee = true;
                fleeHealthThreshhold = 1;
                return;
            }
            fleeHealthThreshhold = (1 - ((float)confidence / 5f));
            _shouldFlee = currentHealthPercent < fleeHealthThreshhold;
        }

    }
}