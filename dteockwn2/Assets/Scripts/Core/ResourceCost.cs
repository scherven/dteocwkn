[System.Serializable]
public struct ResourceCost
{
    public ResourceType type;
    public int amount;

    public ResourceCost(ResourceType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
}
