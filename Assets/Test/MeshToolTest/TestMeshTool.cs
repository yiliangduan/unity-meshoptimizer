using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yiliang.Tools;

public class TestMeshTool : MonoBehaviour
{

    public Material AMaterial;
    public Material BMaterial;

    // Start is called before the first frame update
    void Start()
    {
        if(MaterialTool.Compare(AMaterial, BMaterial))
        {
            Debug.Log("same");
        }
        else
        {
            Debug.Log("diff");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
