using UnityEngine;

public class Hand : MonoBehaviour
{
    public RectTransform cardPrefab;
    public Canvas targetCanvas;
    public int nCards;
    public int tilt;
    public float xSpacing;
    public float ySpacing;

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
            MakeCard(new Vector3(i * xSpacing, ySpacing * i, -2), new Vector3(0, 0, tilt * -i));
        }

        for (int i = 0; i < nMiddle; i++)
        {
            MakeCard(new Vector3(0, 0, -2), new Vector3(0, 0, 0)); // Middle cards are upright
        }
        
        for (int i = 1; i < nRight + 1; i++)
        {
            MakeCard(new Vector3(i * xSpacing, ySpacing * -i, -2), new Vector3(0, 0, -tilt * i));
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    void MakeCard(Vector3 position, Vector3 rotation)
    {
        RectTransform newCard = Instantiate(cardPrefab, position, Quaternion.Euler(rotation), targetCanvas.transform.Find("HandHolder"));
        newCard.anchoredPosition = position;
    }
}
