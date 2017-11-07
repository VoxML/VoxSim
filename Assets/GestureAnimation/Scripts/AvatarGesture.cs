using System.Collections;
using System.Collections.Generic;

public class AvatarGesture {
    public enum Body
    {
        FullBody = -1,
        RightArm = 0,
        LeftArm,
        Head
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
    //  GESTURES
    //  TODO Eventually, we can define a gesture as a compound action combining arbitrary arm motions and hand poses.
    //
    //  NOTE: When adding new gestures, make sure to add them to the gesture dictionary below!
    //
    
    //
    //  Right arm gestures
    //

    // Idle
    public static AvatarGesture RARM_IDLE         = new AvatarGesture("RARM_IDLE",         0) { BodyPart = Body.RightArm, Direction = Orientation.NA    };

    // Numeric
    public static AvatarGesture RARM_NUMBER_ONE   = new AvatarGesture("RARM_NUMBER_ONE",   1) { BodyPart = Body.RightArm, Direction = Orientation.NA    };
    public static AvatarGesture RARM_NUMBER_TWO   = new AvatarGesture("RARM_NUMBER_TWO",   2) { BodyPart = Body.RightArm, Direction = Orientation.NA    };
    public static AvatarGesture RARM_NUMBER_THREE = new AvatarGesture("RARM_NUMBER_THREE", 3) { BodyPart = Body.RightArm, Direction = Orientation.NA    };
    public static AvatarGesture RARM_NUMBER_FOUR  = new AvatarGesture("RARM_NUMBER_FOUR",  4) { BodyPart = Body.RightArm, Direction = Orientation.NA    };
    public static AvatarGesture RARM_NUMBER_FIVE  = new AvatarGesture("RARM_NUMBER_FIVE",  5) { BodyPart = Body.RightArm, Direction = Orientation.NA    };

    // Carry
    public static AvatarGesture RARM_CARRY_FRONT  = new AvatarGesture("RARM_CARRY_FRONT",  6) { BodyPart = Body.RightArm, Direction = Orientation.Front };
    public static AvatarGesture RARM_CARRY_BACK   = new AvatarGesture("RARM_CARRY_BACK",   7) { BodyPart = Body.RightArm, Direction = Orientation.Back  };
    public static AvatarGesture RARM_CARRY_LEFT   = new AvatarGesture("RARM_CARRY_LEFT",   8) { BodyPart = Body.RightArm, Direction = Orientation.Left  };
    public static AvatarGesture RARM_CARRY_RIGHT  = new AvatarGesture("RARM_CARRY_RIGHT",  9) { BodyPart = Body.RightArm, Direction = Orientation.Right };

    // Point
    public static AvatarGesture RARM_POINT_FRONT  = new AvatarGesture("RARM_POINT_FRONT", 10) { BodyPart = Body.RightArm, Direction = Orientation.Front };
    public static AvatarGesture RARM_POINT_BACK   = new AvatarGesture("RARM_POINT_BACK",  11) { BodyPart = Body.RightArm, Direction = Orientation.Back  };
    public static AvatarGesture RARM_POINT_LEFT   = new AvatarGesture("RARM_POINT_LEFT",  12) { BodyPart = Body.RightArm, Direction = Orientation.Left  };
    public static AvatarGesture RARM_POINT_RIGHT  = new AvatarGesture("RARM_POINT_RIGHT", 13) { BodyPart = Body.RightArm, Direction = Orientation.Right };

    //
    //  Left arm gestures
    //

    // Idle
    public static AvatarGesture LARM_IDLE         = new AvatarGesture("LARM_IDLE",         0) { BodyPart = Body.LeftArm, Direction = Orientation.NA    };

    // Numeric
    public static AvatarGesture LARM_NUMBER_ONE   = new AvatarGesture("LARM_NUMBER_ONE",   1) { BodyPart = Body.LeftArm, Direction = Orientation.NA    };
    public static AvatarGesture LARM_NUMBER_TWO   = new AvatarGesture("LARM_NUMBER_TWO",   2) { BodyPart = Body.LeftArm, Direction = Orientation.NA    };
    public static AvatarGesture LARM_NUMBER_THREE = new AvatarGesture("LARM_NUMBER_THREE", 3) { BodyPart = Body.LeftArm, Direction = Orientation.NA    };
    public static AvatarGesture LARM_NUMBER_FOUR  = new AvatarGesture("LARM_NUMBER_FOUR",  4) { BodyPart = Body.LeftArm, Direction = Orientation.NA    };
    public static AvatarGesture LARM_NUMBER_FIVE  = new AvatarGesture("LARM_NUMBER_FIVE",  5) { BodyPart = Body.LeftArm, Direction = Orientation.NA    };

