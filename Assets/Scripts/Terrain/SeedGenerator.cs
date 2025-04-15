using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SeedGenerator : MonoBehaviour
{
    private Dictionary<int, string> wordDictionary = new Dictionary<int, string>();
    private string seed;

    private void Awake()
    {
        PopulateDictionary();

        List<string> selectedWords = GetRandomWords(3);
        string joined = string.Join("", selectedWords);
        SetSeed(joined);

        SetSeed("awedsxdfrtfvghbjkmkl"); // For Testing
        //SetSeed("banana");

        MeshGenerator meshGenerator = gameObject.GetComponent<MeshGenerator>();
        meshGenerator.StartTerrain();

        //StartCoroutine(WaitForPlayerAndStartTerrain());
    }

    private IEnumerator WaitForPlayerAndStartTerrain()
    {
        MeshGenerator meshGenerator = gameObject.GetComponent<MeshGenerator>();

        // Wait until the player is assigned
        while (meshGenerator.player == null)
        {
            Debug.LogWarning("Waiting for player assignment in MeshGenerator...");
            yield return null; // Wait for the next frame
        }

        // Once the player is assigned, start terrain generation
        Debug.Log("Player assigned. Starting terrain generation...");
        meshGenerator.StartTerrain();
    }

    public void SetSeed(string seed)
    {
        Debug.Log("Set seed to: "+ seed);
        this.seed = seed;
    }

    public string GetSeed()
    {
        return seed;
    }

    private void PopulateDictionary()
    {
        wordDictionary.Add(1, "Mountain");
        wordDictionary.Add(2, "River");
        wordDictionary.Add(3, "Cave");
        wordDictionary.Add(4, "Forest");
        wordDictionary.Add(5, "Desert");
        wordDictionary.Add(6, "Valley");
        wordDictionary.Add(7, "Ocean");
        wordDictionary.Add(8, "Glacier");
        wordDictionary.Add(9, "Cliff");
        wordDictionary.Add(10, "Canyon");
        wordDictionary.Add(11, "Marsh");
        wordDictionary.Add(12, "Hills");
        wordDictionary.Add(13, "Meadow");
        wordDictionary.Add(14, "Volcano");
        wordDictionary.Add(15, "Island");
        wordDictionary.Add(16, "Prairie");
        wordDictionary.Add(17, "Lagoon");
        wordDictionary.Add(18, "Peninsula");
        wordDictionary.Add(19, "Plateau");
        wordDictionary.Add(20, "Jungle");
        wordDictionary.Add(21, "Dune");
        wordDictionary.Add(22, "Steppe");
        wordDictionary.Add(23, "Savannah");
        wordDictionary.Add(24, "Geyser");
        wordDictionary.Add(25, "Waterfall");
    }

    private List<string> GetRandomWords(int count)
    {
        List<string> randomWords = new List<string>();
        List<int> keys = new List<int>(wordDictionary.Keys);
        System.Random random = new System.Random();

        while (randomWords.Count < count && keys.Count > 0)
        {
            // Get a random index
            int randomIndex = random.Next(keys.Count);

            // Fetch the word at that index
            int key = keys[randomIndex];
            randomWords.Add(wordDictionary[key]);

            // Remove the key to avoid duplicates
            keys.RemoveAt(randomIndex);
        }

        return randomWords;
    }
}
