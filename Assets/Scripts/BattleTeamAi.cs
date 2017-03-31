using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleTeamAi{
	public BattleTeam ourTeam;

	public BattleTeamAi(BattleTeam newTeam){
		this.ourTeam = newTeam;
	}

	public void Update(){
		foreach(BattleMech mech in this.ourTeam.mechs){
			mech.ai.Update();
		}
	}
}
