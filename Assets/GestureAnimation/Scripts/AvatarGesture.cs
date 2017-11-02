using System.Collections;
using System.Collections.Generic;

public class AvatarGesture {
    public enum Handedness
    {
        NA = -1,
        Right = 0,
        Left
    }

    public enum Orientation
    {
        NA = -1,
        Front = 0,
        Back,
        Left,
        Right
    }

    //
    //  Right arm gestures
    //

    // Idle
    public static AvatarGesture RARM_IDLE         = new AvatarGesture("RARM_IDLE",         0) { Hand = Handedness.NA,    Direction = Orientation.NA    };

    // Numeric
    public static AvatarGesture RARM_NUMBER_ONE   = new AvatarGesture("RARM_NUMBER_ONE",   1) { Hand = Handedness.Right, Direction = Orientation.NA    };
    public static AvatarGesture RARM_NUMBER_TWO   = new AvatarGesture("RARM_NUMBER_TWO",   2) { Hand = Handedness.Right, Direction = Orientation.NA    };
    public static AvatarGesture RARM_NUMBER_THREE = new AvatarGesture("RARM_NUMBER_THREE", 3) { Hand = Handedness.Right, Direction = Orientation.NA    };
    public static AvatarGesture RARM_NUMBER_FOUR  = new AvatarGesture("RARM_NUMBER_FOUR",  4) { Hand = Handedness.Right, Direction = Orientation.NA    };
    public static AvatarGesture RARM_NUMBER_FIVE  = new AvatarGesture("RARM_NUMBER_FIVE",  5) { Hand = Handedness.Right, Direction = Orientation.NA    };

    // Carry
    public static AvatarGesture RARM_CARRY_FRONT  = new AvatarGesture("RARM_CARRY_FRONT",  6) { Hand = Handedness.Right, Direction = Orientation.Front };
    public static AvatarGesture RARM_CARRY_BACK   = new AvatarGesture("RARM_CARRY_BACK",   7) { Hand = Handedness.Right, Direction = Orientation.Back  };
    public static AvatarGesture RARM_CARRY_LEFT   = new AvatarGesture("RARM_CARRY_LEFT",   8) { Hand = Handedness.Right, Direction = Orientation.Left  };
    public static AvatarGesture RARM_CARRY_RIGHT  = new AvatarGesture("RARM_CARRY_RIGHT",  9) { Hand = Handedness.Right, Direction = Orientation.Right };

    // Point
    public static AvatarGesture RARM_POINT_FRONT  = new AvatarGesture("RARM_POINT_FRONT",  6) { Hand = Handedness.Right, Direction = Orientation.Front };
    public static AvatarGesture RARM_POINT_BACK   = new AvatarGesture("RARM_POINT_BACK",   7) { Hand = Handedness.Right, Direction = Orientation.Back  };
    public static AvatarGesture RARM_POINT_LEFT   = new AvatarGesture("RARM_POINT_LEFT",   8) { Hand = Handedness.Right, Direction = Orientation.Left  };
    public static AvatarGesture RARM_POINT_RIGHT  = new AvatarGesture("RARM_POINT_RIGHT",  9) { Hand = Handedness.Right, Direction = Orientation.Right };

    //
    //  Static helpers
    //

    // Dictionary lookup of gesture name to AvatarGesture
    public static Dictionary<string, AvatarGesture> ALL_GESTURES = new Dictionary<string, AvatarGesture>();

    static AvatarGesture()
    {
        // Init string -> gesture dictionary mapping
        // NOTE: When adding new gestures, add them here too!

        AddGestureToList(RARM_IDLE        );

        AddGestureToList(RARM_NUMBER_ONE  );
        AddGestureToList(RARM_NUMBER_TWO  );
        AddGestureToList(RARM_NUMBER_THREE);
        AddGestureToList(RARM_NUMBER_FOUR );
        AddGestureToList(RARM_NUMBER_FIVE );

        AddGestureToList(RARM_CARRY_FRONT );
        AddGestureToList(RARM_CARRY_BACK  );
        AddGestureToList(RARM_CARRY_LEFT  );
        AddGestureToList(RARM_CARRY_RIGHT );

        AddGestureToList(RARM_POINT_FRONT );
        AddGestureToList(RARM_POINT_BACK  );
        AddGestureToList(RARM_POINT_LEFT  );
        AddGestureToList(RARM_POINT_RIGHT);
    }

    private static void AddGestureToList(AvatarGesture gesture)
    {
        ALL_GESTURES.Add(gesture.Name, gesture);
    }

    //
    //  Properties
    //

    public string Name
    {
        get;
        private set;
    }

    public int Id
    {
        get;
        private set;
    }

    public Handedness Hand
    {
        get;
        private set;
    }

    public Orientation Direction
    {
        get;
        private set;
    }

    //
    //  Constructors
    //

    public AvatarGesture(int id)
    {
        Id = id;
    }

    // Name is used internally for a lookup
    private AvatarGesture(string name, int id)
    {
        Name = name;
        Id = id;
    }
}
