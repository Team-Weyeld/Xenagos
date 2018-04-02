using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class BattleTeamAi{
	public BattleTeam ourTeam;

	int mechUpdateIndex;

	public BattleTeamAi(BattleTeam newTeam){
		this.ourTeam = newTeam;

		this.mechUpdateIndex = -1;
	}

	public void Update(){
		BattleMech currentMech = null;

		// Select the next mech, or the first one with enough AP
		int mechCount = this.ourTeam.mechs.Count;
		for(int n = 0; n < mechCount; ++n){
			this.mechUpdateIndex = (this.mechUpdateIndex + 1) % mechCount;
			currentMech = this.ourTeam.mechs[this.mechUpdateIndex];
			if(currentMech.actionPoints > 0){
				break;
			}
		}
		Assert.IsTrue(currentMech.actionPoints > 0);

		currentMech.ai.Update();
	}
}
