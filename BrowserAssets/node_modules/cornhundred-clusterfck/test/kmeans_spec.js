import {expect} from 'chai';
const KMeans = require('../lib/kmeans');
const kmeans = require('../lib/kmeans').kmeans;

describe('KMeans', () => {
  describe('constructor', () => {
    it('should initialize centroids to the passed in points', () => {
      let k = new KMeans([[0,1], [1,2]]);
      expect(k.centroids).to.deep.equal([[0,1], [1,2]]);
    })
    it('should initialize centroids to an empty array if no centroids are provided', () => {
      let k = new KMeans();
      expect(k.centroids).to.deep.equal([]);
    })
  });

  describe('randomCentroids', () => {
    it('should generate a given number of random centroids from a list of points', () => {
      let k = new KMeans();
      let points = [[0,1], [2,43], [9,2], [3,3], [4,3]];
      let centroids = k.randomCentroids(points, 3);

      expect(points).to.include.members(centroids);
    });
  });

  describe('classify', () => {
    it('should classify points using the given centroids', () => {
      let k = new KMeans([[0,0], [1,1], [2,2]]);
      let ans = k.classify([1,0.5], "manhattan");
      expect(ans).to.equal(1);
    });
    it('should use euclidean as the default distance', () => {
      let k = new KMeans([[0,0], [1,1], [2,2]]);
      let ans = k.classify([1,0.5]);
      expect(ans).to.equal(1);
    });
    it('should accept an arbitrary distance function', () => {
      let k = new KMeans([[0,0], [1,1], [2,2]]);
      let ans = k.classify([1,0.5], ((v1, v2) => {Math.hypot(v1) - Math.hypot(v2)}));
      expect(ans).to.equal(0);
    });
  });

  describe('cluster', () => {
    it('should return the correct number of clusters', () => {
      let k = new KMeans();
      let clusters = k.cluster([[0,0], [1,1], [2,2]], 2, "euclidean");
      expect(clusters.length).to.equal(2);
    });

    it('should return two clusters correctly', () => {
      let k = new KMeans();
      let data = [
         [1, 1, 1],
         [2, 2, 2],
         [3, 3, 3],
         [4, 4, 4],
         [5, 5, 5],
         [20, 20, 20],
         [200, 200, 200]
      ];
      let iterations = 20;
      for (let i = 0; i < iterations; i++) {
        let clusters = k.cluster(data, 2, "euclidean");
        expect(clusters).to.deep.include.members([
          [
            [1, 1, 1],
            [2, 2, 2],
            [3, 3, 3],
            [4, 4, 4],
            [5, 5, 5],
            [20, 20, 20]
          ],[
            [200, 200, 200]
          ]
        ]);
      }
    });

    it('should return three clusters correctly', () => {
      let k = new KMeans();
      let data = [
         [1, 1, 1],
         [2, 2, 2],
         [3, 3, 3],
         [4, 4, 4],
         [5, 5, 5],
         [20, 20, 20],
         [200, 200, 200]
      ];
      let iterations = 20;
      for (let i = 0; i < iterations; i++) {
        let clusters = k.cluster(data, 3, "euclidean");
        expect(clusters).to.deep.include.members([
          [
            [1, 1, 1],
            [2, 2, 2],
            [3, 3, 3],
            [4, 4, 4],
            [5, 5, 5],
          ],[
            [20, 20, 20]
          ],[
            [200, 200, 200]
          ]
        ]);
      }
    });
  });

  describe('toJSON', () => {
    it('should return a string of centroids', () => {
      let k = new KMeans([[0,0], [1,1]]);
      let str = k.toJSON();
      expect(str).to.be.a("string");
      expect(str).to.equal("[[0,0],[1,1]]");
    });
  });

  describe('fromJSON', () => {
    it('should set centroids using a JSON string', () => {
      let k = new KMeans();
      expect(k.centroids).to.deep.equal([]);
      k = k.fromJSON("[[0,0],[1,1]]");
      expect(k.centroids).to.deep.equal([[0,0],[1,1]]);
    });
  });

});


describe('kmeans', () => {
  it('should return two clusters correctly', () => {
    let data = [
       [1, 1, 1],
       [2, 2, 2],
       [3, 3, 3],
       [4, 4, 4],
       [5, 5, 5],
       [20, 20, 20],
       [200, 200, 200]
    ];
    let iterations = 20;
    for (let i = 0; i < iterations; i++) {
      let clusters = kmeans(data, 2);
      expect(clusters).to.deep.include.members([
        [
          [1, 1, 1],
          [2, 2, 2],
          [3, 3, 3],
          [4, 4, 4],
          [5, 5, 5],
          [20, 20, 20]
        ],[
          [200, 200, 200]
        ]
      ]);
    }
  });

  it('should return three clusters correctly', () => {
    let data = [
       [1, 1, 1],
       [2, 2, 2],
       [3, 3, 3],
       [4, 4, 4],
       [5, 5, 5],
       [20, 20, 20],
       [200, 200, 200]
    ];
    let iterations = 20;
    for (let i = 0; i < iterations; i++) {
      let clusters = kmeans(data, 3);
      expect(clusters).to.deep.include.members([
        [
          [1, 1, 1],
          [2, 2, 2],
          [3, 3, 3],
          [4, 4, 4],
          [5, 5, 5],
        ],[
          [20, 20, 20]
        ],[
          [200, 200, 200]
        ]
      ]);
    }
  });

});
