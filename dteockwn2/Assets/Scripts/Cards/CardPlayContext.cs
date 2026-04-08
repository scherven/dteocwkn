using UnityEngine;

public class CardPlayContext
{
    public GameInventory inventory;
    public BuildingManager buildingManager;
    public TaskDispatcher taskDispatcher;
    public DeckManager deckManager;
    public HammerManager hammerManager;
    public CardData sourceCard;
    public Vector3? targetPosition;   // null for self-resolving cards (e.g. resource cards)
    public GameObject targetObject;   // null for self-resolving cards
}
