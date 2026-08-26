using UnityEngine;
using UnityEngine.UI;

public class Cintas_Script : MonoBehaviour
{
    public RawImage image;
    public float x;
    public float y;

    // Update is called once per frame
    void Update()
    {
       image.uvRect = new Rect(image.uvRect.position + new Vector2(x,y) * Time.deltaTime, image.uvRect.size);
    }
}
