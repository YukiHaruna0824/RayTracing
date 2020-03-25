using UnityEngine;
using System.Collections;

public class Test : MonoBehaviour
{
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100))
        {
            Texture2D tex = hit.transform.GetComponent<MeshRenderer>().material.mainTexture as Texture2D;
            Vector2 pixelUV = hit.textureCoord;
            print(pixelUV);
            pixelUV.x *= tex.width;
            pixelUV.y *= tex.height;

            tex.SetPixel((int)pixelUV.x, (int)pixelUV.y, Color.black);
            tex.Apply();
        }
    }
}