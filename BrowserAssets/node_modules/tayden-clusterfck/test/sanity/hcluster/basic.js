var clusterfck = require("../../../lib/clusterfck.js");

var colors = [
   [255, 255, 255],
   [253, 253, 253],
   [120, 130, 255],
   [120, 130, 40],
   [23, 30, 10],
   [250, 255, 246],
   [255, 255, 245],
   [31, 30, 12],
   [14, 20, 1],
];

exports.testDistances = function(test) {
   var clusters = clusterfck.hcluster(colors, "euclidean", "single").tree;
   var expected = {"dist":215,"left":{"dist":180.5713155515017,"left":{"dist":7.874007874011811,"left":{"dist":3.4641016151377544,"left":{"value":[253,253,253],"size":1},"right":{"value":[255,255,255],"size":1},"size":2},"right":{"dist":5.0990195135927845,"left":{"value":[250,255,246],"size":1},"right":{"value":[255,255,245],"size":1},"size":2},"size":4},"right":{"value":[120,130,255],"size":1},"size":5},"right":{"dist":136.7662239004938,"left":{"dist":16.186414056238647,"left":{"dist":8.246211251235321,"left":{"value":[31,30,12],"size":1},"right":{"value":[23,30,10],"size":1},"size":2},"right":{"value":[14,20,1],"size":1},"size":3},"right":{"value":[120,130,40],"size":1},"size":4},"size":9};
   test.deepEqual(clusters, expected, "final clusters for euclidean do not match expected");

   clusters = clusterfck.hcluster(colors, "manhattan", "single").tree;
   expected = {"dist":258,"left":{"dist":10,"left":{"dist":6,"left":{"value":[253,253,253],"size":1},"right":{"value":[255,255,255],"size":1},"size":2},"right":{"dist":6,"left":{"value":[250,255,246],"size":1},"right":{"value":[255,255,245],"size":1},"size":2},"size":4},"right":{"dist":217,"left":{"dist":215,"left":{"value":[120,130,255],"size":1},"right":{"value":[120,130,40],"size":1},"size":2},"right":{"dist":28,"left":{"dist":10,"left":{"value":[31,30,12],"size":1},"right":{"value":[23,30,10],"size":1},"size":2},"right":{"value":[14,20,1],"size":1},"size":3},"size":5},"size":9};
   test.deepEqual(clusters, expected, "final clusters for manhattan do not match expected");

   clusters = clusterfck.hcluster(colors, "max", "single").tree;
   expected = {"dist":205,"left":{"dist":130,"left":{"dist":7,"left":{"dist":2,"left":{"value":[253,253,253],"size":1},"right":{"value":[255,255,255],"size":1},"size":2},"right":{"dist":5,"left":{"value":[250,255,246],"size":1},"right":{"value":[255,255,245],"size":1},"size":2},"size":4},"right":{"value":[120,130,255],"size":1},"size":5},"right":{"dist":100,"left":{"value":[120,130,40],"size":1},"right":{"dist":10,"left":{"dist":8,"left":{"value":[31,30,12],"size":1},"right":{"value":[23,30,10],"size":1},"size":2},"right":{"value":[14,20,1],"size":1},"size":3},"size":4},"size":9};
   test.deepEqual(clusters, expected, "final clusters for max distance do not match expected");

   clusters = clusterfck.hcluster(colors, "max", "complete").tree;
   expected = {"dist":254,"left":{"dist":135,"left":{"dist":10,"left":{"dist":2,"left":{"value":[253,253,253],"size":1},"right":{"value":[255,255,255],"size":1},"size":2},"right":{"dist":5,"left":{"value":[250,255,246],"size":1},"right":{"value":[255,255,245],"size":1},"size":2},"size":4},"right":{"value":[120,130,255],"size":1},"size":5},"right":{"dist":110,"left":{"value":[120,130,40],"size":1},"right":{"dist":17,"left":{"dist":8,"left":{"value":[31,30,12],"size":1},"right":{"value":[23,30,10],"size":1},"size":2},"right":{"value":[14,20,1],"size":1},"size":3},"size":4},"size":9};

   test.deepEqual(clusters, expected, "final clusters for max distance do not match expected");

   clusters = clusterfck.hcluster(colors, "max", "average").tree;
   expected = {"dist":235.05,"left":{"dist":133.25,"left":{"dist":8.5,"left":{"dist":2,"left":{"value":[253,253,253],"size":1},"right":{"value":[255,255,255],"size":1},"size":2},"right":{"dist":5,"left":{"value":[250,255,246],"size":1},"right":{"value":[255,255,245],"size":1},"size":2},"size":4},"right":{"value":[120,130,255],"size":1},"size":5},"right":{"dist":103.33333333333333,"left":{"value":[120,130,40],"size":1},"right":{"dist":13.5,"left":{"dist":8,"left":{"value":[31,30,12],"size":1},"right":{"value":[23,30,10],"size":1},"size":2},"right":{"value":[14,20,1],"size":1},"size":3},"size":4},"size":9};
   test.deepEqual(clusters, expected, "final clusters for max distance do not match expected");
   test.done();
}
