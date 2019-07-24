using UnityEngine;

using VoxSimPlatform.Core;
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace Global {
        /// <summary>
        /// Physics helper.
        /// </summary>
        public static class PhysicsHelper {
            public static void ResolveAllPhysicsDiscrepancies(bool macroEventSatisfied) {
                ObjectSelector objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
                foreach (Voxeme voxeme in objSelector.allVoxemes) {
                    ResolvePhysicsDiscrepancies(voxeme.gameObject, macroEventSatisfied);
                }
            }

            public static void ResolvePhysicsDiscrepancies(GameObject obj, bool macroEventSatisfied) {
                // check and see if rigidbody orientations and main body orientations are getting out of sync
                // due to physics effects
                ResolvePhysicsPositionDiscrepancies(obj, macroEventSatisfied);
                ResolvePhysicsRotationDiscrepancies(obj, macroEventSatisfied);
    //          ResolvePhysicsPositionDiscepancies(obj);
            }

            public static void ResolvePhysicsRotationDiscrepancies(GameObject obj, bool macroEventSatisfied) {
                Voxeme voxComponent = obj.GetComponent<Voxeme>();

                // find the smallest displacement angle between an axis on the main body and an axis on this rigidbody
                float displacementAngle = 360.0f;
                Quaternion rigidbodyRotation = Quaternion.identity;
                Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
                foreach (Rigidbody rigidbody in rigidbodies) {
                    foreach (Vector3 mainBodyAxis in Constants.Axes.Values) {
                        foreach (Vector3 rigidbodyAxis in Constants.Axes.Values) {
                            if (Vector3.Angle(obj.transform.rotation * mainBodyAxis, rigidbody.rotation * rigidbodyAxis) <
                                displacementAngle) {
                                displacementAngle = Vector3.Angle(obj.transform.rotation * mainBodyAxis,
                                    rigidbody.rotation * rigidbodyAxis);
                                rigidbodyRotation = rigidbody.rotation;
                            }
                        }
                    }
                }

                if (displacementAngle == 360.0f) {
                    displacementAngle = 0.0f;
                }

                if (displacementAngle > Mathf.Rad2Deg * Constants.EPSILON) {
                    //Debug.Break ();
                    //Debug.Log (obj.name);
                    //Debug.Log (displacementAngle);
                    Quaternion resolve = Quaternion.identity;
                    Quaternion resolveInv = Quaternion.identity;
                    if (voxComponent != null) {
                        if (rigidbodies.Length > 0) {
    //                      foreach (Rigidbody rigidbody in rigidbodies) {
    //                          if (voxComponent.rotationalDisplacement.ContainsKey (rigidbody.gameObject)) {
    //                              Debug.Log (rigidbody.name);
    //                              // initial = initial rotational displacement
    //                              Quaternion initial = Quaternion.Euler (voxComponent.rotationalDisplacement [rigidbody.gameObject]);
    //                              Debug.Log (initial.eulerAngles);
    //                              // current = current rotational displacement due to physics
    //                              Quaternion current = rigidbody.transform.localRotation;// * Quaternion.Inverse ((args [0] as GameObject).transform.rotation));
    //                              Debug.Log (current.eulerAngles);
    //                              // resolve = rotation to get from initial rotational displacement to current rotational displacement
    //                              resolve = current * Quaternion.Inverse (initial);
    //                              Debug.Log (resolve.eulerAngles);
    //                              Debug.Log ((initial * resolve).eulerAngles);
    //                              Debug.Log ((resolve * initial).eulerAngles);
    //                              // resolveInv = rotation to get from final (current rigidbody) rotation back to initial (aligned with main obj) rotation
    //                              resolveInv = initial * Quaternion.Inverse (current);
    //                              //Debug.Log (resolveInv.eulerAngles);
    //                              //rigidbody.transform.rotation = obj.transform.rotation * initial;
    //                              //rigidbody.transform.localRotation = initial;// * (args [0] as GameObject).transform.rotation;
    //                              //Debug.Log (rigidbody.transform.rotation.eulerAngles);
    //
    //                              //rigidbody.transform.localPosition = voxComponent.displacement [rigidbody.name];
    //                              //rigidbody.transform.position = (args [0] as GameObject).transform.position + voxComponent.displacement [rigidbody.name];
    //                          }
    //                      }

                            //Debug.Break ();

                            //Debug.Log (obj.transform.rotation.eulerAngles);
                            //foreach (Rigidbody rigidbody in rigidbodies) {
                            //Debug.Log (Helper.VectorToParsable (rigidbody.transform.localPosition));
                            //}

    //                      obj.transform.rotation = obj.transform.rotation *
    //                          (rigidbodies [0].transform.localRotation * 
    //                              Quaternion.Inverse (Quaternion.Euler (voxComponent.rotationalDisplacement [rigidbodies [0].gameObject])));
                            //(rigidbodies [0].transform.localRotation *
    //                          obj.transform.rotation * Quaternion.Inverse (Quaternion.Euler (voxComponent.rotationalDisplacement [rigidbodies [0].gameObject])));
                            obj.transform.rotation = rigidbodies[0].transform.rotation *
                                                     Quaternion.Inverse(Quaternion.Euler(
                                                         voxComponent.rotationalDisplacement[rigidbodies[0].gameObject]));
                            voxComponent.targetRotation = obj.transform.rotation.eulerAngles;
                            //Debug.Log (obj.transform.rotation.eulerAngles);

                            foreach (Rigidbody rigidbody in rigidbodies) {
                                if (voxComponent.rotationalDisplacement.ContainsKey(rigidbody.gameObject)) {
                                    //Debug.Log (rigidbody.name);
                                    rigidbody.transform.localEulerAngles =
                                        voxComponent.rotationalDisplacement[rigidbody.gameObject];
                                }
                            }

    //                      rigidbodyRotation = Quaternion.identity;
    //                      rigidbodies = obj.GetComponentsInChildren<Rigidbody> ();
    //                      foreach (Rigidbody rigidbody in rigidbodies) {
    //                          foreach (Vector3 mainBodyAxis in Constants.Axes.Values) {
    //                              foreach (Vector3 rigidbodyAxis in Constants.Axes.Values) {
    //                                  if (Vector3.Angle (obj.transform.rotation * mainBodyAxis, rigidbody.rotation * rigidbodyAxis) < displacementAngle) {
    //                                      displacementAngle = Vector3.Angle (obj.transform.rotation * mainBodyAxis, rigidbody.rotation * rigidbodyAxis);
    //                                      rigidbodyRotation = rigidbody.rotation;
    //                                  }
    //                              }
    //                          }
    //                      }
                        }
                    }
                }

                // TODO: Abstract away
                if (voxComponent != null) {
                    if (voxComponent.children != null) {
                        foreach (Voxeme child in voxComponent.children) {
                            if (child.isActiveAndEnabled) {
                                if (child.gameObject != voxComponent.gameObject) {
    //                      ResolvePhysicsPositionDiscepancies (child.gameObject);
    //                      ResolvePhysicsRotationDiscepancies (child.gameObject);

                                    if (macroEventSatisfied) {
                                        child.transform.localRotation =
                                            voxComponent.parentToChildRotationOffset[child.gameObject];
                                        //voxComponent.parentToChildRotationOffset [child.gameObject] = child.transform.localRotation;
                                        child.transform.rotation =
                                            voxComponent.gameObject.transform.rotation * child.transform.localRotation;
                                    }
                                    else {
                                        voxComponent.parentToChildRotationOffset[child.gameObject] =
                                            child.transform.localRotation;
                                        child.targetRotation = child.transform.rotation.eulerAngles;
                                    }

                                    child.transform.localPosition = Helper.RotatePointAroundPivot(
                                        voxComponent.parentToChildPositionOffset[child.gameObject],
                                        Vector3.zero, voxComponent.gameObject.transform.eulerAngles);
                                    //child.transform.localPosition = Helper.RotatePointAroundPivot (child.transform.localEulerAngles,
                                    //  Vector3.zero, voxComponent.gameObject.transform.eulerAngles);
                                    child.transform.position =
                                        voxComponent.gameObject.transform.position + child.transform.localPosition;
                                    child.targetPosition = child.transform.position;
                                }
                            }
                        }
                    }
                }
            }

            public static void ResolvePhysicsPositionDiscrepancies(GameObject obj, bool macroEventSatisfied) {
                Voxeme voxComponent = obj.GetComponent<Voxeme>();
                //Debug.Break ();
                //Debug.Log(string.Format("Before resolution: {0} position = {1}", obj.name, Helper.VectorToParsable(obj.transform.position)));
                // find the displacement between the main body and this rigidbody
                float displacement = float.MaxValue;
                Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
                //Debug.Log (obj.name);
                foreach (Rigidbody rigidbody in rigidbodies) {
                    if (voxComponent.displacement.ContainsKey(rigidbody.gameObject)) {
                        //  if (rigidbody.transform.localPosition.magnitude > voxComponent.displacement [rigidbody.gameObject].magnitude+Constants.EPSILON) {
                        if (rigidbody.transform.localPosition.magnitude -
                            voxComponent.displacement[rigidbody.gameObject].magnitude < displacement) {
                            //                  Debug.Log (rigidbody.name);
                            //                  Debug.Log (Helper.VectorToParsable (obj.transform.position));
                            //                  Debug.Log (Helper.VectorToParsable (rigidbody.transform.position));
                            displacement = rigidbody.transform.localPosition.magnitude -
                                           voxComponent.displacement[rigidbody.gameObject].magnitude;
                        }

                        //  }
                    }
                }

                if (displacement == float.MaxValue) {
                    displacement = 0.0f;
                }

                if (displacement > Constants.EPSILON) {
                    //Debug.Log (string.Format("{0} position displacement magnitude = {1}", obj.name, displacement));
    //              Debug.Log (displacement);
                    if (voxComponent != null) {
                        if (rigidbodies.Length > 0) {
                            //                      Debug.Log (rigidbodies [0].name);
                            //                      Debug.Log (rigidbodies [0].transform.position);
                            //                      Debug.Log (Helper.VectorToParsable(voxComponent.displacement [rigidbodies[0].gameObject]));
                            //Debug.Log(string.Format("{0} position = {1}", rigidbodies[0].name, Helper.VectorToParsable(rigidbodies[0].transform.position)));
                            //Debug.Log(string.Format("{0} position displacement = {1}", rigidbodies[0].name, Helper.VectorToParsable(voxComponent.displacement[rigidbodies[0].gameObject])));
                            //Debug.Log(string.Format("{0} rotation * position displacement = {1}", rigidbodies[0].name, Helper.VectorToParsable((obj.transform.rotation *
                                                      //voxComponent.displacement[rigidbodies[0].gameObject]))));
                            obj.transform.position = rigidbodies[0].transform.position -
                                                     (obj.transform.rotation *
                                                      voxComponent.displacement[rigidbodies[0].gameObject]);
                            //Debug.Log (obj.name);
                            //Debug.Log (obj.transform.position);
                            //obj.transform.position = rigidbodies [0].transform.localPosition - voxComponent.displacement [rigidbodies[0].gameObject] +
                            //  obj.transform.position;
                            voxComponent.targetPosition = obj.transform.position;
                            //                      Debug.Log (Helper.VectorToParsable (rigidbodies [0].transform.position));
                            //                      Debug.Log (Helper.VectorToParsable (rigidbodies [0].transform.localPosition));
                            //                      Debug.Log (Helper.VectorToParsable (obj.transform.position));

                            //Debug.Log (Helper.VectorToParsable (rigidbodies [0].transform.position));
                            //Debug.Log (Helper.VectorToParsable (voxComponent.displacement [rigidbodies[0].name]));

                            foreach (Rigidbody rigidbody in rigidbodies) {
                                if (voxComponent.displacement.ContainsKey(rigidbody.gameObject)) {
                                    //Debug.Log (string.Format("{0} position displacement = {1}",rigidbody.name,Helper.VectorToParsable(voxComponent.displacement[rigidbody.gameObject])));
                                    rigidbody.transform.localPosition = voxComponent.displacement[rigidbody.gameObject];
                                }
                            }
                        }
                    }
                }

                // TODO: Abstract away
                if (voxComponent != null) {
                    if (voxComponent.children != null) {
                        foreach (Voxeme child in voxComponent.children) {
                            if (child.isActiveAndEnabled) {
                                if (child.gameObject != voxComponent.gameObject) {
                                    //                      ResolvePhysicsPositionDiscepancies (child.gameObject);
                                    //                      ResolvePhysicsRotationDiscepancies (child.gameObject);
                                    //Debug.Log ("Moving child: " + gameObject.name);
                                    child.transform.localPosition =
                                        voxComponent.parentToChildPositionOffset[child.gameObject];
                                    child.targetPosition = child.transform.position;
                                }
                            }
                        }
                    }
                }
                //Debug.Log(string.Format("After resolution: {0} position = {1}", obj.name, Helper.VectorToParsable(obj.transform.position)));
            }

            public static float GetConcavityMinimum(GameObject obj) {
                Bounds bounds = Helper.GetObjectSize(obj);

                Vector3 concavityMin = bounds.min;
                foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>()) {
    //              Debug.Log (renderer.gameObject.name + " " + Helper.GetObjectWorldSize (renderer.gameObject).min.y);
                    if (Helper.GetObjectSize(renderer.gameObject).min.y > concavityMin.y) {
                        concavityMin = Helper.GetObjectSize(renderer.gameObject).min;
                    }
                }

                concavityMin = Helper.RotatePointAroundPivot(concavityMin, bounds.center, obj.transform.eulerAngles) +
                               obj.transform.position;

    //          Debug.Log (obj.transform.eulerAngles);
    //          Debug.Log (concavityMin.y);
                return concavityMin.y;

                /*
                Bounds bounds = Helper.GetObjectWorldSize (obj);

                float concavityMinY = bounds.min.y;
                foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>()) {
                    //Debug.Log (renderer.gameObject.name + " " + Helper.GetObjectWorldSize (renderer.gameObject).min.y);
                    if (Helper.GetObjectWorldSize (renderer.gameObject).min.y > concavityMinY) {
                        concavityMinY = Helper.GetObjectWorldSize (renderer.gameObject).min.y;
                    }
                }

                return concavityMinY;
                 */
            }
        }
    }
}