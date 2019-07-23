namespace VoxSimPlatform {
    // Namespace GenLex contains GL (Generative Lexicon) structures on which VoxSim depends
    //  (can't call it GL because of OpenGL)
    namespace GenLex {
        public enum GLType {
            None,
            Agent,  // substitute for GL Human type
            AgentList,
            Artifact,
            ArtifactList,
            Location,
            LocationList,
            PhysObj,
            PhysObjList,
            Vector,
            VectorList,
            Method,
            MethodList
        }
    }
}