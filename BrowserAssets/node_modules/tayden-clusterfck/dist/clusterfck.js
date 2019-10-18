/* MIT license */
/* Version: 0.6.0 */
(function(f){if(typeof exports==="object"&&typeof module!=="undefined"){module.exports=f()}else if(typeof define==="function"&&define.amd){define([],f)}else{var g;if(typeof window!=="undefined"){g=window}else if(typeof global!=="undefined"){g=global}else if(typeof self!=="undefined"){g=self}else{g=this}g.clusterfck = f()}})(function(){var define,module,exports;return (function e(t,n,r){function s(o,u){if(!n[o]){if(!t[o]){var a=typeof require=="function"&&require;if(!u&&a)return a(o,!0);if(i)return i(o,!0);var f=new Error("Cannot find module '"+o+"'");throw f.code="MODULE_NOT_FOUND",f}var l=n[o]={exports:{}};t[o][0].call(l.exports,function(e){var n=t[o][1][e];return s(n?n:e)},l,l.exports,e,t,n,r)}return n[o].exports}var i=typeof require=="function"&&require;for(var o=0;o<r.length;o++)s(r[o]);return s})({1:[function(require,module,exports){
module.exports = {
   hcluster: require("./hcluster"),
   Kmeans: require("./kmeans"),
   kmeans: require("./kmeans").kmeans
};
},{"./hcluster":3,"./kmeans":4}],2:[function(require,module,exports){
module.exports = {
  euclidean: function(v1, v2) {
      var total = 0;
      for (var i = 0; i < v1.length; i++) {
         total += Math.pow(v2[i] - v1[i], 2);      
      }
      return Math.sqrt(total);
   },
   manhattan: function(v1, v2) {
     var total = 0;
     for (var i = 0; i < v1.length ; i++) {
        total += Math.abs(v2[i] - v1[i]);      
     }
     return total;
   },
   max: function(v1, v2) {
     var max = 0;
     for (var i = 0; i < v1.length; i++) {
        max = Math.max(max , Math.abs(v2[i] - v1[i]));      
     }
     return max;
   }
};
},{}],3:[function(require,module,exports){
var distances = require("./distance");

var HierarchicalClustering = function(distance, linkage, threshold) {
   this.distance = distance;
   this.linkage = linkage;
   this.threshold = threshold == undefined ? Infinity : threshold;
}

HierarchicalClustering.prototype = {
   tree: function(items, snapshotPeriod, snapshotCb) {
      this.tree = [];
      this.dists = [];  // distances between each pair of clusters
      this.mins = []; // closest cluster for each cluster
      this.index = []; // keep a hash of all clusters by key

      for (var i = 0; i < items.length; i++) {
         var cluster = {
            value: items[i],
            key: i,
            index: i,
            size: 1
         };
         this.tree[i] = cluster;
         this.index[i] = cluster;
         this.dists[i] = [];
         this.mins[i] = 0;
      }

      for (var i = 0; i < this.tree.length; i++) {
         for (var j = 0; j <= i; j++) {
            var dist = (i == j) ? Infinity :
               this.distance(this.tree[i].value, this.tree[j].value);
            this.dists[i][j] = dist;
            this.dists[j][i] = dist;

            if (dist < this.dists[i][this.mins[i]]) {
               this.mins[i] = j;
            }
         }
      }

      var merged = this.mergeClosest();
      var i = 0;
      while (merged) {
        if (snapshotCb && (i++ % snapshotPeriod) == 0) {
           snapshotCb(this.tree);
        }
        merged = this.mergeClosest();
      }

      this.tree.forEach(function(cluster) {
        // clean up metadata used for clustering
        delete cluster.key;
        delete cluster.index;
      });

      return this.tree;
   },

   mergeClosest: function() {
      // find two closest clusters from cached mins
      var minKey = 0, min = Infinity;
      for (var i = 0; i < this.tree.length; i++) {
         var key = this.tree[i].key,
             dist = this.dists[key][this.mins[key]];
         if (dist < min) {
            minKey = key;
            min = dist;
         }
      }
      if (min >= this.threshold) {
         return false;
      }

      var c1 = this.index[minKey],
          c2 = this.index[this.mins[minKey]];

      // merge two closest clusters
      var merged = {
         dist: min,
         left: c1,
         right: c2,
         key: c1.key,
         size: c1.size + c2.size
      };

      this.tree[c1.index] = merged;
      this.tree.splice(c2.index, 1);
      this.index[c1.key] = merged;

      // update distances with new merged cluster
      for (var i = 0; i < this.tree.length; i++) {
         var ci = this.tree[i];
         var dist;
         if (c1.key == ci.key) {
            dist = Infinity;
         }
         else if (this.linkage == "single") {
            dist = this.dists[c1.key][ci.key];
            if (this.dists[c1.key][ci.key] > this.dists[c2.key][ci.key]) {
               dist = this.dists[c2.key][ci.key];
            }
         }
         else if (this.linkage == "complete") {
            dist = this.dists[c1.key][ci.key];
            if (this.dists[c1.key][ci.key] < this.dists[c2.key][ci.key]) {
               dist = this.dists[c2.key][ci.key];
            }
         }
         else if (this.linkage == "average") {
            dist = (this.dists[c1.key][ci.key] * c1.size
                   + this.dists[c2.key][ci.key] * c2.size) / (c1.size + c2.size);
         }
         else {
            dist = this.distance(ci.value, c1.value);
         }

         this.dists[c1.key][ci.key] = this.dists[ci.key][c1.key] = dist;
      }


      // update cached mins
      for (var i = 0; i < this.tree.length; i++) {
         var key1 = this.tree[i].key;
         if (this.mins[key1] == c1.key || this.mins[key1] == c2.key) {
            var min = key1;
            for (var j = 0; j < this.tree.length; j++) {
               var key2 = this.tree[j].key;
               if (this.dists[key1][key2] < this.dists[key1][min]) {
                  min = key2;
               }
            }
            this.mins[key1] = min;
         }
         this.tree[i].index = i;
      }

      // clean up metadata used for clustering
      delete c1.key; delete c2.key;
      delete c1.index; delete c2.index;

      return true;
   },
   clusters: function(num){
     //  Return all nodes if num is invalid
     if(num > this.tree.size || num < 1) num = this.tree.size

     var result = [],
         subtrees = [this.tree];

    //  Get a list of root nodes for num different clusters
     while(num > 1){
       var furthest = _findNextFurthest(subtrees);
       subtrees.splice(subtrees.indexOf(furthest), 1);
       subtrees.push(furthest.left, furthest.right);
       num--;
     }

     //  Transform the subtrees node list into a list of the subtrees leaf values
     subtrees.forEach(function(tree) {
       result.push(_getValues(tree));
     })

     //  Split the next furthest distance root node
     function _findNextFurthest(subtrees) {
       var max = -1,
           furthest;
       subtrees.forEach(function(tree){
         if(tree.dist > max) {
           max = tree.dist;
           furthest = tree;
         }
       });
       return furthest;
     }

     //  Traverse the tree and yield a list of the leaf node values
     function _getValues(tree) {
       if(tree.size === 1) return [tree.value];
       return _getValues(tree.left).concat(_getValues(tree.right));
     }

     return result;
   }
}

var hcluster = function(items, distance, linkage, threshold, snapshot, snapshotCallback) {
   distance = distance || "euclidean";
   linkage = linkage || "average";

   if (typeof distance == "string") {
     distance = distances[distance];
   }
  var hc = new HierarchicalClustering(distance, linkage, threshold),
      tree = hc.tree(items, snapshot, snapshotCallback);

   return {
     tree: (threshold === undefined ? tree[0] : tree),
     clusters: hc.clusters
   };
}

module.exports = hcluster;

},{"./distance":2}],4:[function(require,module,exports){
var distances = require("./distance");

function KMeans(centroids) {
   this.centroids = centroids || [];
}

KMeans.prototype.randomCentroids = function(points, k) {
   var centroids = points.slice(0); // copy
   centroids.sort(function() {
      return (Math.round(Math.random()) - 0.5);
   });
   return centroids.slice(0, k);
}

KMeans.prototype.classify = function(point, distance) {
   var min = Infinity,
       index = 0;

   distance = distance || "euclidean";
   if (typeof distance == "string") {
      distance = distances[distance];
   }

   for (var i = 0; i < this.centroids.length; i++) {
      var dist = distance(point, this.centroids[i]);
      if (dist < min) {
         min = dist;
         index = i;
      }
   }

   return index;
}

KMeans.prototype.cluster = function(points, k, distance, snapshotPeriod, snapshotCb) {
   k = k || Math.max(2, Math.ceil(Math.sqrt(points.length / 2)));

   distance = distance || "euclidean";
   if (typeof distance == "string") {
      distance = distances[distance];
   }

   this.centroids = this.randomCentroids(points, k);

   var assignment = new Array(points.length);
   var clusters = new Array(k);

   var iterations = 0;
   var movement = true;
   while (movement) {
      // update point-to-centroid assignments
      for (var i = 0; i < points.length; i++) {
         assignment[i] = this.classify(points[i], distance);
      }

      // update location of each centroid
      movement = false;
      for (var j = 0; j < k; j++) {
         var assigned = [];
         for (var i = 0; i < assignment.length; i++) {
            if (assignment[i] == j) {
               assigned.push(points[i]);
            }
         }

         if (!assigned.length) {
            continue;
         }

         var centroid = this.centroids[j];
         var newCentroid = new Array(centroid.length);

         for (var g = 0; g < centroid.length; g++) {
            var sum = 0;
            for (var i = 0; i < assigned.length; i++) {
               sum += assigned[i][g];
            }
            newCentroid[g] = sum / assigned.length;

            if (newCentroid[g] != centroid[g]) {
               movement = true;
            }
         }

         this.centroids[j] = newCentroid;
         clusters[j] = assigned;
      }

      if (snapshotCb && (iterations++ % snapshotPeriod == 0)) {
         snapshotCb(clusters);
      }
   }

   return clusters;
}

KMeans.prototype.toJSON = function() {
   return JSON.stringify(this.centroids);
}

KMeans.prototype.fromJSON = function(json) {
   this.centroids = JSON.parse(json);
   return this;
}

module.exports = KMeans;

module.exports.kmeans = function(vectors, k) {
   return (new KMeans()).cluster(vectors, k);
}
},{"./distance":2}]},{},[1])(1)
});