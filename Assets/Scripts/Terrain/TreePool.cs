using System.Collections.Generic;
using UnityEngine;

public class TreePool : MonoBehaviour
{
    public GameObject treeParent;
    public GameObject treePrefab;
    public int poolSize = 1000;  // Number of trees to pool
    private Queue<GameObject> treePool;

    private void Start()
    {
        //treePool = new Queue<GameObject>();

        //// Pre-instantiate tree objects and add to pool
        //for (int i = 0; i < poolSize; i++)
        //{
        //    GameObject tree = Instantiate(treePrefab, treeParent.transform);
        //    tree.SetActive(false);  // Start inactive
        //    treePool.Enqueue(tree);
        //}
    }

    // Get an unused tree from the pool
    public GameObject GetTree(Vector3 position, Quaternion rotation)
    {
        if (treePool.Count > 0)
        {
            GameObject tree = treePool.Dequeue();
            tree.transform.position = position;
            tree.transform.rotation = rotation;
            tree.SetActive(true);  // Activate the tree
            return tree;
        }
        else
        {
            // If no trees left in the pool, instantiate a new one
            GameObject newTree = Instantiate(treePrefab, position, rotation, treeParent.transform);
            return newTree;
        }
    }

    // Return tree to the pool when it's no longer needed
    public void ReturnTree(GameObject tree)
    {
        tree.SetActive(false);
        treePool.Enqueue(tree);
    }
}
