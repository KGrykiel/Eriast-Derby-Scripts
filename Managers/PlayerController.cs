using UnityEngine;
using Assets.Scripts.Managers.PlayerUI;
using Assets.Scripts.Managers.Turn;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// MonoBehaviour bridge between the Unity Inspector and PlayerInputCoordinator.
    /// Holds the serialised UI references that cannot live in a plain C# class.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        private PlayerUIReferences ui;

        public PlayerInputCoordinator InputCoordinator { get; private set; }

        public void Initialize(TurnService turnController)
        {
            InputCoordinator = new PlayerInputCoordinator(turnController, ui);
        }
    }
}