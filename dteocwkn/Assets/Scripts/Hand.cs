using UnityEngine;

public class Hand : MonoBehaviour
{
    public Transform card;
    public int nCards;
    public int tilt;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int nLeft, nMiddle, nRight;
        if (nCards % 2 == 1)
        {
            nLeft = nRight = nCards / 2;
            nMiddle = 1;
        }
        else
        {
            nLeft = nRight = nCards / 2 - 1;
            nMiddle = 2;
        }

        Debug.Log($"Hand configuration: Left={nLeft}, Middle={nMiddle}, Right={nRight}");

        for (int i = -nLeft; i < 0; i++)
        {
            MakeCard(new Vector3(i * 1f, 0.2f * (i + 1) - 0.1f, -2), new Vector3(0, 0, tilt * -i));
        }

        for (int i = 0; i < nMiddle; i++)
        {
            MakeCard(new Vector3(0, 0, -2), new Vector3(0, 0, 0)); // Middle cards are upright
        }
        
        for (int i = 1; i < nRight + 1; i++)
        {
            MakeCard(new Vector3(i * 1f, 0.2f * -(i - 1) - 0.1f, -2), new Vector3(0, 0, -tilt * i));
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    void MakeCard(Vector3 position, Vector3 rotation)
    {
        Transform newCard = Instantiate(card, position, Quaternion.Euler(rotation), transform);
        newCard.localPosition = position;
    }
}
