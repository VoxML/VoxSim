using Episteme;
using NUnit.Framework;
using UnityEngine;

public class EpisemeClientTest : MonoBehaviour
{
	private EpistemicState model;

	private void mock()
	{
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

	[Test]
	public void TestInit()
	{
		mock();
		
		// for logging
		var json = Jsonifier.JsonifyEpistemicState(model);
		Debug.Log(json);
		
		// this would do the actual http request
		model.InitiateEpisim();
	}

	[Test]
	public void TestUpdate()
	{
		mock();
		
		// retrieve a concept
		var yesL = model.GetConcept("posack", ConceptType.ACTION, ConceptMode.L);
		// set certainty value
		yesL.Certainty = 1.0;
		// retrive a relation
		var yesG = model.GetConcept("POSACK", ConceptType.ACTION, ConceptMode.G);
		var r = model.GetRelation(yesL, yesG);
		// set certainty value
		if (r != null)
		{
			r.Certainty = 1.0;
		}
		
		// for logging
		var json = Jsonifier.JsonifyUpdates(model, new[] {yesL}, new[] {r});
		Debug.Log(json);
		
		// this would do the actual http request: need to pass two arrays for each 
		model.UpdateEpisim(new[] {yesL}, new[] {r});
	}

}