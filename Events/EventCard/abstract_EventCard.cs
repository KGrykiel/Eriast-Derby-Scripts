using UnityEngine;

public abstract class EventCard : ScriptableObject
{
    [TextArea]
    public string description;

    // Called when the event is triggered for a vehicle
    public abstract void Trigger(Vehicle vehicle, Stage stage);
}
