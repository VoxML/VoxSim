using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using VoxSimPlatform.Global;
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
                GLType glType = GLType.T;

                switch(typeStr) {
                    case "agent":
                        glType = GLType.Agent;
                        break;

                    case "agent[]":
                        glType = GLType.AgentList;
                        break;

                    case "artifact":
                        glType = GLType.Artifact;
                        break;

                    case "artifact[]":
                        glType = GLType.ArtifactList;
                        break;
                    
                    case "location":
                        glType = GLType.Location;
                        break;

                    case "location[]":
                        glType = GLType.LocationList;
                        break;

                    case "physobj":
                        glType = GLType.PhysObj;
                        break;

                    case "physob[]":
                        glType = GLType.PhysObjList;
                        break;

                    case "vector":
                        glType = GLType.Vector;
                        break;

                    case "vector[]":
                        glType = GLType.VectorList;
                        break;

                    case "method":
                        glType = GLType.Method;
                        break;

                    case "method[]":
                        glType = GLType.MethodList;
                        break;

                    default:
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
                    case GLType.T:
                        break;

                    case GLType.Agent:
                        if (obj is GameObject) {
                            if ((obj as GameObject).tag == "Agent") {
                                isType = true;
                            }
                        }
                        break;

                    case GLType.AgentList:
                        if ((obj is IList) && (obj.GetType().IsGenericType) &&
                            (obj.GetType().IsAssignableFrom(typeof(List<GameObject>)))) {
                            if (((List<GameObject>)obj).All(o => o.tag == "Agent")) {
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
                    
                    case GLType.ArtifactList:
                        if ((obj is IList) && (obj.GetType().IsGenericType) &&
                            (obj.GetType().IsAssignableFrom(typeof(List<GameObject>)))) {
                            List<Voxeme> voxComponents = ((List<GameObject>)obj).Select(o => o.GetComponent<Voxeme>()).ToList();
                            if (voxComponents.Count > 0) {
                                if (voxComponents.Select(v => v.voxml.Lex.Type.Split('*')).ToList().All(t => t.Contains("artifact"))) {
                                    isType = true;
                                }
                            }
                        }
                        break;

                    case GLType.Location:
                        if (obj is Vector3) {
                            isType = true;
                        }
                        else if ((obj is string) && (Helper.vec.IsMatch(obj as string))) {
                            isType = true;
                        }
                        break;
                    
                    case GLType.LocationList:
                        if ((obj is IList) && (obj.GetType().IsGenericType)) {
                            if (obj.GetType().IsAssignableFrom(typeof(List<Vector3>))) {
                                isType = true;
                            }
                            else if (obj.GetType().IsAssignableFrom(typeof(List<string>))) {
                                if (((List<string>)obj).All(o => Helper.vec.IsMatch(o))) {
                                    isType = true;
                                }
                            }
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

                    case GLType.PhysObjList:
                        if ((obj is IList) && (obj.GetType().IsGenericType) &&
                            (obj.GetType().IsAssignableFrom(typeof(List<GameObject>)))) {
                            List<Voxeme> voxComponents = ((List<GameObject>)obj).Select(o => o.GetComponent<Voxeme>()).ToList();
                            if (voxComponents.Count > 0) {
                                if (voxComponents.Select(v => v.voxml.Lex.Type.Split('*')).ToList().All(t => t.Contains("physobj"))) {
                                    isType = true;
                                }
                            }
                        }
                        break;

                    case GLType.Vector:
                        if (obj is Vector3) {
                            isType = true;
                        }
                        else if ((obj is string) && (Helper.vec.IsMatch(obj as string))) {
                            isType = true;
                        }
                        break;

                    case GLType.VectorList:
                        if ((obj is IList) && (obj.GetType().IsGenericType)) {
                            if (obj.GetType().IsAssignableFrom(typeof(List<Vector3>))) {
                                isType = true;
                            }
                            else if (obj.GetType().IsAssignableFrom(typeof(List<string>))) {
                                if (((List<string>)obj).All(o => Helper.vec.IsMatch(o))) {
                                    isType = true;
                                }
                            }
                        }
                        break;

                    case GLType.Method:
                        if (obj is MethodInfo) {
                            isType = true;
                        }
                        break;

                    case GLType.MethodList:
                        if ((obj is IList) && (obj.GetType().IsGenericType)) {
                            if (obj.GetType().IsAssignableFrom(typeof(List<MethodInfo>))) {
                                isType = true;
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
