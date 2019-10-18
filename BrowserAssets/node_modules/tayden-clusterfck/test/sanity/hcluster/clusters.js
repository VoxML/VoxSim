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

exports.testGroups = function(test) {
   var clusters = clusterfck.hcluster(colors, "euclidean", "single").clusters(0);
   var expected = [[[120,130,255]],[[120,130,40]],[[14,20,1]],[[31,30,12]],[[23,30,10]],[[250,255,246]],[[255,255,245]],[[253,253,253]],[[255,255,255]]];
   test.deepEqual(clusters, expected, "clusters 0 should do nothing");

   clusters = clusterfck.hcluster(colors, "manhattan", "single").clusters(1);
   expected = [[[253,253,253],[255,255,255],[250,255,246],[255,255,245],[120,130,255],[120,130,40],[31,30,12],[23,30,10],[14,20,1]]];
   test.deepEqual(clusters, expected, "clusters 1 should produce one cluster");

   clusters = clusterfck.hcluster(colors, "max", "single").clusters(3);
   expected = [[[120,130,40],[31,30,12],[23,30,10],[14,20,1]],[[253,253,253],[255,255,255],[250,255,246],[255,255,245]],[[120,130,255]]];

   test.deepEqual(clusters, expected, "clusters do not match");

   clusters = clusterfck.hcluster(colors, "max", "complete").clusters(9);
   expected = [[[120,130,255]],[[120,130,40]],[[14,20,1]],[[31,30,12]],[[23,30,10]],[[250,255,246]],[[255,255,245]],[[253,253,253]],[[255,255,255]]];
   test.deepEqual(clusters, expected, "clusters 9 should produce one item clusters");

   test.done();
}
