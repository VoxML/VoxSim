module.exports = {
  context: "./",
  entry: __dirname + "/lib/clusterfck",
  output: {
    path: __dirname + "/dist",
    filename: "clusterfck.min.js",
    libraryTarget: "var",
    library: "clusterfck"
  }
}
