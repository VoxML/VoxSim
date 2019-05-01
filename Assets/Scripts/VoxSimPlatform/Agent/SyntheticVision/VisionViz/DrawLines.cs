using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxSimPlatform {
    namespace Agent {
        namespace SyntheticVision {
            namespace VisionViz {
            	[ExecuteInEditMode]
            	public class DrawLines : MonoBehaviour {
            		public Material lineMaterial;
            		List<Vector3[,]> outlines;
            		List<Color> colors;

            		void Awake() {
            			outlines = new List<Vector3[,]>();
            			colors = new List<Color>();
            			lineMaterial = CreateLineMaterial();
            		}

            		void OnPostRender() {
            			if (outlines == null || lineMaterial == null) return;
            			if (lineMaterial.SetPass(0)) {
            				GL.Begin(GL.LINES);
            				for (int j = 0; j < outlines.Count; j++) {
            					GL.Color(colors[j]);
            //                    Debug.Log(colors[j]);
            					for (int i = 0; i < outlines[j].GetLength(0); i++) {
            //                        Debug.Log(string.Format("{0}-{1}: {2} - {3} ({4})", j, i, outlines[j][i,0], outlines[j][i,1], colors[j]));
            						GL.Vertex(outlines[j][i, 0]);
            						GL.Vertex(outlines[j][i, 1]);
            					}
            				}

            				GL.End();
            			}
            		}

            		public void setOutlines(Vector3[,] newOutlines, Color newcolor) {
            			if (newOutlines == null) return;
            			if (outlines == null) return;
            			if (newOutlines.GetLength(0) > 0) {
            				outlines.Add(newOutlines);
            				colors.Add(newcolor);
            //                Debug.Log(string.Format("{0}/{1} - {2}", outlines.Count, colors.Count, newcolor));
            			}
            		}

            		void Update() {
            			outlines = new List<Vector3[,]>();
            			colors = new List<Color>();
            		}

            		static Material CreateLineMaterial() {
            			// Unity has a built-in shader that is useful for drawing
            			// simple colored things.
            			Shader shader = Shader.Find("Hidden/Internal-Colored");
            			Material lineMaterial = new Material(shader);
            			lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            			// Turn on alpha blending
            			lineMaterial.SetInt("_SrcBlend", (int) BlendMode.SrcAlpha);
            			lineMaterial.SetInt("_DstBlend", (int) BlendMode.OneMinusSrcAlpha);
            			// Turn backface culling off
            			lineMaterial.SetInt("_Cull", (int) CullMode.Off);
            			// Turn off depth writes
            			lineMaterial.SetInt("_ZWrite", 0);
            			return lineMaterial;
            		}
            	}
            }
        }
    }
}