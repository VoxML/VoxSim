using UnityEngine;
using System.Collections;

// NOTE: Not yet implemented. Most recent thing, started on the same day I'm adding these notes.

public class TextureGrabber : MonoBehaviour {
    Texture2D texture;
    // This should grab textures from a file, apply them to objects.
    // Think of it as the first step to making a functional heatmap or sorts.
    void Start() {
        // From a unity answers thread, user Jaap Kreijkamp: https://answers.unity.com/questions/9919/how-do-i-create-a-texture-dynamically-in-unity.html
        // Create a new 2x2 texture ARGB32 (32 bit with alpha) and no mipmaps
        texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);

        // set the pixel values
        texture.SetPixel(0, 0, new Color(1.0f, 1.0f, 1.0f, 0.5f));
        texture.SetPixel(1, 0, Color.clear);
        texture.SetPixel(0, 1, Color.white);
        texture.SetPixel(1, 1, Color.black);

        // Apply all SetPixel calls
        texture.Apply();

        // connect texture to material of GameObject this script is attached to
        transform.parent.Find("VisionCanvas").Find("VisionCameraMask").Find("VisionCamera").GetComponent<CanvasRenderer>().SetTexture(texture);

        //renderer.material.mainTexture = texture;
    }

    // Update is called once per frame
    void Update() {

    }
}
