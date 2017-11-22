﻿using System.Collections;
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
			Concept moveL = new Concept ("PUT", ConceptType.ACTION, ConceptMode.G);
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
			state.AddConcept(red);
			state.AddConcept(green);
			state.AddConcept(yellow);
			state.AddConcept(orange);
			state.AddConcept(black);
			state.AddConcept(purple);
			state.AddConcept(white);

			// now add more concepts (objects)
			Concept redBlock = new Concept("red_block", ConceptType.OBJECT, ConceptMode.L);
			Concept greenBlock = new Concept("green_block", ConceptType.OBJECT, ConceptMode.L);
			Concept yellowBlock = new Concept("yellow_block", ConceptType.OBJECT, ConceptMode.L);
			Concept orangeBlock = new Concept("orange_block", ConceptType.OBJECT, ConceptMode.L);
			Concept blackBlock = new Concept("black_block", ConceptType.OBJECT, ConceptMode.L);
			Concept purpleBlock = new Concept("purple_block", ConceptType.OBJECT, ConceptMode.L);
			Concept whiteBlock = new Concept("white_block", ConceptType.OBJECT, ConceptMode.L);
			state.AddConcept(redBlock);
			state.AddConcept(greenBlock);
			state.AddConcept(yellowBlock);
			state.AddConcept(orangeBlock);
			state.AddConcept(blackBlock);
			state.AddConcept(purpleBlock);
			state.AddConcept(whiteBlock);

			state.SetEpisimUrl(PlayerPrefs.GetString("EpiSimURL"));

			state.InitiateEpisim();
		}
		
		// Update is called once per frame
		void Update () {
			
		}
	}
}
