using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabFaces : MonoBehaviour {
    // Start is called before the first frame update
    //#pragma strict

    public int materialIndex;
    public Vector2 uvAnimationRate;
    public string textureName;
    Vector2 uvOffset;



    // https://answers.unity.com/questions/542787/change-texture-of-cube-sides.html
    void Start() {
        materialIndex = 0;
        uvAnimationRate = new Vector2(0.5f, 0.0f);
        textureName = "_MainTex";
        uvOffset = Vector2.zero;
        Mesh mesh = GetComponent<MeshFilter>().mesh;

        Vector2[] UVs = new Vector2[mesh.vertices.Length];
        // Front
        UVs[0] = new Vector2(0.0f, 0.0f);
        UVs[1] = new Vector2(0.333f, 0.0f);
        UVs[2] = new Vector2(0.0f, 0.333f);
        UVs[3] = new Vector2(0.333f, 0.333f);
        // Top
        UVs[4] = new Vector2(0.334f, 0.333f);
        UVs[5] = new Vector2(0.666f, 0.333f);
        UVs[8] = new Vector2(0.334f, 0.0f);
        UVs[9] = new Vector2(0.666f, 0.0f);
        // Back
        UVs[6] = new Vector2(1.0f, 0.0f);
        UVs[7] = new Vector2(0.667f, 0.0f);
        UVs[10] = new Vector2(1.0f, 0.333f);
        UVs[11] = new Vector2(0.667f, 0.333f);
        // Bottom
        UVs[12] = new Vector2(0.0f, 0.334f);
        UVs[13] = new Vector2(0.0f, 0.666f);
        UVs[14] = new Vector2(0.333f, 0.666f);
        UVs[15] = new Vector2(0.333f, 0.334f);
        // Left
        UVs[16] = new Vector2(0.334f, 0.334f);
        UVs[17] = new Vector2(0.334f, 0.666f);
        UVs[18] = new Vector2(0.666f, 0.666f);
        UVs[19] = new Vector2(0.666f, 0.334f);
        // Right        
        UVs[20] = new Vector2(0.667f, 0.334f);
        UVs[21] = new Vector2(0.667f, 0.666f);
        UVs[22] = new Vector2(1.0f, 0.666f);
        UVs[23] = new Vector2(1.0f, 0.334f);



        // Set the top of the cube to INSTEAD be the left/right/top/back in a 2x2 grid
        // Top
        UVs[4] = new Vector2(0f, 0f);
        UVs[5] = new Vector2(0f, 0f);
        UVs[8] = new Vector2(0f, 0f); // unchanged
        UVs[9] = new Vector2(0f, 0f);


        mesh.uv = UVs;
    }

    // Update is called once per frame
    void Update() {
        // pull direction of new focus
        //uvAnimationRate = new Vector2(0.5f, 0.1f);
        //uvOffset = Vector2.zero;
        if (Input.GetKey("m")) {
            Mesh mesh = GetComponent<MeshFilter>().mesh;
            uvAnimationRate = new Vector2(0.0f, 0.0f); // stops rotation
            Debug.Log(mesh.uv[4] + "" + mesh.uv[5] + "" + mesh.uv[8] + "" + mesh.uv[9]);

            Texture2D tex = (UnityEngine.Texture2D)GetComponent<Renderer>().material.mainTexture; // Get texture of object under mouse pointer
            //c = tex.GetPixelBilinear(hit.textureCoord2.x, hit.textureCoord2.y); // Get color from texture

        }

    }


    // http://wiki.unity3d.com/index.php/Scrolling_UVs
    void LateUpdate() {
        // set the offset, multiply times delta.
        uvOffset += (uvAnimationRate * Time.deltaTime);
        if (GetComponent<Renderer>().enabled) {
            GetComponent<Renderer>().materials[materialIndex].SetTextureOffset(textureName, uvOffset);
        }
    }

    void ZoomNewTexture(Vector2[] corners) {
        // Corners ordered: top left, top right, bottom left, bottom right
        // At least, for the face that's from above
        // We want it to, uh, slowly move to the new textures.
        // Else we need to figure out camera movement, I think.

    }


}


// Finding 'color' under a mouse position (really use it for just getting place on texture)
// https://forum.unity.com/threads/get-color-of-texture-under-mouse-position.103500/

//Vector2 pos = Input.mousePosition; // Mouse position
//RaycastHit hit;
//Camera _cam = Camera.mainCamera; // Camera to use for raycasting
//Ray ray = _cam.ScreenPointToRay(pos);
//Physics.Raycast(_cam.transform.position, ray.direction, out hit, 10000.0f);
//Color c;
//if(hit.collider) {
//    Texture2D tex = (Texture2D)hit.collider.gameObject.renderer.material.mainTexture; // Get texture of object under mouse pointer
//c = tex.GetPixelBilinear(hit.textureCoord2.x, hit.textureCoord2.y); // Get color from texture
//}


// https://answers.unity.com/questions/234215/parrallax-texture-offset-based-on-camera-movement.html


// https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html

//public class ScrollingUVs : MonoBehaviour {
//    public int materialIndex = 0;
//    public Vector2 uvAnimationRate = new Vector2(1.0f, 0.0f);
//    public string textureName = "_MainTex";

//    Vector2 uvOffset = Vector2.zero;

//    void LateUpdate() {
//        uvOffset += (uvAnimationRate * Time.deltaTime);
//        if (renderer.enabled) {
//            renderer.materials[materialIndex].SetTextureOffset(textureName, uvOffset);
//        }
//    }
//}


//Mesh mesh = GetComponent<MeshFilter>().mesh;
//Vector2[] UVs = new Vector2[mesh.vertices.Length];
//// Front
//UVs[0] = new Vector2(0.0f, 0.0f);
//UVs[1] = new Vector2(0.333f, 0.0f);
//UVs[2] = new Vector2(0.0f, 0.333f);
//UVs[3] = new Vector2(0.333f, 0.333f);
//// Top
//UVs[4] = new Vector2(0.334f, 0.333f);
//UVs[5] = new Vector2(0.666f, 0.333f);
//UVs[8] = new Vector2(0.334f, 0.0f);
//UVs[9] = new Vector2(0.666f, 0.0f);
//// Back
//UVs[6] = new Vector2(1.0f, 0.0f);
//UVs[7] = new Vector2(0.667f, 0.0f);
//UVs[10] = new Vector2(1.0f, 0.333f);
//UVs[11] = new Vector2(0.667f, 0.333f);
//// Bottom
//UVs[12] = new Vector2(0.0f, 0.334f);
//UVs[13] = new Vector2(0.0f, 0.666f);
//UVs[14] = new Vector2(0.333f, 0.666f);
//UVs[15] = new Vector2(0.333f, 0.334f);
//// Left
//UVs[16] = new Vector2(0.334f, 0.334f);
//UVs[17] = new Vector2(0.334f, 0.666f);
//UVs[18] = new Vector2(0.666f, 0.666f);
//UVs[19] = new Vector2(0.666f, 0.334f);
//// Right        
//UVs[20] = new Vector2(0.667f, 0.334f);
//UVs[21] = new Vector2(0.667f, 0.666f);
//UVs[22] = new Vector2(1.0f, 0.666f);
//UVs[23] = new Vector2(1.0f, 0.334f);
//mesh.uv = UVs;