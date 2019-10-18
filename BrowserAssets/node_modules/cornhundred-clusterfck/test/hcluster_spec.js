import {expect} from 'chai';
const hcluster = require('../lib/hcluster');

describe('hcluster', () => {

  describe('constructor', () => {
    it('should default to euclidean distance', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]]);
      expect(h.hc.distance).to.deep.equal(require('../lib/distance').euclidean);
    });

    it('should get the euclidean distance function', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], "euclidean");
      expect(h.hc.distance).to.deep.equal(require('../lib/distance').euclidean);
    });

    it('should get the manhattan distance function', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], "manhattan");
      expect(h.hc.distance).to.deep.equal(require('../lib/distance').manhattan);
    });

    it('should get the max distance function', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], "max");
      expect(h.hc.distance).to.deep.equal(require('../lib/distance').max);
    });

    it('should accept an arbitrary distance function', () => {
      let d = (p, q) => Math.hypot(p) - Math.hypot(q);
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], d, "complete");
      expect(h.hc.distance).to.deep.equal(d);
    });

    it('should default to average linkage', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]]);
      expect(h.hc.linkage).to.equal("average");
    });

    it('should set the linkage param', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], "euclidean", "complete");
      expect(h.hc.linkage).to.equal("complete");
    });
  });

  describe('tree', () => {
    it('should return a tree object under key "tree"', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], "euclidean", "complete");
      expect(h).to.have.property('tree');
      expect(h.tree).to.be.an('object');
    });

    it('should build the correct tree object', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], "euclidean", "complete");
      expect(h.tree).to.deep.equal({
        "dist": 282.842712474619,
        "left": {
          "dist": 28.284271247461902,
          "left": {
            "dist": 2.8284271247461903,
            "left": {"size": 1, "value": [2,2]},
            "right": {"size": 1, "value": [0,0]},
            "size": 2
          },
          "right": {"size": 1, "value": [20,20]},
          "size": 3
        },
        "right": {"size": 1, "value": [200,200]},
        "size": 4
      });
    });

    it('should not return links greater than the threshold', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], "euclidean", "complete", 200);
      expect(h.tree).to.deep.equal([{
        "dist": 28.284271247461902,
        "left": {
          "dist": 2.8284271247461903,
          "left": {"size": 1, "value": [2,2]},
          "right": {"size": 1, "value": [0,0]},
          "size": 2
        },
        "right": {"size": 1, "value": [20,20]},
        "size": 3
      },{
        "size": 1, "value": [200,200]
      }]);
    });
  });

  describe('clusters', () => {
    it('should return the correct number of clusters', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], "euclidean", "complete");

      let clusters = h.clusters(3);
      expect(clusters.length).to.equal(3);

      clusters = h.clusters(2);
      expect(clusters.length).to.equal(2);

      clusters = h.clusters(1);
      expect(clusters.length).to.equal(1);
    });

    it('should handle an invalid number of clusters by default to return all points', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], "euclidean", "complete");

      let clusters = h.clusters(6);
      expect(clusters.length).to.equal(4);

      clusters = h.clusters(-1);
      expect(clusters.length).to.equal(4);

      clusters = h.clusters(0);
      expect(clusters.length).to.equal(4);
    });

    it('should return two clusters correctly', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], "euclidean", "complete");
      let clusters = h.clusters(2);
      expect(clusters).to.deep.have.same.members([[[2,2],[0,0],[20,20]],[[200,200]]]);
    });

    it('should return three clusters correctly', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], "euclidean", "complete");
      let clusters = h.clusters(3);
      expect(clusters).to.deep.have.same.members([[[200,200]],[[2,2],[0,0]],[[20,20]]]);
    });

    it('should return four clusters correctly', () => {
      let h = hcluster([[0,0],[2,2],[20,20],[200,200]], "euclidean", "complete");
      let clusters = h.clusters(4);
      expect(clusters).to.deep.have.same.members([[[200,200]],[[2,2]],[[0,0]],[[20,20]]]);
    });
  });

});
