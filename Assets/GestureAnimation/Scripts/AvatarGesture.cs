using System.Collections.Generic;

public class AvatarGesture {
	public enum Body {
		FullBody = -1,
		RightArm = 0,
		LeftArm,
		Head
	}

	public enum Orientation {
		NA = -1,
		Front = 0,
		Back,
		Left,
		Right,
		Up,
		Down
	}

	//
	//  GESTURES
	//  TODO Eventually, we can define a gesture as a compound action combining arbitrary arm motions and hand poses.
	//

	// Dictionary lookup of gesture name to AvatarGesture
	private static Dictionary<string, AvatarGesture> sGestureList = new Dictionary<string, AvatarGesture>();

	public static Dictionary<string, AvatarGesture> AllGestures {
		get {
			if (sGestureList == null) {
				sGestureList = new Dictionary<string, AvatarGesture>(); // Just in case it is not initialized
			}

			return sGestureList;
		}
	}

	private static void AddGestureToList(AvatarGesture gesture) {
		AllGestures.Add(gesture.Name.ToLower(), gesture);
	}

	//
	//  Right arm gestures
	//

	// Idle
	public static AvatarGesture RARM_IDLE = new AvatarGesture("RARM_IDLE", 0)
		{BodyPart = Body.RightArm, Direction = Orientation.NA};

	// Numeric
	public static AvatarGesture RARM_NUMBER_ONE = new AvatarGesture("RARM_NUMBER_ONE", 1)
		{BodyPart = Body.RightArm, Direction = Orientation.NA};

	public static AvatarGesture RARM_NUMBER_TWO = new AvatarGesture("RARM_NUMBER_TWO", 2)
		{BodyPart = Body.RightArm, Direction = Orientation.NA};

	public static AvatarGesture RARM_NUMBER_THREE = new AvatarGesture("RARM_NUMBER_THREE", 3)
		{BodyPart = Body.RightArm, Direction = Orientation.NA};

	public static AvatarGesture RARM_NUMBER_FOUR = new AvatarGesture("RARM_NUMBER_FOUR", 4)
		{BodyPart = Body.RightArm, Direction = Orientation.NA};

	public static AvatarGesture RARM_NUMBER_FIVE = new AvatarGesture("RARM_NUMBER_FIVE", 5)
		{BodyPart = Body.RightArm, Direction = Orientation.NA};

	// Carry
	public static AvatarGesture RARM_CARRY_FRONT = new AvatarGesture("RARM_CARRY_FRONT", 6)
		{BodyPart = Body.RightArm, Direction = Orientation.Front};

	public static AvatarGesture RARM_CARRY_BACK = new AvatarGesture("RARM_CARRY_BACK", 7)
		{BodyPart = Body.RightArm, Direction = Orientation.Back};

	public static AvatarGesture RARM_CARRY_LEFT = new AvatarGesture("RARM_CARRY_LEFT", 8)
		{BodyPart = Body.RightArm, Direction = Orientation.Left};

	public static AvatarGesture RARM_CARRY_RIGHT = new AvatarGesture("RARM_CARRY_RIGHT", 9)
		{BodyPart = Body.RightArm, Direction = Orientation.Right};

	// Point
	public static AvatarGesture RARM_POINT_FRONT = new AvatarGesture("RARM_POINT_FRONT", 10)
		{BodyPart = Body.RightArm, Direction = Orientation.Front};

	public static AvatarGesture RARM_POINT_BACK = new AvatarGesture("RARM_POINT_BACK", 11)
		{BodyPart = Body.RightArm, Direction = Orientation.Back};

	public static AvatarGesture RARM_POINT_LEFT = new AvatarGesture("RARM_POINT_LEFT", 12)
		{BodyPart = Body.RightArm, Direction = Orientation.Left};

	public static AvatarGesture RARM_POINT_RIGHT = new AvatarGesture("RARM_POINT_RIGHT", 13)
		{BodyPart = Body.RightArm, Direction = Orientation.Right};

