using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Episteme;

namespace Agent {
	public class EpistemicModel : MonoBehaviour {

		public EpistemicState state;

		public bool engaged;

		// Use this for initialization
		void Start () {
			engaged = false;
			state = new EpistemicState();
			// creating Concept instances (actions)
			// available types: ACTION, OBJECT, PROPERTY
			// available modes: L, G
			Concept pointG = new Concept ("point", ConceptType.ACTION, ConceptMode.G);
			Concept deixis_thisL = new Concept("THIS", ConceptType.ACTION, ConceptMode.L);
			Concept deixis_thatL = new Concept("THAT", ConceptType.ACTION, ConceptMode.L);
			Concept grabG = new Concept ("grab", ConceptType.ACTION, ConceptMode.G);
			Concept grabL = new Concept ("GRAB", ConceptType.ACTION, ConceptMode.L);
			Concept moveG = new Concept ("move", ConceptType.ACTION, ConceptMode.G);
			Concept moveL = new Concept ("PUT", ConceptType.ACTION, ConceptMode.L);
			Concept pushG = new Concept ("push", ConceptType.ACTION, ConceptMode.G);
			Concept pushL = new Concept ("PUSH", ConceptType.ACTION, ConceptMode.L);

			Concept posackG = new Concept("posack", ConceptType.ACTION, ConceptMode.G);
			Concept posackL = new Concept("YES", ConceptType.ACTION, ConceptMode.L);
			Concept negackG = new Concept("negack", ConceptType.ACTION, ConceptMode.G);
			Concept negackL = new Concept("NO", ConceptType.ACTION, ConceptMode.L);

			// add concepts to the epistemic model
			state.AddConcept(pointG);
			state.AddConcept(deixis_thisL);
			state.AddConcept(deixis_thatL);
			state.AddRelation(pointG, deixis_thisL, true);
			state.AddRelation(pointG, deixis_thatL, true);

			state.AddConcept(grabG);
			state.AddConcept(grabL);
			state.AddRelation(grabG, grabL, true);
			state.AddConcept(moveG);
			state.AddConcept(moveL);
			state.AddRelation(moveG, moveL, true);
			state.AddConcept(pushG);
			state.AddConcept(pushL);
			state.AddRelation(pushG, pushL, true);

			state.AddConcept(posackG);
			state.AddConcept(posackL);
			state.AddConcept(negackG);
			state.AddConcept(negackL);
			// add relations between them, third boolean param is bidirectional
			state.AddRelation(posackG, posackL, true);
			state.AddRelation(negackG, negackL, true);

			Concept red = new Concept("RED", ConceptType.PROPERTY, ConceptMode.L);
			Concept green = new Concept("GREEN", ConceptType.PROPERTY, ConceptMode.L);
			Concept yellow = new Concept("YELLOW", ConceptType.PROPERTY, ConceptMode.L);
			Concept orange = new Concept("ORANGE", ConceptType.PROPERTY, ConceptMode.L);
			Concept black = new Concept("BLACK", ConceptType.PROPERTY, ConceptMode.L);
			Concept purple = new Concept("PURPLE", ConceptType.PROPERTY, ConceptMode.L);
			Concept white = new Concept("WHITE", ConceptType.PROPERTY, ConceptMode.L);

			Concept left = new Concept("LEFT", ConceptType.PROPERTY, ConceptMode.L);
			Concept right = new Concept("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
			Concept forward = new Concept("FORWARD", ConceptType.PROPERTY, ConceptMode.L);
			Concept back = new Concept("BACK", ConceptType.PROPERTY, ConceptMode.L);
			Concept up = new Concept("UP", ConceptType.PROPERTY, ConceptMode.L);
			Concept down = new Concept("DOWN", ConceptType.PROPERTY, ConceptMode.L);
			Concept big = new Concept("BIG", ConceptType.PROPERTY, ConceptMode.L);
			Concept small = new Concept("SMALL", ConceptType.PROPERTY, ConceptMode.L);
			state.AddConcept(red);
			state.AddConcept(green);
			state.AddConcept(yellow);
			state.AddConcept(orange);
			state.AddConcept(black);
			state.AddConcept(purple);
			state.AddConcept(white);

			state.AddConcept(left);
			state.AddConcept(right);
			state.AddConcept(forward);
			state.AddConcept(back);
			state.AddConcept(up);
			state.AddConcept(down);
			state.AddConcept(big);
			state.AddConcept(small);

			// now add more concepts (objects)
			Concept redBlock = new Concept("block1", ConceptType.OBJECT, ConceptMode.L);
			Concept greenBlock = new Concept("block2", ConceptType.OBJECT, ConceptMode.L);
			Concept yellowBlock = new Concept("block3", ConceptType.OBJECT, ConceptMode.L);
			Concept orangeBlock = new Concept("block4", ConceptType.OBJECT, ConceptMode.L);
			Concept blackBlock = new Concept("block5", ConceptType.OBJECT, ConceptMode.L);
			Concept lgPurpleBlock = new Concept("block6", ConceptType.OBJECT, ConceptMode.L);
			Concept smPurpleBlock = new Concept("block7", ConceptType.OBJECT, ConceptMode.L);
			Concept whiteBlock = new Concept("block8", ConceptType.OBJECT, ConceptMode.L);
			state.AddConcept(redBlock);
			state.AddConcept(greenBlock);
			state.AddConcept(yellowBlock);
			state.AddConcept(orangeBlock);
			state.AddConcept(blackBlock);
			state.AddConcept(lgPurpleBlock);
			state.AddConcept(smPurpleBlock);
			state.AddConcept(whiteBlock);

			string url = PlayerPrefs.GetString ("EpiSim URL");
			url = !url.StartsWith ("http://") ? "http://" + url : url;
			state.SetEpisimUrl(url);

			state.InitiateEpisim();
		}
		
		// Update is called once per frame
		void Update () {
			
		}
	}
}
