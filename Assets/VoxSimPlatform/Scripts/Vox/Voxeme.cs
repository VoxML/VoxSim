using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

using MajorAxes;
//using RootMotion.FinalIK;
using VoxSimPlatform.Agent;
using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Vox {
        public class Voxeme : MonoBehaviour {
            [HideInInspector] public VoxML voxml = new VoxML();

            private bool showOpVox = false;
            public OperationalVox opVox;

            public string predicate;
            
            public float density;

            // rotation information for each subobject's rigidbody
            // (physics-resultant changes between the completion of one event and the start of the next must be brought into line)
            //public Dictionary<string,Vector3> startEventRotations = new Dictionary<string, Vector3> ();
            //public Dictionary<string,Vector3> endEventRotations = new Dictionary<string, Vector3> ();
            public Dictionary<GameObject, Vector3> displacement = new Dictionary<GameObject, Vector3>();
            public Dictionary<GameObject, Vector3> rotationalDisplacement = new Dictionary<GameObject, Vector3>();

            Rigging rigging;

            VoxMLLibrary library;

            public GameObject graspConvention = null;

            //public List<InteractionTarget> interactionTargets = new List<InteractionTarget>();

            public LinkedList<Vector3> interTargetPositions = new LinkedList<Vector3>();

            private Vector3 _targetPosition;
            public Vector3 targetPosition {
                get { return _targetPosition; }
                set {
                    if (_targetPosition != value) {
                        OnTargetPositionChanged(_targetPosition, value);
                    }
                    _targetPosition = value;
                }
            }

            public LinkedList<Vector3> interTargetRotations = new LinkedList<Vector3>();

            private Vector3 _targetRotation;
            public Vector3 targetRotation {
                get { return _targetRotation; }
                set {
                    if (_targetRotation != value) {
                        OnTargetRotationChanged(_targetRotation, value);
                    }
                    _targetRotation = value;
                }
            }

            private Vector3 _targetScale;
            public Vector3 targetScale {
                get { return _targetScale; }
                set {
                    if (_targetScale != value) {
                        OnTargetScaleChanged(_targetScale, value);
                    }
                    _targetScale = value;
                }
            }

            public float moveSpeed = 1.0f;
            public float turnSpeed = 5.0f;
            public float defaultMoveSpeed = 0.0f;
            public float defaultTurnSpeed = 0.0f;

            public float minYBound;
            //    public float minYBound {
            //        get { return _minYBound; }
            //        set {
            //                if (value != _minYBound) {
            //                    Debug.Break ();
            //                }
            //                _minYBound = value;
            //            }
            //    }

            private GameObject _supportingSurface;
            public GameObject supportingSurface {
                get { return _supportingSurface; }
                set {
                    if (_supportingSurface != value) {
                        Debug.Log(string.Format("==================== Supporting surface changed ==================== {0}: {1}->{2}",
                            gameObject.name,
                            (_supportingSurface == null) ? "NULL" : _supportingSurface.name,
                            (value == null) ? "NULL" : value.name));
                    }
                    _supportingSurface = value;
                }
            }

            public bool _rigidbodiesOutOfSync;
            public bool rigidbodiesOutOfSync {
                get { return _rigidbodiesOutOfSync; }
                set {
                    if (_rigidbodiesOutOfSync != value) {
                        Debug.Log(string.Format("==================== rigidbodiesOutOfSync changed ==================== {0}: {1}->{2}",
                             gameObject.name,
                             _rigidbodiesOutOfSync,
                             value));
                    }
                    _rigidbodiesOutOfSync = value;
                }
            }

            public bool isGrasped = false;
            public Transform graspTracker = null;
            public Transform grasperCoord = null;

            public Voxeme[] children;
            public Dictionary<GameObject, Vector3> parentToChildPositionOffset;
            public Dictionary<GameObject, Quaternion> parentToChildRotationOffset;

            public Vector3 startPosition;
            public Vector3 startRotation;
            public Vector3 startScale;

            public Transform defaultParent;

            public event EventHandler VoxMLLoaded;

            public void OnVoxMLLoaded(object sender, EventArgs e) {
                if (VoxMLLoaded != null) {
                    VoxMLLoaded(this, e);
                }
            }
                
#if UNITY_EDITOR
            // Todo: How many of these fields are actually in active use?
            [CustomEditor(typeof(Voxeme))]
            public class DebugPreview : Editor {
                public override void OnInspectorGUI() {
                    var bold = new GUIStyle();
                    bold.fontStyle = FontStyle.Bold;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Predicate", GUILayout.Width(120));
                    EditorGUI.BeginChangeCheck();
                    string predicate = ((Voxeme)target).predicate;
                    predicate = GUILayout.TextField(predicate, GUILayout.MaxWidth(200));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Inspector");
                        ((Voxeme)target).predicate = predicate;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Density", GUILayout.Width(120));
                    EditorGUI.BeginChangeCheck();
                    float density = ((Voxeme)target).density;
                    density = Convert.ToSingle(GUILayout.TextField(density.ToString(), GUILayout.MaxWidth(200)));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Inspector");
                        ((Voxeme)target).density = density;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Displacement", bold);
                    if (((Voxeme)target).displacement != null) {
                        foreach (GameObject item in ((Voxeme)target).displacement.Keys) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(item.name, GUILayout.Width(120));
                            GUILayout.Label("X");
                            GUILayout.TextField(((Voxeme)target).displacement[item].x.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("Y");
                            GUILayout.TextField(((Voxeme)target).displacement[item].y.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("Z");
                            GUILayout.TextField(((Voxeme)target).displacement[item].z.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.Label("Rotational Displacement", bold);
                    if (((Voxeme)target).rotationalDisplacement != null) {
                        foreach (GameObject item in ((Voxeme)target).rotationalDisplacement.Keys) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(item.name, GUILayout.Width(120));
                            GUILayout.Label("X");
                            GUILayout.TextField(((Voxeme)target).rotationalDisplacement[item].x.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("Y");
                            GUILayout.TextField(((Voxeme)target).rotationalDisplacement[item].y.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("Z");
                            GUILayout.TextField(((Voxeme)target).rotationalDisplacement[item].z.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Grasp Convention", GUILayout.Width(120));
                    GUILayout.TextField(((Voxeme)target).graspConvention == null ? "None" :
                        ((Voxeme)target).graspConvention.ToString(), GUILayout.MaxWidth(200));
                    GUILayout.EndHorizontal();

                    /*GUILayout.Label("Interaction Targets", bold);
                    if (((Voxeme)target).interactionTargets != null) {
                        foreach (InteractionTarget item in ((Voxeme)target).interactionTargets) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(item.name, GUILayout.Width(120));
                            GUILayout.TextField(item.ToString(), GUILayout.MaxWidth(200));
                            GUILayout.EndHorizontal();
                        }
                    }*/

                    GUILayout.Label("Inter Target Positions", bold);
                    if (((Voxeme)target).interTargetPositions != null) {
                        foreach (Vector3 item in ((Voxeme)target).interTargetPositions) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("X");
                            GUILayout.TextField(item.x.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("Y");
                            GUILayout.TextField(item.y.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("Z");
                            GUILayout.TextField(item.z.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Target Position", GUILayout.Width(120));
                    GUILayout.Label("X");
                    ((Voxeme)target).targetPosition = new Vector3(
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).targetPosition.x.ToString(), GUILayout.MaxWidth(60))),
                        ((Voxeme)target).targetPosition.y, ((Voxeme)target).targetPosition.z);
                    GUILayout.Label("Y");
                    ((Voxeme)target).targetPosition = new Vector3(((Voxeme)target).targetPosition.x,
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).targetPosition.y.ToString(), GUILayout.MaxWidth(60))),
                        ((Voxeme)target).targetPosition.z);
                    GUILayout.Label("Z");
                    ((Voxeme)target).targetPosition = new Vector3(((Voxeme)target).targetPosition.x, ((Voxeme)target).targetPosition.y,
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).targetPosition.z.ToString(), GUILayout.MaxWidth(60))));
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Inter Target Rotations", bold);
                    if (((Voxeme)target).interTargetRotations != null) {
                        foreach (Vector3 item in ((Voxeme)target).interTargetRotations) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("X");
                            GUILayout.TextField(item.x.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("Y");
                            GUILayout.TextField(item.y.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("Z");
                            GUILayout.TextField(item.z.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Target Rotation", GUILayout.Width(120));
                    GUILayout.Label("X");
                    ((Voxeme)target).targetRotation = new Vector3(
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).targetRotation.x.ToString(), GUILayout.MaxWidth(60))),
                        ((Voxeme)target).targetRotation.y, ((Voxeme)target).targetRotation.z);
                    GUILayout.Label("Y");
                    ((Voxeme)target).targetRotation = new Vector3(((Voxeme)target).targetRotation.x,
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).targetRotation.y.ToString(), GUILayout.MaxWidth(60))),
                        ((Voxeme)target).targetRotation.z);
                    GUILayout.Label("Z");
                    ((Voxeme)target).targetRotation = new Vector3(((Voxeme)target).targetRotation.x, ((Voxeme)target).targetRotation.y,
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).targetRotation.z.ToString(), GUILayout.MaxWidth(60))));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Target Scale", GUILayout.Width(120));
                    GUILayout.Label("X");
                    ((Voxeme)target).targetScale = new Vector3(
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).targetScale.x.ToString(), GUILayout.MaxWidth(60))),
                        ((Voxeme)target).targetScale.y, ((Voxeme)target).targetScale.z);
                    GUILayout.Label("Y");
                    ((Voxeme)target).targetScale = new Vector3(((Voxeme)target).targetScale.x,
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).targetScale.y.ToString(), GUILayout.MaxWidth(60))),
                        ((Voxeme)target).targetScale.z);
                    GUILayout.Label("Z");
                    ((Voxeme)target).targetScale = new Vector3(((Voxeme)target).targetScale.x, ((Voxeme)target).targetScale.y,
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).targetScale.z.ToString(), GUILayout.MaxWidth(60))));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Move Speed", GUILayout.Width(120));
                    EditorGUI.BeginChangeCheck();
                    float moveSpeed = ((Voxeme)target).moveSpeed;
                    moveSpeed = Convert.ToSingle(GUILayout.TextField(moveSpeed.ToString(), GUILayout.MaxWidth(200)));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Inspector");
                        ((Voxeme)target).moveSpeed = moveSpeed;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Turn Speed", GUILayout.Width(120));
                    EditorGUI.BeginChangeCheck();
                    float turnSpeed = ((Voxeme)target).turnSpeed;
                    turnSpeed = Convert.ToSingle(GUILayout.TextField(turnSpeed.ToString(), GUILayout.MaxWidth(200)));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Inspector");
                        ((Voxeme)target).turnSpeed = turnSpeed;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Default Move Speed", GUILayout.Width(120));
                    EditorGUI.BeginChangeCheck();
                    float defaultMoveSpeed = ((Voxeme)target).defaultMoveSpeed;
                    defaultMoveSpeed = Convert.ToSingle(GUILayout.TextField(moveSpeed.ToString(), GUILayout.MaxWidth(200)));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Inspector");
                        ((Voxeme)target).defaultMoveSpeed = defaultMoveSpeed;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Default Turn Speed", GUILayout.Width(120));
                    EditorGUI.BeginChangeCheck();
                    float defaultTurnSpeed = ((Voxeme)target).defaultTurnSpeed;
                    defaultTurnSpeed = Convert.ToSingle(GUILayout.TextField(defaultTurnSpeed.ToString(), GUILayout.MaxWidth(200)));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Inspector");
                        ((Voxeme)target).defaultTurnSpeed = defaultTurnSpeed;
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Min Y Bound", GUILayout.Width(120));
                    ((Voxeme)target).minYBound = Convert.ToSingle(GUILayout.TextField(((Voxeme)target).minYBound.ToString(),
                        GUILayout.MaxWidth(200)));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Supporting Surface", GUILayout.Width(120));
                    GUILayout.TextField(((Voxeme)target).supportingSurface == null ? "None" :
                        ((Voxeme)target).supportingSurface.ToString(), GUILayout.MaxWidth(200));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Is Grasped", GUILayout.Width(120));
                    ((Voxeme)target).isGrasped = GUILayout.Toggle(((Voxeme)target).isGrasped, string.Empty, GUILayout.MaxWidth(200));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Grasp Tracker", GUILayout.Width(120));
                    GUILayout.TextField(((Voxeme)target).graspTracker == null ? "None" :
                        ((Voxeme)target).graspTracker.ToString(), GUILayout.MaxWidth(200));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Grasper Coord", GUILayout.Width(120));
                    GUILayout.TextField(((Voxeme)target).grasperCoord == null ? "None" :
                        ((Voxeme)target).grasperCoord.ToString(), GUILayout.MaxWidth(200));
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Children", bold);
                    if (((Voxeme)target).children != null) {
                        foreach (Voxeme item in ((Voxeme)target).children) {
                            GUILayout.BeginHorizontal();
                            GUILayout.TextField(item.ToString(), GUILayout.MaxWidth(200));
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.Label("Parent To Child Position Offset", bold);
                    if (((Voxeme)target).parentToChildPositionOffset != null) {
                        foreach (GameObject item in ((Voxeme)target).parentToChildPositionOffset.Keys) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(item.name, GUILayout.Width(120));
                            GUILayout.Label("X");
                            GUILayout.TextField(((Voxeme)target).parentToChildPositionOffset[item].x.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("Y");
                            GUILayout.TextField(((Voxeme)target).parentToChildPositionOffset[item].y.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("Z");
                            GUILayout.TextField(((Voxeme)target).parentToChildPositionOffset[item].z.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.Label("Parent To Child Rotation Offset", bold);
                    if (((Voxeme)target).parentToChildRotationOffset != null) {
                        foreach (GameObject item in ((Voxeme)target).parentToChildRotationOffset.Keys) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(item.name, GUILayout.Width(120));
                            GUILayout.Label("X");
                            GUILayout.TextField(((Voxeme)target).parentToChildRotationOffset[item].x.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("Y");
                            GUILayout.TextField(((Voxeme)target).parentToChildRotationOffset[item].y.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("Z");
                            GUILayout.TextField(((Voxeme)target).parentToChildRotationOffset[item].z.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.Label("W");
                            GUILayout.TextField(((Voxeme)target).parentToChildRotationOffset[item].w.ToString(), GUILayout.MaxWidth(60));
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Rigidbodies Out Of Sync", GUILayout.Width(120));
                    ((Voxeme)target).rigidbodiesOutOfSync = GUILayout.Toggle(((Voxeme)target).rigidbodiesOutOfSync, string.Empty, GUILayout.MaxWidth(200));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Start Position", GUILayout.Width(120));
                    GUILayout.Label("X");
                    ((Voxeme)target).startPosition = new Vector3(
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).startPosition.x.ToString(), GUILayout.MaxWidth(60))),
                        ((Voxeme)target).startPosition.y, ((Voxeme)target).startPosition.z);
                    GUILayout.Label("Y");
                    ((Voxeme)target).startPosition = new Vector3(((Voxeme)target).startPosition.x,
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).startPosition.y.ToString(), GUILayout.MaxWidth(60))),
                        ((Voxeme)target).startPosition.z);
                    GUILayout.Label("Z");
                    ((Voxeme)target).startPosition = new Vector3(((Voxeme)target).startPosition.x, ((Voxeme)target).startPosition.y,
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).startPosition.z.ToString(), GUILayout.MaxWidth(60))));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Start Rotation", GUILayout.Width(120));
                    GUILayout.Label("X");
                    ((Voxeme)target).startRotation = new Vector3(
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).startRotation.x.ToString(), GUILayout.MaxWidth(60))),
                        ((Voxeme)target).startRotation.y, ((Voxeme)target).startRotation.z);
                    GUILayout.Label("Y");
                    ((Voxeme)target).startRotation = new Vector3(((Voxeme)target).startRotation.x,
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).startRotation.y.ToString(), GUILayout.MaxWidth(60))),
                        ((Voxeme)target).startRotation.z);
                    GUILayout.Label("Z");
                    ((Voxeme)target).startRotation = new Vector3(((Voxeme)target).startRotation.x, ((Voxeme)target).startRotation.y,
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).startRotation.z.ToString(), GUILayout.MaxWidth(60))));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Start Scale", GUILayout.Width(120));
                    GUILayout.Label("X");
                    ((Voxeme)target).startScale = new Vector3(
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).startScale.x.ToString(), GUILayout.MaxWidth(60))),
                        ((Voxeme)target).startScale.y, ((Voxeme)target).startScale.z);
                    GUILayout.Label("Y");
                    ((Voxeme)target).startScale = new Vector3(((Voxeme)target).startScale.x,
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).startScale.y.ToString(), GUILayout.MaxWidth(60))),
                        ((Voxeme)target).startScale.z);
                    GUILayout.Label("Z");
                    ((Voxeme)target).startScale = new Vector3(((Voxeme)target).startScale.x, ((Voxeme)target).startScale.y,
                        Convert.ToSingle(GUILayout.TextField(((Voxeme)target).startScale.z.ToString(), GUILayout.MaxWidth(60))));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Default Parent", GUILayout.Width(120));
                    GUILayout.TextField(((Voxeme)target).defaultParent == null ? "Null" :
                        ((Voxeme)target).defaultParent.ToString(), GUILayout.MaxWidth(120));
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Operational Vox", bold);
                    if (GUILayout.Button(((Voxeme)target).showOpVox ? "Hide" : "Show", GUILayout.MaxWidth(60))) {
                        ((Voxeme)target).showOpVox = !((Voxeme)target).showOpVox;
                    }

                    if ((((Voxeme)target).showOpVox) && (((Voxeme)target).opVox != null)) {
                        GUILayout.BeginVertical();
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("PRED", GUILayout.Width(120));
                        GUILayout.Label(((Voxeme)target).opVox.Lex.Pred);
                        GUILayout.EndHorizontal();

                        GUILayout.Label("TYPE", GUILayout.Width(120));
                        GUILayout.Label("COMPONENTS", GUILayout.Width(120));
                        foreach (Triple<string, GameObject, int> component in ((Voxeme)target).opVox.Type.Components) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Name:");
                            GUILayout.Label(component.Item1);
                            GUILayout.Label("GameObject:");
                            GUILayout.Label(component.Item2.ToString());
                            GUILayout.Label("Index:");
                            GUILayout.Label(component.Item3.ToString());
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.Label("CONCAVITY", GUILayout.Width(120));
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Name:");
                        GUILayout.Label(((Voxeme)target).opVox.Type.Concavity.Item1);
                        GUILayout.Label("GameObject:");
                        GUILayout.Label(((Voxeme)target).opVox.Type.Concavity.Item2.ToString());
                        GUILayout.Label("Index:");
                        GUILayout.Label(((Voxeme)target).opVox.Type.Concavity.Item3.ToString());
                        GUILayout.EndHorizontal();

                        GUILayout.Label("SYMMETRY", GUILayout.Width(120));
                        GUILayout.Label("ROT", GUILayout.Width(120));
                        GUILayout.BeginHorizontal();
                        foreach (string s in ((Voxeme)target).opVox.Type.RotatSym) {
                            GUILayout.Label(s);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.Label("REFL", GUILayout.Width(120));
                        GUILayout.BeginHorizontal();
                        foreach (string s in ((Voxeme)target).opVox.Type.ReflSym) {
                            GUILayout.Label(s);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.Label("HABITATS", GUILayout.Width(120));
                        GUILayout.Label("INTRINSIC", GUILayout.Width(120));
                        foreach (KeyValuePair<int, List<string>> kv in ((Voxeme)target).opVox.Habitat.IntrinsicHabitats) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Index:");
                            GUILayout.Label(kv.Key.ToString());
                            GUILayout.BeginVertical();
                            foreach (string formula in kv.Value) {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Formula:");
                                GUILayout.Label(formula);
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.Label("EXTRINSIC", GUILayout.Width(120));
                        foreach (KeyValuePair<int, List<string>> kv in ((Voxeme)target).opVox.Habitat.ExtrinsicHabitats) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Index:");
                            GUILayout.Label(kv.Key.ToString());
                            GUILayout.BeginVertical();
                            foreach (string formula in kv.Value) {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Formula:");
                                GUILayout.Label(formula);
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.Label("AFFORDANCES", GUILayout.Width(120));
                        foreach (KeyValuePair<int, List<Pair<string, Pair<string, string>>>> kv in ((Voxeme)target).opVox.Affordance.Affordances) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Habitat Index:");
                            GUILayout.Label(kv.Key.ToString());
                            GUILayout.BeginVertical();
                            foreach (Pair<string, Pair<string, string>> affordance in kv.Value) {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Condition: " + ((affordance.Item1 != "") ? affordance.Item1 : "None"));
                                GUILayout.Label("Events: " + affordance.Item2.Item1);
                                GUILayout.Label("Result: " + ((affordance.Item2.Item2 != "") ? affordance.Item2.Item2 : "None"));
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.EndVertical();
                    }
                }
            }
#endif

            // Use this for initialization
            void Start() {
                Debug.Log(string.Format("Starting voxeme {0}", gameObject.name));

                if (GameObject.Find("VoxWorld") == null) {
                    Debug.LogError("Could not find VoxWorld object!  Unable to load VoxML!");
                    return;
                }
          
                library = GameObject.Find("VoxWorld").GetComponent<VoxMLLibrary>();
                if (library == null) {
                    Debug.LogError("Could not find VoxMLLibrary on VoxWorld object!  Unable to load VoxML!");
                    return;
                }

                LoadVoxML();

                supportingSurface = null;

                // get movement blocking
                minYBound = GlobalHelper.GetObjectWorldSize(gameObject).min.y;

                // get rigging components
                rigging = gameObject.GetComponent<Rigging>();
                if (rigging != null) {
                    rigging.rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
                }

                startPosition = transform.position;
                startRotation = transform.eulerAngles;
                startScale = transform.localScale;

                targetPosition = transform.position;
                targetRotation = transform.eulerAngles;
                targetScale = transform.localScale;

                parentToChildPositionOffset = new Dictionary<GameObject, Vector3>();
                parentToChildRotationOffset = new Dictionary<GameObject, Quaternion>();
                children = GetComponentsInChildren<Voxeme>();
                foreach (Voxeme child in children) {
                    if (child.isActiveAndEnabled) {
                        if (child.gameObject != gameObject) {
                            //Debug.Log (child.transform);
                            parentToChildPositionOffset[child.gameObject] = child.transform.localPosition;
                            parentToChildRotationOffset[child.gameObject] = child.transform.localRotation;
                            //Debug.Log (parentToChildPositionOffset [child.gameObject]);
                        }
                    }
                }

                Debug.Log(gameObject);
                Debug.Log(GlobalHelper.VectorToParsable(GlobalHelper.GetObjectWorldSize(gameObject).min));
                Debug.Log(GlobalHelper.VectorToParsable(GlobalHelper.GetObjectWorldSize(gameObject).max));
            }

            // Update is called once per frame
            void Update() {
                if (interTargetPositions.Count == 0) {
                    // no queued path
                    if (!GlobalHelper.VectorIsNaN(targetPosition)) {
                        // has valid destination
                        if (!isGrasped) {
                            if (transform.position != targetPosition) {
                                Vector3 offset = MoveToward(targetPosition);

                                //if (offset.sqrMagnitude <= Constants.EPSILON) {
                                //    transform.position = targetPosition;

                                //    foreach (Voxeme child in children) {
                                //        if (child.isActiveAndEnabled) {
                                //            if (child.gameObject != gameObject) {
                                //                child.transform.localPosition = parentToChildPositionOffset[child.gameObject];
                                //                child.targetPosition = child.transform.position;
                                //            }
                                //        }
                                //    }
                                //}
                            }
                        }
                        else {
                            GraspScript graspController = grasperCoord.root.gameObject.GetComponent<GraspScript>();
                            if (graspTracker.transform.position != targetPosition + graspController.graspTrackerOffset) {
                                Vector3 offset = MoveToward(targetPosition + graspController.graspTrackerOffset);

                                //if (offset.sqrMagnitude <= Constants.EPSILON) {
                                //    graspTracker.transform.position = targetPosition + graspController.graspTrackerOffset;
                                //}
                            }
                        }
                    }
                    else {
                        // cannot execute motion
                        OutputHelper.PrintOutput(Role.Affector, "I'm sorry, I can't do that.");
                        GameObject.Find("BehaviorController").GetComponent<EventManager>().SendMessage("AbortEvent");
                        targetPosition = transform.position;
                    }
                }
                else {
                    Vector3 interimTarget = interTargetPositions.ElementAt(0);
                    if (!isGrasped) {
                        //if (transform.position != interimTarget) {
                        Vector3 offset = MoveToward(interimTarget);

                        if (offset.sqrMagnitude <= Constants.EPSILON) {
                            //transform.position = interimTarget;

                            //foreach (Voxeme child in children) {
                            //    if (child.isActiveAndEnabled) {
                            //        if (child.gameObject != gameObject) {
                            //            child.transform.localPosition = parentToChildPositionOffset[child.gameObject];
                            //            child.targetPosition = child.transform.position;
                            //        }
                            //    }
                            //}

                            Debug.Log(string.Format("Removing {0} from {1}.interTargetPositions", GlobalHelper.VectorToParsable(interimTarget), gameObject.name));
                            interTargetPositions.RemoveFirst();
                        }

                        //}
                    }
                    else {
                        GraspScript graspController = grasperCoord.root.gameObject.GetComponent<GraspScript>();
                        //if (graspTracker.transform.position != interimTarget+graspController.graspTrackerOffset) {
                        Vector3 offset = MoveToward(interimTarget + graspController.graspTrackerOffset);

                        if (offset.sqrMagnitude <= Constants.EPSILON) {
                            //graspTracker.transform.position = interimTarget; //+graspController.graspTrackerOffset;
                            interTargetPositions.RemoveFirst();
                        }

                        //}
                    }
                }

                if (interTargetRotations.Count == 0) {
                    // no queued sequence
                    if (!GlobalHelper.VectorIsNaN(targetRotation)) {
                        // has valid target
                        if (!isGrasped) {
                            if (transform.rotation != Quaternion.Euler(targetRotation)) {
                                //Debug.Log (transform.eulerAngles);
                                float offset = RotateToward(targetRotation);

                                if ((Mathf.Deg2Rad * offset) < 0.01f) {
                                    transform.rotation = Quaternion.Euler(targetRotation);

        //                            foreach (Rigidbody rigidbody in rigging.rigidbodies) {
        //                                if (rotationalDisplacement.ContainsKey (rigidbody.gameObject)) {
        //                                    rigidbody.transform.localEulerAngles = rotationalDisplacement [rigidbody.gameObject];
        //                                }
        //                            }
                                }
                            }
                        }
                        else {
                            // grasp tracking
                        }
                    }
                    else {
                        // cannot execute motion
                        OutputHelper.PrintOutput(Role.Affector, "I'm sorry, I can't do that.");
                        GameObject.Find("BehaviorController").GetComponent<EventManager>().SendMessage("AbortEvent");
                        targetRotation = transform.eulerAngles;
                    }
                }
                else {
                    Vector3 interimTarget = interTargetRotations.ElementAt(0);
                    if (!isGrasped) {
                        if (transform.rotation != Quaternion.Euler(interimTarget)) {
                            //Debug.Log (transform.rotation == Quaternion.Euler (targetRotation));
                            float offset = RotateToward(interimTarget);
                            //Debug.Log (offset);
                            //Debug.Log (Quaternion.Angle(transform.rotation,Quaternion.Euler (interimTarget)));
                            //if ((Mathf.Deg2Rad * Quaternion.Angle (transform.rotation, Quaternion.Euler (interimTarget))) < 0.01f) {
                            if ((Mathf.Deg2Rad * offset) < 0.01f) {
                                transform.rotation = Quaternion.Euler(interimTarget);

                                //Debug.Log (interimTarget);
                                interTargetRotations.RemoveFirst();
                                //Debug.Log (interTargetRotations.Peek ());
                            }
                        }
                    }
                    else {
                        // grasp tracking
                    }
                }

                if ((transform.localScale != targetScale) && (!isGrasped)) {
                    Vector3 offset = transform.localScale - targetScale;
                    Vector3 normalizedOffset = Vector3.Normalize(offset);

                    transform.localScale = new Vector3(transform.localScale.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
                        transform.localScale.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
                        transform.localScale.z - normalizedOffset.z * Time.deltaTime * moveSpeed);

                    if (offset.sqrMagnitude <= 0.01f) {
                        transform.localScale = targetScale;
                    }
                }

                // Don't let the object sink below supporting surface
                //Comment out line below. Not needed. *BC 16NOV21
                //AdjustToSupportingSurface();

                if (rigging != null) {
                    if (rigging.usePhysicsRig) {
                        return;
                    }
                }

                if (supportingSurface != null) {
                    //Debug.Log (supportingSurface.name);
                    // add check for SupportingSurface component
                    Bounds surfaceBounds = GlobalHelper.GetObjectWorldSize(supportingSurface);
                    Bounds objectBounds = GlobalHelper.GetObjectWorldSize(gameObject);
        //            Renderer[] renderers = supportingSurface.GetComponentsInChildren<Renderer> ();
        //            Bounds surfaceBounds = new Bounds ();
        //            foreach (Renderer renderer in renderers) {
        //                if (renderer.bounds.max.y > surfaceBounds.max.y) {
        //                    surfaceBounds = renderer.bounds;
        //                }
        //            }

        //            Vector3 currentMin = gameObject.transform.position;
        //            renderers = gameObject.GetComponentsInChildren<Renderer> ();
        //            Bounds objectBounds = new Bounds ();
        //            foreach (Renderer renderer in renderers) {
        //                if (renderer.bounds.max.y > objectBounds.max.y) {
        //                    objectBounds = renderer.bounds;
        //                }
        //
        //                if (renderer.bounds.min.y < currentMin.y) {
        //                    currentMin = renderer.bounds.min;
        //                }
        //            }

                    if (GlobalHelper.IsTopmostVoxemeInHierarchy(gameObject)) {
                        if (objectBounds.min.y < minYBound) {
                            transform.position = new Vector3(transform.position.x,
                                transform.position.y + (minYBound - objectBounds.min.y),
                                transform.position.z);
                        }
                    }

                    /*if (supportingSurface.GetComponent<SupportingSurface> ().surfaceType == SupportingSurface.SupportingSurfaceType.Concave) {
                        /*if (objectBounds.min.y < surfaceBounds.min.y) {
                        transform.position = new Vector3 (transform.position.x,
                                                          transform.position.y + (surfaceBounds.min.y - objectBounds.min.y),
                                                          transform.position.z);
                    }*/
                    /*if (currentMin.y < surfaceBounds.min.y) {
                        transform.position = new Vector3 (transform.position.x,
                            transform.position.y + (surfaceBounds.min.y - currentMin.y),
                            transform.position.z);
                    }
                } else {
                    /*if (objectBounds.min.y < surfaceBounds.max.y) {
                    transform.position = new Vector3 (transform.position.x,
                                         transform.position.y + (surfaceBounds.max.y - objectBounds.min.y),
                                         transform.position.z);
                }*/
                    /*if (currentMin.y < surfaceBounds.max.y) {
                        transform.position = new Vector3 (transform.position.x,
                            transform.position.y + (surfaceBounds.max.y - currentMin.y),
                            transform.position.z);
                    }
                }*/
                }

                // check relationships
            }

            void FixedUpdate() {
                //foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
                //{
                //    Debug.Log(string.Format("Voxeme.FixedUpdate: sync {0} {1} {2}",
                //                rb.name, GlobalHelper.VectorToParsable(transform.position),
                //                GlobalHelper.VectorToParsable(rb.transform.position)));
                //}

                if (rigidbodiesOutOfSync) {
                    Debug.Log(string.Format("Voxeme.FixedUpdate: {0} rigidbodies out of sync", name));

                    Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
                    foreach (Rigidbody rb in rigidbodies)
                    {
                        if (displacement.ContainsKey(rb.gameObject))
                        {
                            Debug.Log(string.Format("Voxeme.FixedUpdate: Before sync {0} {1} {2} {3}",
                                rb.name, GlobalHelper.VectorToParsable(transform.position),
                                GlobalHelper.VectorToParsable(rb.transform.position),
                                GlobalHelper.VectorToParsable(rb.transform.localPosition)));
                            transform.position = rb.transform.position - displacement[rb.gameObject];
                            rb.transform.localPosition = displacement[rb.gameObject];
                            targetPosition = transform.position;
                            Debug.Log(string.Format("Voxeme.FixedUpdate: After sync {0} {1} {2} {3}",
                                rb.name, GlobalHelper.VectorToParsable(transform.position),
                                GlobalHelper.VectorToParsable(rb.transform.position),
                                GlobalHelper.VectorToParsable(rb.transform.localPosition)));
                        }

                        if (rotationalDisplacement.ContainsKey(rb.gameObject))
                        {
                            Debug.Log(string.Format("Voxeme.FixedUpdate: Before sync {0} {1} {2} {3}",
                                rb.name, GlobalHelper.VectorToParsable(transform.eulerAngles),
                                GlobalHelper.VectorToParsable(rb.transform.eulerAngles),
                                GlobalHelper.VectorToParsable(rb.transform.localEulerAngles)));
                            transform.eulerAngles = rb.transform.eulerAngles - rotationalDisplacement[rb.gameObject];
                            rb.transform.localEulerAngles = rotationalDisplacement[rb.gameObject];
                            targetRotation = transform.eulerAngles;
                            Debug.Log(string.Format("Voxeme.FixedUpdate: After sync {0} {1} {2} {3}",
                                rb.name, GlobalHelper.VectorToParsable(transform.eulerAngles),
                                GlobalHelper.VectorToParsable(rb.transform.eulerAngles),
                                GlobalHelper.VectorToParsable(rb.transform.localEulerAngles)));
                        }
                    }

                    rigidbodiesOutOfSync = false;
                }
            }

            void AdjustToSupportingSurface() {
                Vector3 rayStartX = new Vector3(GlobalHelper.GetObjectWorldSize(gameObject).min.x - Constants.EPSILON,
                    GlobalHelper.GetObjectWorldSize(gameObject).min.y + Constants.EPSILON,
                    GlobalHelper.GetObjectWorldSize(gameObject).center.z);
                Vector3 contactPointX = GlobalHelper.RayIntersectionPoint(rayStartX, Vector3.right);
                //contactPointX = new Vector3 (contactPointX.x, transform.position.y, contactPointX.z);

                Vector3 rayStartZ = new Vector3(GlobalHelper.GetObjectWorldSize(gameObject).center.x,
                    GlobalHelper.GetObjectWorldSize(gameObject).min.y + Constants.EPSILON,
                    GlobalHelper.GetObjectWorldSize(gameObject).min.z - Constants.EPSILON);
                Vector3 contactPointZ = GlobalHelper.RayIntersectionPoint(rayStartZ, Vector3.forward);
                //contactPointZ = new Vector3 (contactPointZ.x, transform.position.y, contactPointZ.z);

                Vector3 contactPoint = (contactPointZ.y < contactPointX.y)
                    ? new Vector3(contactPointZ.x, transform.position.y, contactPointZ.z)
                    : new Vector3(contactPointX.x, transform.position.y, contactPointX.z);

                bool grasped = false;
                /*InteractionObject interactionObject = gameObject.GetComponent<InteractionObject>();
                if ((interactionObject != null) && (interactionObject.lastUsedInteractionSystem != null)) {
                    grasped = interactionObject.lastUsedInteractionSystem.IsPaused(FullBodyBipedEffector.LeftHand) ||
                              interactionObject.lastUsedInteractionSystem.IsPaused(FullBodyBipedEffector.RightHand);
                }*/

                RaycastHit[] hits;

                //        hits = Physics.RaycastAll (transform.position, AxisVector.negYAxis);
                hits = Physics.RaycastAll(contactPoint, AxisVector.negYAxis);
                List<RaycastHit> hitList = new List<RaycastHit>(hits);
                hits = hitList.OrderBy(h => h.distance).ToArray();
                foreach (RaycastHit hit in hits) {
                    if (hit.collider.gameObject.GetComponent<BoxCollider>() != null) {
                        if ((!hit.collider.gameObject.GetComponent<BoxCollider>().isTrigger) &&
                            (!hit.collider.gameObject.transform.IsChildOf(gameObject.transform))) {
                            //TODO: under what circumstances does the following check matter?
                            if (!GlobalHelper.FitsIn(GlobalHelper.GetObjectWorldSize(hit.collider.gameObject),
                                GlobalHelper.GetObjectWorldSize(gameObject), true)) {
                                supportingSurface = hit.collider.gameObject;

                                //if (!grasped) {
                                bool themeIsConcave = (GlobalHelper.GetMostImmediateParentVoxeme(gameObject)
                                    .GetComponent<Voxeme>().voxml.Type.Concavity.Contains("Concave"));
                                bool themeIsUpright =
                                    (Vector3.Dot(gameObject.transform.root.transform.up, Vector3.up) > 0.5f);
                                bool themeIsUpsideDown =
                                    (Vector3.Dot(gameObject.transform.root.transform.up, Vector3.up) < -0.5f);

                                bool supportIsConcave = (GlobalHelper.GetMostImmediateParentVoxeme(supportingSurface)
                                    .GetComponent<Voxeme>().voxml.Type.Concavity.Contains("Concave"));
                                bool supportIsUpright =
                                    (Vector3.Dot(supportingSurface.transform.root.transform.up, Vector3.up) > 0.5f);
                                bool supportIsUpsideDown =
                                    (Vector3.Dot(supportingSurface.transform.root.transform.up, Vector3.up) < -0.5f);

                                // if theme is concave, the concavity isn't enabled, and the object is on top of an object that fits inside of it
                                // e.g. cup on top of ball
                                if ((themeIsConcave) &&
                                    (Concavity.IsEnabled(GlobalHelper.GetMostImmediateParentVoxeme(gameObject))) &&
                                    (GlobalHelper.FitsIn(GlobalHelper.GetObjectWorldSize(supportingSurface.transform.root.gameObject),
                                        GlobalHelper.GetObjectWorldSize(gameObject)))) {
                                    minYBound = GlobalHelper.GetObjectWorldSize(supportingSurface).min.y;
                                    //flip the plate.  flip the cup.  put the plate under the cup
                                    //flip the cup.  put the ball under the cup
                                }
                                else {
                                    // otherwise
                                    if (supportIsConcave) {
                                        // if the object under this object is concave
                                        if (Concavity.IsEnabled(GlobalHelper.GetMostImmediateParentVoxeme(supportingSurface))) {
                                            // if the object under this object has its concavity enabled
                                            minYBound = PhysicsHelper.GetConcavityMinimum(supportingSurface.transform.root
                                                .gameObject);
                                            //                                        Debug.Log (gameObject.name);
                                            //                                        Debug.Log (supportingSurface.name);
                                            //                                        Debug.Log (minYBound);
                                            //Debug.Break ();
                                        }
                                        else {
                                            // if the object under this object is not upright
                                            //Debug.Break ();
                                            minYBound = GlobalHelper.GetObjectWorldSize(supportingSurface).max.y;
                                            //                                Debug.Log (minYBound);
                                            //Debug.Log (minYBound);
                                        }
                                    }
                                    else {
                                        // if the object under this object is not concave
                                        minYBound = GlobalHelper.GetObjectWorldSize(supportingSurface).max.y;
                                        //                            Debug.Log (minYBound);
                                        //Debug.Break ();
                                    }

                                    //**
                                    //Bug list:
                                    // put the plate under the cup jitter
                                }

                                break;
                            }
                            //}
                        }
                    }
                }
            }

            public void ResetVoxeme() {
                if (gameObject.transform.parent != null) {
                    GameObject parent = gameObject.transform.parent.gameObject;
                    Voxeme parentVox = GlobalHelper.GetMostImmediateParentVoxeme(parent).GetComponent<Voxeme>();

                    // if this voxeme is not (intentionally) a subcomponent of another voxeme object
                    if ((parentVox != null) && (parentVox.opVox != null)) {
                        if (!(parentVox.opVox.Type.Components.Select(i => i.Item2).ToList()).Contains(gameObject)) {
                            RiggingHelper.UnRig(gameObject, gameObject.transform.parent.gameObject);
                        }
                    }
                }

                moveSpeed = defaultMoveSpeed;
                turnSpeed = defaultTurnSpeed;

                transform.position = startPosition;
                transform.eulerAngles = startRotation;
                transform.localScale = startScale;

                targetPosition = startPosition;
                targetRotation = startRotation;
                targetScale = startScale;
            }

            public Vector3 MoveToward(Vector3 target) {
                Debug.Log (string.Format("{0}: at {1}, moving toward {2}",gameObject.name,
                    GlobalHelper.VectorToParsable(transform.position),GlobalHelper.VectorToParsable(target)));
                if (!isGrasped) {
                    Vector3 offset = transform.position - target;
                    Vector3 normalizedOffset = Vector3.Normalize(offset);

                    Debug.Log (string.Format("{0}: {1}, {2}",gameObject.name,
                        GlobalHelper.VectorToParsable(offset),GlobalHelper.VectorToParsable(normalizedOffset)));

                    float t = (Time.deltaTime * moveSpeed) / offset.magnitude;

                    if (rigging.usePhysicsRig) {
                        //Rigidbody rb = gameObject.GetComponent<Rigidbody>();
                        //if (rb != null) {
                        //    rb.MovePosition(Vector3.Lerp(transform.position, target, t));
                        //}

                        Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
                        foreach (Rigidbody rigidbody in rigidbodies) {
                            //rigidbody.MovePosition(new Vector3(
                            //    transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
                            //    transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
                            //    transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed));
                            rigidbody.MovePosition(Vector3.Lerp(transform.position, target, t));
                        }
                    }

                    transform.position = Vector3.Lerp(transform.position, target, t);

                    //transform.position = new Vector3(transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
                    //    transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
                    //    transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed);

                    Debug.Log (string.Format("offset.sqrMagnitude({0}) = {1}",gameObject.name,
                        offset.sqrMagnitude));

                    if (offset.sqrMagnitude <= Constants.EPSILON) {
                        transform.position = target;
                    }

                    foreach (Voxeme child in children) {
                        if (child.isActiveAndEnabled) {
                            if (child.gameObject != gameObject) {
                                //Debug.Log ("Moving child: " + gameObject.name);
                                child.transform.localPosition = parentToChildPositionOffset[child.gameObject];
                                child.targetPosition = child.transform.position;
                            }
                        }
                    }

                    Debug.Log (string.Format("{0}: {1}, {2}",gameObject.name,
                        GlobalHelper.VectorToParsable(transform.position), GlobalHelper.VectorToParsable(target)));

                    return offset;
                }
                else {
                    Vector3 offset = graspTracker.transform.position - target;
                    Vector3 normalizedOffset = Vector3.Normalize(offset);

                    /*if (rigging.usePhysicsRig) {
                        Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody> ();
                        foreach (Rigidbody rigidbody in rigidbodies) {
                            rigidbody.MovePosition (new Vector3 (transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
                                transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
                                transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed));
                        }
                    }*/

                    graspTracker.transform.position = new Vector3(
                        graspTracker.transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
                        graspTracker.transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
                        graspTracker.transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed);

                    return offset;
                }
            }

            public float RotateToward(Vector3 target) {
                float offset = 0.0f;
                if (!isGrasped) {
                    //Quaternion offset = Quaternion.FromToRotation (transform.eulerAngles, targetRotation);
                    //Vector3 normalizedOffset = Vector3.Normalize (offset);

                    float angle = Quaternion.Angle(transform.rotation, Quaternion.Euler(target));
                    float timeToComplete = angle / turnSpeed;
                    float donePercentage = Mathf.Min(1.0f, Time.deltaTime / timeToComplete);
                    Quaternion rot = Quaternion.Slerp(transform.rotation, Quaternion.Euler(target), donePercentage * 100.0f);
                    //Debug.Log (turnSpeed);
                    //Quaternion resolve = Quaternion.identity;

                    if (rigging.usePhysicsRig) {
                        float displacementAngle = 360.0f;
                        //Rigidbody rb = gameObject.GetComponent<Rigidbody>();
                        //if (rb != null) {
                        //    rb.MoveRotation(rot);
                        //}
                        Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
                        foreach (Rigidbody rigidbody in rigidbodies) {
                            rigidbody.MoveRotation(rot);
                        }
                    }

                    transform.rotation = rot;
                    //GameObject.Find ("ReachObject").transform.position = transform.position;

                    foreach (Voxeme child in children) {
                        if (child.isActiveAndEnabled) {
                            if (child.gameObject != gameObject) {
                                child.transform.localRotation = parentToChildRotationOffset[child.gameObject];
                                child.transform.rotation = gameObject.transform.rotation * child.transform.localRotation;
                                child.targetRotation = child.transform.rotation.eulerAngles;
                                child.transform.localPosition = GlobalHelper.RotatePointAroundPivot(
                                    parentToChildPositionOffset[child.gameObject],
                                    Vector3.zero, gameObject.transform.eulerAngles);
                                child.transform.position = gameObject.transform.position + child.transform.localPosition;
                                child.targetPosition = child.transform.position;
                                //Debug.Log (child.name);
                                //Debug.Break ();
                                //Debug.Log (Helper.VectorToParsable(child.transform.localPosition));
                            }
                        }
                    }

                    offset = Quaternion.Angle(rot, Quaternion.Euler(target));
                    //Debug.Log (offset);
                }
                else {
                    //float offset = Quaternion.FromToRotation (transform.eulerAngles, targetRotation);//graspTracker.transform.position - target;
                    //Vector3 normalizedOffset = Vector3.Normalize (offset);

                    /*if (rigging.usePhysicsRig) {
                            Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody> ();
                            foreach (Rigidbody rigidbody in rigidbodies) {
                                rigidbody.MovePosition (new Vector3 (transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
                                    transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
                                    transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed));
                            }
                        }*/

                    /*graspTracker.transform.position = new Vector3 (graspTracker.transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
                        graspTracker.transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
                        graspTracker.transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed);*/
                }

                return offset;
            }

            /// <summary>
            /// Triggered when the gameObject's target position changes
            /// </summary>
            // IN: oldVal -- previous value of gameObject's targetPosition
            //      newVal -- new or current value of gameObject's targetPosition
            void OnTargetPositionChanged(Vector3 oldVal, Vector3 newVal) {
                Debug.Log(string.Format("==================== Target position changed ==================== {0}: {1}->{2}",
                    gameObject.name, GlobalHelper.VectorToParsable(oldVal), GlobalHelper.VectorToParsable(newVal)));
            }

            /// <summary>
            /// Triggered when the gameObject's target rotation changes
            /// </summary>
            // IN: oldVal -- previous value of gameObject's targetRotation
            //      newVal -- new or current value of gameObject's targetRotation
            void OnTargetRotationChanged(Vector3 oldVal, Vector3 newVal) {
                Debug.Log(string.Format("==================== Target rotation changed ==================== {0}: {1}->{2}",
                    gameObject.name, GlobalHelper.VectorToParsable(oldVal), GlobalHelper.VectorToParsable(newVal)));
            }

            /// <summary>
            /// Triggered when the gameObject's target scale changes
            /// </summary>
            // IN: oldVal -- previous value of gameObject's targetScale
            //      newVal -- new or current value of gameObject's targetScale
            void OnTargetScaleChanged(Vector3 oldVal, Vector3 newVal) {
                Debug.Log(string.Format("==================== Target scale changed ==================== {0}: {1}->{2}",
                    gameObject.name, GlobalHelper.VectorToParsable(oldVal), GlobalHelper.VectorToParsable(newVal)));
            }

            void OnCollisionEnter(Collision other) {
                if (other.gameObject.tag == "MainCamera") {
                    return;
                }
            }

            public void LoadVoxML() {
                try {
                    // load the VoxML from the file whose "predicate" field matches the predicate of this object
                    string filename = library.VoxMLPredicateDict.Where(kvp => kvp.Value == predicate).FirstOrDefault().Key;
                    Debug.LogFormat("Loading VoxML (filename {0})", filename);

                    if (library.VoxMLEntityTypeDict.ContainsKey(filename) && library.VoxMLEntityTypeDict[filename] == "objects") {
                        voxml = library.VoxMLObjectDict[filename];
                    }
                }
                catch (FileNotFoundException ex) {
                    voxml = new VoxML();
                    voxml.Entity.Type = VoxEntity.EntityType.Object;
                }

                // populate operational voxeme structure
                PopulateOperationalVoxeme(voxml);
            }

            void PopulateOperationalVoxeme(VoxML v) {
                opVox = new OperationalVox();

                // set entity type
                opVox.VoxemeType = v.Entity.Type;

                // set lex
                opVox.Lex.Pred = v.Lex.Pred;
                //Debug.Log (opVox.Lex.Pred);

                // set type info

                // find component objects
                foreach (VoxTypeComponent c in v.Type.Components) {
                    Regex operators = new Regex(@"[\*\+]");
                    string oper = String.Empty;
                    //Debug.Log (c.Value);
                    if (operators.Match(c.Value).Length > 0) {
                        oper = operators.Match(c.Value).Groups[0].ToString();
                        //Debug.Log (oper);
                    }

                    string[] s;
                    if (oper != string.Empty) {
                        s = c.Value.Remove(c.Value.IndexOf(oper)).Split('[');
                    }
                    else {
                        s = c.Value.Split('[');
                    }

                    if (oper == String.Empty) {
                        Transform obj = null;
                        int index = -1;
                        obj = gameObject.transform.Find(gameObject.name + "*/" + s[0]);
                        if (s.Length > 1) {
                            index = GlobalHelper.StringToInt(s[1].Remove(s[1].IndexOf(']')));
                        }

                        if (obj != null) {
                            opVox.Type.Components.Add(new Triple<string, GameObject, int>(s[0], obj.gameObject, index));
                        }
                    }
                    else if (oper == "+" || oper == "*") {
                        Transform subVox = gameObject.transform.Find(gameObject.name + "*/");
                        if (subVox != null) {
                            foreach (Transform child in subVox) {
                                if (child.name == s[0]) {
                                    int index = -1;
                                    if (s.Length > 1) {
                                        index = GlobalHelper.StringToInt(s[1].Remove(s[1].IndexOf(']')));
                                    }

                                    opVox.Type.Components.Add(
                                        new Triple<string, GameObject, int>(s[0], child.gameObject, index));
                                }
                            }
                        }
                    }
                }

                // set component as semantic head
                string[] str = v.Type.Head.Split('[');
                if (str.Length > 1) {
                    int i = GlobalHelper.StringToInt(str[1].Remove(str[1].IndexOf(']')));
                    if (opVox.Type.Components.FindIndex(c => c.Item3 == i) != -1) {
                        opVox.Type.Head = opVox.Type.Components.First(c => c.Item3 == i);
                    }
                    // if none, add entire game object as semantic head for voxeme
                    else {
                        opVox.Type.Head = new Triple<string, GameObject, int>(gameObject.name, gameObject, i);
                        opVox.Type.Components.Add(new Triple<string, GameObject, int>(gameObject.name, gameObject, i));
                    }
                }
                else {
                    opVox.Type.Head = new Triple<string, GameObject, int>(gameObject.name, gameObject, -1);
                    opVox.Type.Components.Add(new Triple<string, GameObject, int>(gameObject.name, gameObject, -1));
                }

                // set concavity info
                string[] concavity = v.Type.Concavity.Split('[');
                // if reentrancy index given
                if (concavity.Length > 1) {
                    int index = GlobalHelper.StringToInt(concavity[1].Remove(concavity[1].IndexOf(']')));
                    if (opVox.Type.Components.FindIndex(c => c.Item3 == index) != -1) {
                        GameObject obj = opVox.Type.Components.Find(c => c.Item3 == index).Item2;
                        opVox.Type.Concavity = new Triple<string, GameObject, int>(concavity[0], obj, index);
                    }
                    // if none, add entire game object as concavity segment
                    else {
                        opVox.Type.Concavity =
                            new Triple<string, GameObject, int>(concavity[0], gameObject, opVox.Type.Head.Item3);
                    }
                }
                else {
                    opVox.Type.Concavity = new Triple<string, GameObject, int>(concavity[0], gameObject, opVox.Type.Head.Item3);
                }
        //        foreach (string sym in rotsym) {
        //            opVox.Type.RotatSym.Add (sym);
        //        }

                // set symmetry info
                string[] rotsym = v.Type.RotatSym.Split(',');
                foreach (string sym in rotsym) {
                    opVox.Type.RotatSym.Add(sym);
                }

                string[] reflsym = v.Type.ReflSym.Split(',');
                foreach (string sym in reflsym) {
                    opVox.Type.ReflSym.Add(sym);
                }

                // set habitat info
                foreach (VoxHabitatIntr ih in v.Habitat.Intrinsic) {
                    string[] s = ih.Name.Split('[');
                    int index = GlobalHelper.StringToInt(s[1].Remove(s[1].IndexOf(']')));
                    //Debug.Log(index);
                    //Debug.Log (s[0] + " = {" + ih.Value + "}");

                    if (!opVox.Habitat.IntrinsicHabitats.ContainsKey(index)) {
                        opVox.Habitat.IntrinsicHabitats.Add(index, new List<string>() {s[0] + " = {" + ih.Value + "}"});
                    }
                    else {
                        opVox.Habitat.IntrinsicHabitats[index].Add(s[0] + " = {" + ih.Value + "}");
                    }
                }

                foreach (VoxHabitatExtr eh in v.Habitat.Extrinsic) {
                    string[] s = eh.Name.Split('[');
                    int index = Convert.ToInt16(s[1].Remove(s[1].IndexOf(']')));
                    //Debug.Log(index);
                    //Debug.Log (s[0] + " = {" + ih.Value + "}");

                    if (!opVox.Habitat.ExtrinsicHabitats.ContainsKey(index)) {
                        opVox.Habitat.ExtrinsicHabitats.Add(index, new List<string>() {s[0] + " = {" + eh.Value + "}"});
                    }
                    else {
                        opVox.Habitat.ExtrinsicHabitats[index].Add(s[0] + " = {" + eh.Value + "}");
                    }
                }

                /*foreach (KeyValuePair<int,List<string>> kv in opVox.Habitat.IntrinsicHabitats) {
                            Debug.Log (kv.Key);
                            foreach (string s in kv.Value) {
                                Debug.Log (s);
                            }
                        }*/

                // set affordance info
                foreach (VoxAffordAffordance a in v.Afford_Str.Affordances) {
                    //Debug.Log (a.Formula);
                    Regex reentrancyForm = new Regex(@"\[[0-9]+\]");
                    Regex numericalForm = new Regex(@"[0-9]+");
                    string[] s = a.Formula.Split(new string[] {"->"}, StringSplitOptions.None);
                    string[] conditions = s[0].Split(new char[] {','}, 2);
                    MatchCollection reentrancies = reentrancyForm.Matches(s[1]);
                    string aff = "";
                    string cHabitat = "";
                    string cFormula = "";
                    string events = "";
                    string result = "";
                    cHabitat = conditions[0]; // split into habitat and non-habitat condition (if any)
                    cFormula = conditions.Length > 1
                        ? conditions[1]
                        : ""; // split into habitat and non-habitat condition (if any)
                    int index = (cHabitat.Split('[').Length > 1)
                        ? GlobalHelper.StringToInt(cHabitat.Split('[')[1].Remove(cHabitat.Split('[')[1].IndexOf(']')))
                        : 0;

                    //Debug.Log ("Habitat index: " + index.ToString ());
                    foreach (Match match in reentrancies) {
                        GroupCollection groups = match.Groups;
                        foreach (Group group in groups) {
                            aff = s[1].Replace(group.Value, group.Value.Trim(new char[] {'[', ']'}));
                        }
                    }

                    //if (cFormula != "") {
                    //    Debug.Log ("Formula: " + cFormula);
                    //}
                    //Debug.Log ("Affordance: " + aff);

                    events = aff.Split(']')[0].Trim('[');
                    MatchCollection numerical = numericalForm.Matches(events);
                    foreach (Match match in numerical) {
                        GroupCollection groups = match.Groups;
                        foreach (Group group in groups) {
                            events = events.Replace(group.Value, '[' + group.Value + ']');
                        }
                    }
                    //Debug.Log ("Events: " + events);

                    result = aff.Split(']')[1];
                    numerical = numericalForm.Matches(result);
                    foreach (Match match in numerical) {
                        GroupCollection groups = match.Groups;
                        foreach (Group group in groups) {
                            result = result.Replace(group.Value, '[' + group.Value + ']');
                        }
                    }
                    //Debug.Log ("Result: " + result);

                    Pair<string, string> affordance = new Pair<string, string>(events, result);
                    if (!opVox.Affordance.Affordances.ContainsKey(index)) {
                        opVox.Affordance.Affordances.Add(index,
                            new List<Pair<string, Pair<string, string>>>()
                                {new Pair<string, Pair<string, string>>(cFormula, affordance)});
                    }
                    else {
                        opVox.Affordance.Affordances[index].Add(new Pair<string, Pair<string, string>>(cFormula, affordance));
                    }
                }

                opVox.Embodiment.Scale = v.Embodiment.Scale;
                opVox.Embodiment.Movable = v.Embodiment.Movable;

                if (v.Entity.Type == VoxEntity.EntityType.Object) {
                    AttributeSet attrSet = gameObject.GetComponent<AttributeSet>();
                    if (attrSet != null) {
                        //attrSet.attributes.Clear();
                        for (int i = 0; i < v.Attributes.Attrs.Count; i++) {
                            attrSet.attributes.Add(voxml.Attributes.Attrs[i].Value);
                            Debug.Log(attrSet.attributes[i]);
                        }
                    }
                }

                OnVoxMLLoaded(this, new VoxMLEventArgs(gameObject, v));
            }
        }
    }
}