	// Push
	public static AvatarGesture RARM_PUSH_FRONT = new AvatarGesture("RARM_PUSH_FRONT", 14)
		{BodyPart = Body.RightArm, Direction = Orientation.Front};

	public static AvatarGesture RARM_PUSH_BACK = new AvatarGesture("RARM_PUSH_BACK", 15)
		{BodyPart = Body.RightArm, Direction = Orientation.Back};

	public static AvatarGesture RARM_PUSH_LEFT = new AvatarGesture("RARM_PUSH_LEFT", 16)
		{BodyPart = Body.RightArm, Direction = Orientation.Left};

	// Thumbs
	public static AvatarGesture RARM_THUMBS_UP = new AvatarGesture("RARM_THUMBS_UP", 17)
		{BodyPart = Body.RightArm, Direction = Orientation.NA};

	public static AvatarGesture RARM_THUMBS_DOWN = new AvatarGesture("RARM_THUMBS_DOWN", 18)
		{BodyPart = Body.RightArm, Direction = Orientation.NA};

	// Carry (updated)
	public static AvatarGesture RARM_CARRY_UP = new AvatarGesture("RARM_CARRY_UP", 19)
		{BodyPart = Body.RightArm, Direction = Orientation.Up};

	public static AvatarGesture RARM_CARRY_DOWN = new AvatarGesture("RARM_CARRY_DOWN", 20)
		{BodyPart = Body.RightArm, Direction = Orientation.Down};

	public static AvatarGesture RARM_CARRY_STILL = new AvatarGesture("RARM_CARRY_STILL", 21)
		{BodyPart = Body.RightArm, Direction = Orientation.NA};

	// Push (updated)
	public static AvatarGesture RARM_PUSH_RIGHT = new AvatarGesture("RARM_PUSH_RIGHT", 22)
		{BodyPart = Body.RightArm, Direction = Orientation.Right};

	// Misc
	public static AvatarGesture RARM_WAVE = new AvatarGesture("RARM_WAVE", 23)
		{BodyPart = Body.RightArm, Direction = Orientation.NA};

	//
	//  Left arm gestures
	//

	// Idle
	public static AvatarGesture LARM_IDLE = new AvatarGesture("LARM_IDLE", 0)
		{BodyPart = Body.LeftArm, Direction = Orientation.NA};

	// Numeric
	public static AvatarGesture LARM_NUMBER_ONE = new AvatarGesture("LARM_NUMBER_ONE", 1)
		{BodyPart = Body.LeftArm, Direction = Orientation.NA};

	public static AvatarGesture LARM_NUMBER_TWO = new AvatarGesture("LARM_NUMBER_TWO", 2)
		{BodyPart = Body.LeftArm, Direction = Orientation.NA};

	public static AvatarGesture LARM_NUMBER_THREE = new AvatarGesture("LARM_NUMBER_THREE", 3)
		{BodyPart = Body.LeftArm, Direction = Orientation.NA};

	public static AvatarGesture LARM_NUMBER_FOUR = new AvatarGesture("LARM_NUMBER_FOUR", 4)
		{BodyPart = Body.LeftArm, Direction = Orientation.NA};

	public static AvatarGesture LARM_NUMBER_FIVE = new AvatarGesture("LARM_NUMBER_FIVE", 5)
		{BodyPart = Body.LeftArm, Direction = Orientation.NA};

	// Carry
	public static AvatarGesture LARM_CARRY_FRONT = new AvatarGesture("LARM_CARRY_FRONT", 6)
		{BodyPart = Body.LeftArm, Direction = Orientation.Front};

	public static AvatarGesture LARM_CARRY_BACK = new AvatarGesture("LARM_CARRY_BACK", 7)
		{BodyPart = Body.LeftArm, Direction = Orientation.Back};

	public static AvatarGesture LARM_CARRY_LEFT = new AvatarGesture("LARM_CARRY_LEFT", 8)
		{BodyPart = Body.LeftArm, Direction = Orientation.Left};

	public static AvatarGesture LARM_CARRY_RIGHT = new AvatarGesture("LARM_CARRY_RIGHT", 9)
		{BodyPart = Body.LeftArm, Direction = Orientation.Right};