    // Carry
    public static AvatarGesture LARM_CARRY_FRONT  = new AvatarGesture("LARM_CARRY_FRONT",  6) { BodyPart = Body.LeftArm, Direction = Orientation.Front };
    public static AvatarGesture LARM_CARRY_BACK   = new AvatarGesture("LARM_CARRY_BACK",   7) { BodyPart = Body.LeftArm, Direction = Orientation.Back  };
    public static AvatarGesture LARM_CARRY_LEFT   = new AvatarGesture("LARM_CARRY_LEFT",   8) { BodyPart = Body.LeftArm, Direction = Orientation.Left  };
    public static AvatarGesture LARM_CARRY_RIGHT  = new AvatarGesture("LARM_CARRY_RIGHT",  9) { BodyPart = Body.LeftArm, Direction = Orientation.Right };

    // Point
    public static AvatarGesture LARM_POINT_FRONT  = new AvatarGesture("LARM_POINT_FRONT", 10) { BodyPart = Body.LeftArm, Direction = Orientation.Front };
    public static AvatarGesture LARM_POINT_BACK   = new AvatarGesture("LARM_POINT_BACK",  11) { BodyPart = Body.LeftArm, Direction = Orientation.Back  };
    public static AvatarGesture LARM_POINT_LEFT   = new AvatarGesture("LARM_POINT_LEFT",  12) { BodyPart = Body.LeftArm, Direction = Orientation.Left  };
    public static AvatarGesture LARM_POINT_RIGHT  = new AvatarGesture("LARM_POINT_RIGHT", 13) { BodyPart = Body.LeftArm, Direction = Orientation.Right };

    //
    //  Head gestures
    //

    public static AvatarGesture HEAD_IDLE         = new AvatarGesture("HEAD_IDLE",         0) { BodyPart = Body.Head };
    public static AvatarGesture HEAD_NOD          = new AvatarGesture("HEAD_NOD",          1) { BodyPart = Body.Head };
    public static AvatarGesture HEAD_SHAKE        = new AvatarGesture("HEAD_SHAKE",        2) { BodyPart = Body.Head };
    public static AvatarGesture HEAD_TILT         = new AvatarGesture("HEAD_TILT",         3) { BodyPart = Body.Head };

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
        AddGestureToList(RARM_POINT_RIGHT );

        AddGestureToList(LARM_NUMBER_ONE);
        AddGestureToList(LARM_NUMBER_TWO);
        AddGestureToList(LARM_NUMBER_THREE);
        AddGestureToList(LARM_NUMBER_FOUR);
        AddGestureToList(LARM_NUMBER_FIVE);

        AddGestureToList(LARM_CARRY_FRONT);
        AddGestureToList(LARM_CARRY_BACK);
        AddGestureToList(LARM_CARRY_LEFT);
        AddGestureToList(LARM_CARRY_RIGHT);

        AddGestureToList(LARM_POINT_FRONT);
        AddGestureToList(LARM_POINT_BACK);
        AddGestureToList(LARM_POINT_LEFT);
        AddGestureToList(LARM_POINT_RIGHT);

        AddGestureToList(HEAD_IDLE        );
        AddGestureToList(HEAD_NOD         );
        AddGestureToList(HEAD_SHAKE       );
        AddGestureToList(HEAD_TILT        );
    }

    private static void AddGestureToList(AvatarGesture gesture)
    {
        ALL_GESTURES.Add(gesture.Name.ToLower(), gesture);
    }

    //
    //  Properties
    //

    public string Name
    {
        get;
        private set;
    }

    public int Id // Tells the controller which animation to trigger
    {
        get;
        private set;
    }

    public Body BodyPart // Tells the controller which animation layer to use
    {
        get;
        private set;
    }

    public Orientation Direction // Unused
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
