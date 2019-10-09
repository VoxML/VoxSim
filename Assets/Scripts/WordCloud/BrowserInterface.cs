using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ZenFulcrum.EmbeddedBrowser;

public class BrowserInterface : MonoBehaviour
{
    float prior0;
    float prior1;
    ZenFulcrum.EmbeddedBrowser.IPromise<ZenFulcrum.EmbeddedBrowser.JSONNode> ip = null; //`1[ZenFulcrum.EmbeddedBrowser.JSONNode]

    // Start is called before the first frame update
    void Start(){
        
    }

    // Update is called once per frame
    void Update(){
        
    }

    string Arbitrary_Func() {
        Browser b = Selection.activeGameObject.GetComponent<Browser>();
        //Transform panel = transform.parent.Find("Panel");
        //if (prior0 - 0 < 0.001){
        //    // Find relationship between scale of panel and pixel size of texture?
        //    prior0 = panel.localScale[0];
        //    prior1 = panel.localScale[1];
        //}
        //// Come up with ratio for resizing
        //int rat0 = (int)(prior0 * b.Size[0] / panel.localScale[0]);
        //int rat1 = (int)(prior1 * b.Size[1] / panel.localScale[1]);


        //prior0 = panel.localScale[0];
        //prior1 = panel.localScale[1];

        //b.Resize(new Texture2D(rat0, rat1));
        //Debug.LogWarning(ip);
        //if (ip != null) {
        //    Debug.LogWarning(ip + "\n" + ip.GetType().ToString());
        //}
        //ip = b.EvalJS("var a = 3; return a + 3;");

        //Prints clustergrammer.js
        //b.EvalJS("document.title").Then(ret => Debug.LogWarning("Document title: " + ret)).Done();


        // Not a perfect call, but makes a new clustergrammer from new source.
        // Side effect of duplicating the side bar.
        // Note that the function doesn't need to mention the specific javascript file it's from
        //      That's because it's all loaded into the html already, I think.
        //b.CallFunction("make_clust('mult_viewOLD.json')").Done();

        b.CallFunction("Clustergrammer()").Done();

        //b.EvalJS("").Then(ret => Debug.LogWarning(ret)).Done();



        //Debug.LogWarning(ip + "\n" + ip.GetType().ToString());
        // ZenFulcrum.EmbeddedBrowser.Promise`1[ZenFulcrum.EmbeddedBrowser.JSONNode]


        //Webpack is a module bundler.It takes disparate dependencies, creates modules for them and bundles the entire network up into manageable output files.This is especially useful for Single Page Applications(SPAs), which is the defacto standard for Web Applications today.


        return "test";
    }

    [MenuItem("Jarvis/Arbitrary &#A")]
    static void Arb() {
        BrowserInterface bi = Selection.activeGameObject.GetComponent<BrowserInterface>();
        Debug.LogWarning("Result: " + bi.Arbitrary_Func());
    }

    // Makes sure that we have this object selected yo
    //[MenuItem("VoxSim/New WordCloud &#w", true)]
    //static bool ValidateNewWordCloud() {

    //}

    // Makes sure that we have this object selected yo
    [MenuItem("Jarvis/Arbitrary &#A", true)]
    static bool ValidateArb() {
        return (Selection.activeGameObject != null) &&
       (Selection.activeGameObject.GetComponent<BrowserInterface>() != null) &&
       (Selection.activeGameObject.GetComponent<Browser>() != null);
    }
}
