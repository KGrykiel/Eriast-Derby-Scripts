using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Stage : MonoBehaviour
{
    public string stageName;
    public float length = 10f;
    public List<Stage> nextStages = new List<Stage>();

    [Header("Modifiers applied on enter")]
    public List<AttributeModifierEffect> onEnterModifiers = new List<AttributeModifierEffect>();

    [Header("Event Cards")]
    public List<EventCard> eventCards = new List<EventCard>();

    [Header("Events")]
    public UnityEvent onEnter;
    public UnityEvent onLeave;

    // Track vehicles currently in this stage
    [HideInInspector]
    public List<Vehicle> vehiclesInStage = new List<Vehicle>();

    private void OnDrawGizmos()
    {
        if (nextStages != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var nextStage in nextStages)
            {
                if (nextStage != null)
                {
                    Gizmos.DrawLine(transform.position, nextStage.transform.position);
                }
            }
        }
    }

    public void DrawAndTriggerEventCard(Vehicle vehicle)
    {
        if (eventCards == null || eventCards.Count == 0) return;

        // Draw a random event card
        var card = eventCards[Random.Range(0, eventCards.Count)];
        card.Trigger(vehicle, this);
    }


    public void TriggerEnter(Vehicle vehicle)
    {
        // Add vehicle to the list if not already present
        if (!vehiclesInStage.Contains(vehicle))
            vehiclesInStage.Add(vehicle);

        foreach (var modData in onEnterModifiers)
        {
            if (modData != null)
                vehicle.AddModifier(modData.ToRuntimeModifier(this));
        }
        DrawAndTriggerEventCard(vehicle);
        onEnter?.Invoke();
    }

    public void TriggerLeave(Vehicle vehicle)
    {
        // Remove vehicle from the list
        vehiclesInStage.Remove(vehicle);

        // Remove all modifiers applied by this stage (using 'this' as the source)
        vehicle.RemoveModifiersFromSource(this, true);

        onLeave?.Invoke();
    }
}
