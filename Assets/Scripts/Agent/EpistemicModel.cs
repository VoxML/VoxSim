using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Episteme;
using Network;

namespace Agent {
	public enum EpistemicCertaintyOperation
	{
		Increase,
		Decrease
	}

	public class EpistemicModel : MonoBehaviour {

		public EpistemicState state;

		public bool engaged;

		public static EpistemicState initModel()
		{
			EpistemicState state = new EpistemicState();
			// creating Concept instances (actions)
			// available types: ACTION, OBJECT, PROPERTY
			// available modes: L, G
			Concept pointG = new Concept ("point", ConceptType.ACTION, ConceptMode.G);
			Concept deixis_thisL = new Concept("THIS", ConceptType.ACTION, ConceptMode.L);
			Concept deixis_thatL = new Concept("THAT", ConceptType.ACTION, ConceptMode.L);
			Concept deixis_thereL = new Concept("THERE", ConceptType.ACTION, ConceptMode.L);
			Concept grabG = new Concept ("grab", ConceptType.ACTION, ConceptMode.G);
			Concept grabL = new Concept ("GRAB", ConceptType.ACTION, ConceptMode.L);
			Concept moveG = new Concept ("move", ConceptType.ACTION, ConceptMode.G);
			//Concept moveL = new Concept ("PUT", ConceptType.ACTION, ConceptMode.L);
			Concept pushG = new Concept ("push", ConceptType.ACTION, ConceptMode.G);
			//Concept pushL = new Concept ("PUSH", ConceptType.ACTION, ConceptMode.L);

			Concept posackG = new Concept("posack", ConceptType.ACTION, ConceptMode.G);
			Concept posackL = new Concept("YES", ConceptType.ACTION, ConceptMode.L);
			Concept negackG = new Concept("negack", ConceptType.ACTION, ConceptMode.G);
			Concept negackL = new Concept("NO", ConceptType.ACTION, ConceptMode.L);

			// todo nevermind as "undo" and nothing as "cancel"
			Concept neverMindL = new Concept("NEVERMIND", ConceptType.ACTION, ConceptMode.L);
			Concept nothingL = new Concept("NOTHING", ConceptType.ACTION, ConceptMode.L);

			// add concepts to the epistemic model
			state.AddConcept(pointG);
			state.AddConcept(deixis_thisL);
			state.AddConcept(deixis_thatL);
			state.AddConcept(deixis_thereL);
			state.AddRelation(pointG, deixis_thisL, true);
			state.AddRelation(pointG, deixis_thatL, true);
			state.AddRelation(pointG, deixis_thereL, true);

			state.AddConcept(grabG);
			state.AddConcept(grabL);
			state.AddRelation(grabG, grabL, true);
			state.AddConcept(moveG);
//			state.AddConcept(moveL);
//			state.AddRelation(moveG, moveL, true);
			state.AddConcept(pushG);
//			state.AddConcept(pushL);
//			state.AddRelation(pushG, pushL, true);

			state.AddConcept(posackG);
			state.AddConcept(posackL);
			state.AddConcept(negackG);
			state.AddConcept(negackL);
			state.AddConcept(neverMindL);
			state.AddConcept(nothingL);
			// add relations between them, third boolean param is bidirectional
			state.AddRelation(posackG, posackL, true);
			state.AddRelation(negackG, negackL, true);

			state.AddPropertyGroup(new PropertyGroup("COLOR", PropertyType.Nominal));
			Concept red = new Concept("RED", ConceptType.PROPERTY, ConceptMode.L);
			Concept green = new Concept("GREEN", ConceptType.PROPERTY, ConceptMode.L);
			Concept yellow = new Concept("YELLOW", ConceptType.PROPERTY, ConceptMode.L);
			Concept orange = new Concept("ORANGE", ConceptType.PROPERTY, ConceptMode.L);
			Concept black = new Concept("BLACK", ConceptType.PROPERTY, ConceptMode.L);
			Concept purple = new Concept("PURPLE", ConceptType.PROPERTY, ConceptMode.L);
			Concept white = new Concept("WHITE", ConceptType.PROPERTY, ConceptMode.L);
			red.SubgroupName = "COLOR";
			green.SubgroupName = "COLOR";
			yellow.SubgroupName = "COLOR";
			orange.SubgroupName = "COLOR";
			black.SubgroupName = "COLOR";
			purple.SubgroupName = "COLOR";
			white.SubgroupName = "COLOR";
			state.AddConcept(red);
			state.AddConcept(green);
			state.AddConcept(yellow);
			state.AddConcept(orange);
			state.AddConcept(black);
			state.AddConcept(purple);
			state.AddConcept(white);

//			state.AddPropertyGroup(new PropertyGroup("SIZE", PropertyType.Ordinal));
//			Concept small = new Concept("SMALL", ConceptType.PROPERTY, ConceptMode.L);
//			Concept big = new Concept("BIG", ConceptType.PROPERTY, ConceptMode.L);
//			small.SubgroupName = "SIZE";
//			big.SubgroupName = "SIZE";
//			state.AddConcept(small);
//			state.AddConcept(big);

			state.AddPropertyGroup(new PropertyGroup("DIRECTION", PropertyType.Nominal));
			Concept left = new Concept("LEFT", ConceptType.PROPERTY, ConceptMode.L);
			Concept right = new Concept("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
			Concept back = new Concept("BACK", ConceptType.PROPERTY, ConceptMode.L);
			Concept forward = new Concept("FRONT", ConceptType.PROPERTY, ConceptMode.L);
			Concept up = new Concept("UP", ConceptType.PROPERTY, ConceptMode.L);
			Concept down = new Concept("DOWN", ConceptType.PROPERTY, ConceptMode.L);
			left.SubgroupName = "DIRECTION";
			right.SubgroupName = "DIRECTION";
			back.SubgroupName = "DIRECTION";
			forward.SubgroupName = "DIRECTION";
			up.SubgroupName = "DIRECTION";
			down.SubgroupName = "DIRECTION";
			state.AddConcept(left);
			state.AddConcept(right);
			state.AddConcept(back);
			state.AddConcept(forward);
			state.AddConcept(up);
			state.AddConcept(down);


			// now add more concepts (objects)
			/*Concept yellowBlock = new Concept("block1", ConceptType.OBJECT, ConceptMode.G);
			Concept smPurpleBlock = new Concept("block2", ConceptType.OBJECT, ConceptMode.G);
			Concept blackBlock = new Concept("block3", ConceptType.OBJECT, ConceptMode.G);
			Concept greenBlock = new Concept("block4", ConceptType.OBJECT, ConceptMode.G);
			Concept orangeBlock = new Concept("block5", ConceptType.OBJECT, ConceptMode.G);
			Concept lgPurpleBlock = new Concept("block7", ConceptType.OBJECT, ConceptMode.G);
			Concept redBlock = new Concept("block6", ConceptType.OBJECT, ConceptMode.G);
			Concept whiteBlock = new Concept("block8", ConceptType.OBJECT, ConceptMode.G);
			state.AddConcept(yellowBlock);
			state.AddConcept(smPurpleBlock);
			state.AddConcept(blackBlock);
			state.AddConcept(greenBlock);
			state.AddConcept(orangeBlock);
			state.AddConcept(redBlock);
			state.AddConcept(lgPurpleBlock);
			state.AddConcept(whiteBlock);*/

			return state;
		}
		
		// Use this for initialization
		void Start () {
			engaged = false;
			state = initModel();

			if (PlayerPrefs.HasKey ("URLs")) {
				string epiSimUrlString = string.Empty;
				foreach (string url in PlayerPrefs.GetString("URLs").Split(';')) {
					if (url.Split ('=') [0] == "EpiSim URL") {
						epiSimUrlString = url.Split ('=') [1];
						string epiSimUrl = !epiSimUrlString.StartsWith ("http://") ? "http://" + epiSimUrlString : epiSimUrlString;
						state.SetEpisimUrl (epiSimUrl);
						state.InitiateEpisim();
						break;
					}
				}
			}
		}

		// Update is called once per frame
		void Update () {
		}
	}
}
