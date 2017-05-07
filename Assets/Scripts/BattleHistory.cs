using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
struct SerializedHistory{
	public Scenario scenario;
	// scenario's map file name is not used because the map file could have been changed.
	public BattleMap startingMap;
	public int currentTeamIndex;
	// This is really stupid and it makes the output really ugly but it's the easiest way because Unity's json utility
	// is garbo.
	public string[] moveTypeNames;
	public string[] jsonTextOfMoves;
}

[System.Serializable]
public struct BattleHistory{
	public Scenario scenario;
	public BattleMap startingMap;
	public int currentTeamIndex;
	public List<object> moves;

	public string ToJSON(){
		SerializedHistory output = new SerializedHistory();

		output.scenario = this.scenario;
		output.startingMap = this.startingMap;
		output.currentTeamIndex = this.currentTeamIndex;

		output.moveTypeNames = new string[this.moves.Count];
		output.jsonTextOfMoves = new string[this.moves.Count];
		for(int n = 0; n < this.moves.Count; ++n){
			object o = this.moves[n];

			output.moveTypeNames[n] = o.GetType().FullName;

			if(o.GetType() == typeof(BattleMove.Move)){
				var move = (BattleMove.Move)o;
				output.jsonTextOfMoves[n] = JsonUtility.ToJson(move);
			}else if(o.GetType() == typeof(BattleMove.StandingFire)){
				var move = (BattleMove.StandingFire)o;
				output.jsonTextOfMoves[n] = JsonUtility.ToJson(move);
			}else if(o.GetType() == typeof(BattleMove.SetTarget)){
				var move = (BattleMove.SetTarget)o;
				output.jsonTextOfMoves[n] = JsonUtility.ToJson(move);
			}else{
				throw new UnityException();
			}
		}

		string jsonText = JsonUtility.ToJson(output, true);

		return jsonText;
	}

	public void FromJSON(string jsonText){
		SerializedHistory input = JsonUtility.FromJson<SerializedHistory>(jsonText);

		this.scenario = input.scenario;
		this.startingMap = input.startingMap;
		this.currentTeamIndex = input.currentTeamIndex;

		this.moves = new List<object>(input.jsonTextOfMoves.Length);
		for(int n = 0; n < input.jsonTextOfMoves.Length; ++n){
			System.Type type = System.Type.GetType(input.moveTypeNames[n]);
			object move = JsonUtility.FromJson(input.jsonTextOfMoves[n], type);
			this.moves.Add(move);
		}
	}
}
