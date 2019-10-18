import {expect} from 'chai';
const distance = require('../lib/distance');

describe('distance', () => {

  describe('euclidean', () => {
    it('should return the correct distance between two simple integers', () => {
      let answer = distance.euclidean([0], [5]);
      expect(answer).to.equal(5);
    });

    it('should return the correct distance between two 2D vectors', () => {
      let answer = distance.euclidean([5, 2], [3, 2]);
      expect(answer).to.equal(2);
    });

    it('should return the correct distance between large vectors', () => {
      let answer = distance.euclidean([5, 2, 2, 2, 2, 2, 2, 2], [7, 2, 2, 2, 2, 2, 2, 2]);
      expect(answer).to.equal(2);
    });
  });

  describe('mahattan', () => {
    it('should return the correct distance between two simple integers', () => {
      let answer = distance.manhattan([0], [5]);
      expect(answer).to.equal(5);
    });

    it('should return the correct distance between two 2D vectors', () => {
      let answer = distance.manhattan([5, 4], [3, 2]);
      expect(answer).to.equal(4);
    });

    it('should return the correct distance between large vectors', () => {
      let answer = distance.manhattan([5, 4, 2, 8, 2, 2, 2, 2], [7, 2, 2, 2, 2, 2, 2, 2]);
      expect(answer).to.equal(10);
    });
  });

  describe('max', () => {
    it('should return the correct distance between two simple integers', () => {
      let answer = distance.max([0], [5]);
      expect(answer).to.equal(5);
    });

    it('should return the maximum difference between two dimensions of two 2D vectors', () => {
      let answer = distance.max([5, 4], [3, 2]);
      expect(answer).to.equal(2);
    });

    it('should return the maximum difference between two dimensions of two large vectors', () => {
      let answer = distance.max([5, 4, 2, 8, 2, 2, 2, 2], [7, 2, 2, 2, 2, 2, 2, 2]);
      expect(answer).to.equal(6);
    });
  });

});