	// Point
	public static AvatarGesture LARM_POINT_FRONT = new AvatarGesture("LARM_POINT_FRONT", 10)
		{BodyPart = Body.LeftArm, Direction = Orientation.Front};

	public static AvatarGesture LARM_POINT_BACK = new AvatarGesture("LARM_POINT_BACK", 11)
		{BodyPart = Body.LeftArm, Direction = Orientation.Back};

	public static AvatarGesture LARM_POINT_LEFT = new AvatarGesture("LARM_POINT_LEFT", 12)
		{BodyPart = Body.LeftArm, Direction = Orientation.Left};

	public static AvatarGesture LARM_POINT_RIGHT = new AvatarGesture("LARM_POINT_RIGHT", 13)
		{BodyPart = Body.LeftArm, Direction = Orientation.Right};

	// Push
	public static AvatarGesture LARM_PUSH_FRONT = new AvatarGesture("LARM_PUSH_FRONT", 14)
		{BodyPart = Body.LeftArm, Direction = Orientation.Front};

	public static AvatarGesture LARM_PUSH_BACK = new AvatarGesture("LARM_PUSH_BACK", 15)
		{BodyPart = Body.LeftArm, Direction = Orientation.Back};

	public static AvatarGesture LARM_PUSH_RIGHT = new AvatarGesture("LARM_PUSH_RIGHT", 16)
		{BodyPart = Body.LeftArm, Direction = Orientation.Left};

	// Thumbs
	public static AvatarGesture LARM_THUMBS_UP = new AvatarGesture("LARM_THUMBS_UP", 17)
		{BodyPart = Body.LeftArm, Direction = Orientation.NA};

	public static AvatarGesture LARM_THUMBS_DOWN = new AvatarGesture("LARM_THUMBS_DOWN", 18)
		{BodyPart = Body.LeftArm, Direction = Orientation.NA};

	// Carry (updated)
	public static AvatarGesture LARM_CARRY_UP = new AvatarGesture("LARM_CARRY_UP", 19)
		{BodyPart = Body.LeftArm, Direction = Orientation.Up};

	public static AvatarGesture LARM_CARRY_DOWN = new AvatarGesture("LARM_CARRY_DOWN", 20)
		{BodyPart = Body.LeftArm, Direction = Orientation.Down};

	public static AvatarGesture LARM_CARRY_STILL = new AvatarGesture("LARM_CARRY_STILL", 21)
		{BodyPart = Body.LeftArm, Direction = Orientation.NA};

	// Push (updated)
	public static AvatarGesture LARM_PUSH_LEFT = new AvatarGesture("LARM_PUSH_LEFT", 22)
		{BodyPart = Body.LeftArm, Direction = Orientation.Left};

	// Misc
	public static AvatarGesture LARM_WAVE = new AvatarGesture("LARM_WAVE", 23)
		{BodyPart = Body.LeftArm, Direction = Orientation.NA};


	//
	//  Head gestures
	//

	public static AvatarGesture HEAD_IDLE = new AvatarGesture("HEAD_IDLE", 0) {BodyPart = Body.Head};
	public static AvatarGesture HEAD_NOD = new AvatarGesture("HEAD_NOD", 1) {BodyPart = Body.Head};
	public static AvatarGesture HEAD_SHAKE = new AvatarGesture("HEAD_SHAKE", 2) {BodyPart = Body.Head};
	public static AvatarGesture HEAD_TILT = new AvatarGesture("HEAD_TILT", 3) {BodyPart = Body.Head};

	//
	//  Properties
	//

	public string Name { get; private set; }

	// TODO May be better to create a separate named trigger for each animation added... then won't need IDs
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

	// Allows for the creation of arbitrary gestures assuming you know the animation id
	public AvatarGesture(int id) {
		Id = id;
	}

	// Name is used internally for a lookup
	private AvatarGesture(string name, int id) {
		Name = name;
		Id = id;

		// Add self to master list
		AddGestureToList(this);
	}
}