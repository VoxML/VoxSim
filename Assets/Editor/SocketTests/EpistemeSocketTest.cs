using UnityEngine;
using System.Threading;

using NUnit.Framework;
using VoxSimPlatform.Agent;
using VoxSimPlatform.Episteme;

public class EpistemeSocketTest : MonoBehaviour {
	private EpistemicState model;

	private void mock() {
		model = new EpistemicState();
		// creating Concept instances (actions)
		// available types: ACTION, OBJECT, PROPERTY
		// available modes: L, G
		Concept yesL = new Concept("posack", ConceptType.ACTION, ConceptMode.L);
		Concept yesG = new Concept("POSACK", ConceptType.ACTION, ConceptMode.G);
		Concept noL = new Concept("negack", ConceptType.ACTION, ConceptMode.L);
		Concept noG = new Concept("NEGACK", ConceptType.ACTION, ConceptMode.G);

		// add concepts to the epistemic model
		model.AddConcept(yesL);
		model.AddConcept(yesG);
		model.AddConcept(noL);
		model.AddConcept(noG);
		// add relations between them, third boolean param is bidirectional
		model.AddRelation(yesG, yesL, true);
		model.AddRelation(noG, noL, true);

		// now add more concepts (objects)
		Concept redBlock = new Concept("RED", ConceptType.OBJECT, ConceptMode.L);
		Concept blueBlock = new Concept("BLUE", ConceptType.OBJECT, ConceptMode.L);
		Concept greenBlock = new Concept("GREEN", ConceptType.OBJECT, ConceptMode.L);
		model.AddConcept(redBlock);
		model.AddConcept(blueBlock);
		model.AddConcept(greenBlock);

		model.SetEpisimUrl("http://localhost:5000");
	}

	private void second_mock() {
		model = new EpistemicState();
		// creating Concept instances (actions)
		// available types: ACTION, OBJECT, PROPERTY
		// available modes: L, G
		Concept pushG = new Concept("push", ConceptType.ACTION, ConceptMode.G);
		Concept rightL = new Concept("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
		model.AddConcept(pushG);
		model.AddConcept(rightL);

		// now add more concepts (objects)

		model.SetEpisimUrl("http://localhost:5000");
	}

	[Test]
	public void TestInit() {
		mock();

		// for logging
		var json = Jsonifier.JsonifyEpistemicStateInitiation(model);
		Debug.Log(json);

		// this would do the actual http request
		model.InitiateEpisim();
	}

	[Test]
	public void TestUpdate() {
		mock();

		// retrieve a concept
		var yesL = model.GetConcept("posack", ConceptType.ACTION, ConceptMode.L);
		// set certainty value
		yesL.Certainty = 1.0;
		// retrive a relation
		var yesG = model.GetConcept("POSACK", ConceptType.ACTION, ConceptMode.G);
		var r = model.GetRelation(yesL, yesG);
		// set certainty value
		if (r != null) {
			r.Certainty = 1.0;
		}

		// for logging
		var json = Jsonifier.JsonifyUpdates(model, new[] {yesL}, new[] {r});
		Debug.Log(json);

		// this would do the actual http request: need to pass two arrays for each 
		model.UpdateEpisim(new[] {yesL}, new[] {r});
	}

	[Test]
	public void TestUpdate2() {
		second_mock();
		model.InitiateEpisim();
		var json = Jsonifier.JsonifyEpistemicStateInitiation(model);
		Debug.Log(json);

		var conceptG = model.GetConcept("push", ConceptType.ACTION, ConceptMode.G);
		Debug.Log(conceptG);
		var conceptL = model.GetConcept("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
		Debug.Log(conceptL);
		if (conceptG.Certainty < 0.5 || conceptL.Certainty < 0.5) {
			conceptG.Certainty = 0.5;
			conceptL.Certainty = 0.5;
			json = Jsonifier.JsonifyUpdates(model, new[] {conceptG, conceptL}, new Relation[] { });
			Debug.Log(json);
			model.UpdateEpisim(new[] {conceptG, conceptL}, new Relation[] { });
		}
	}

	[Test]
	public void TestSubgroup() {
		mock();
		model.AddPropertyGroup(new PropertyGroup("SIZE", PropertyType.Ordinal));
		model.AddPropertyGroup(new PropertyGroup("COLOR", PropertyType.Nominal));
		Concept round = new Concept("round", ConceptType.PROPERTY, ConceptMode.L);
		Concept big = new Concept("big", ConceptType.PROPERTY, ConceptMode.L);
		big.SubgroupName = "SIZE";
		Concept small = new Concept("small", ConceptType.PROPERTY, ConceptMode.L);
		small.SubgroupName = "SIZE";
		Concept blue = new Concept("blue", ConceptType.PROPERTY, ConceptMode.L);
		blue.SubgroupName = "COLOR";
		Concept red = new Concept("red", ConceptType.PROPERTY, ConceptMode.L);
		red.SubgroupName = "COLOR";
		model.AddConcept(round);
		model.AddConcept(big);
		model.AddConcept(small);
		model.AddConcept(blue);
		model.AddConcept(red);
		var json = Jsonifier.JsonifyEpistemicStateInitiation(model);
		Debug.Log(json);
		model.InitiateEpisim();
	}

	[Test]
	public void TestActualModel() {
		model = EpistemicModel.initModel();
		model.SetEpisimUrl("http://localhost:5000");
//		var json = Jsonifier.JsonifyEpistemicState(model);
//		Debug.Log(json);
		model.InitiateEpisim();
		var moveL = model.GetConcept("PUT", ConceptType.ACTION, ConceptMode.L);
		var pushL = model.GetConcept("PUSH", ConceptType.ACTION, ConceptMode.L);
		if ((moveL != null) && (pushL != null)) {
			moveL.Certainty = -1;
			pushL.Certainty = -1;
			var json = Jsonifier.JsonifyEpistemicStateInitiation(model);
			Debug.Log(json);
			model.UpdateEpisim(new[] {moveL, pushL}, new Relation[] { });
		}
	}

	[Test]
	public void TestDisengage() {
		mock();
		model.InitiateEpisim();
		Thread.Sleep(2000);
		model.DisengageEpisim();
		Thread.Sleep(2000);
	}

	[Test]
	public void TestSideload() {
		mock();
		// retrieve a concept
		var yesL = model.GetConcept("posack", ConceptType.ACTION, ConceptMode.L);
		// set certainty value
		yesL.Certainty = .99;
		// retrive a relation
		var yesG = model.GetConcept("POSACK", ConceptType.ACTION, ConceptMode.G);
		var r = model.GetRelation(yesL, yesG);
		// set certainty value
		if (r != null) {
			r.Certainty = 0.43;
		}

		// for logging
		var json = Jsonifier.JsonifyUpdates(model, new[] {yesL}, new[] {r});
		Debug.Log(json);
		mock();
		model.SideloadCertaintyState(json);
	}
}