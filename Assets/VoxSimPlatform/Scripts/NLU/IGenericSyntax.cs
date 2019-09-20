namespace VoxSimPlatform {
    namespace NLU {
        /// <summary>
        /// Interface to recursively defined data type to shove JSONs into
        /// </summary>
        public interface IGenericSyntax {
            string ExportTagOrWords(bool top = false);
            IGenericSyntax CreateFromJSON(string jsonString);
        }
    }
}
