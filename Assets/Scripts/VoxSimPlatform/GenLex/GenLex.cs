using UnityEngine;
using System.Linq;

using VoxSimPlatform.Vox;

namespace VoxSimPlatform { 
    namespace GenLex {
        public static class GenLex {
            /// <summary>
            /// Gets the equivalent GL type of a string representation (i.e., from a voxeme encoding)
            /// </summary>
            // IN: string
            // OUT: GLType
            public static GLType GetGLType(string typeStr) {
                GLType glType = GLType.None;

                switch(typeStr) {
                    case "agent":
                        glType = GLType.Agent;
                        break;

                    case "artifact":
                        glType = GLType.Artifact;
                        break;
                    
                    case "location":
                        glType = GLType.Location;
                        break;

                    case "physobj":
                        glType = GLType.PhysObj;
                        break;

                    default:
                        glType = GLType.None;
                        break;
                }

                return glType;
            }

            // IN: obj -- untyped object (entity); glType -- the GL type to check against
            // OUT: bool -- is this object represented by this GL type?
            public static bool IsGLType(object obj, GLType glType) {
                // implementation should reflect how each GL type in use
                //  is represented in a 3D visualized simulation
                // not every entity must also be a voxeme
                //  e.g., a location, as a specific point or region in continuous vector space,
                //  cannot be a voxeme
                bool isType = false;

                switch(glType) {
                    case GLType.None:
                        // an entity should never have no type
                        break;

                    case GLType.Agent:
                        if (obj is GameObject) {
                            if ((obj as GameObject).tag == "Agent") {
                                isType = true;
                            }
                        }
                        break;

                    case GLType.Artifact:
                        if (obj is GameObject) {
                            Voxeme voxComponent = (obj as GameObject).GetComponent<Voxeme>();
                            if (voxComponent != null) {
                                string[] types = voxComponent.voxml.Lex.Type.Split('*');
                                if (types.Where(t => GetGLType(t) == GLType.Artifact).ToList().Count > 0) {
                                    isType = true;
                                }
                            }
                        }
                        break;

                    case GLType.Location:
                        if (obj is Vector3) {
                            isType = true;
                        }
                        break;

                    case GLType.PhysObj:
                        if (obj is GameObject) {
                            Voxeme voxComponent = (obj as GameObject).GetComponent<Voxeme>();
                            if (voxComponent != null) {
                                string[] types = voxComponent.voxml.Lex.Type.Split('*');
                                if (types.Where(t => GetGLType(t) == GLType.PhysObj).ToList().Count > 0) {
                                    isType = true;
                                }
                            }
                        }
                        break;

                    default:
                        break;
                }

                return isType;
            }
        }
    }
}
