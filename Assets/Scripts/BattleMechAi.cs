using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMechAi{
	public Battle battle;
	public BattleMech ourMech;

	public BattleMechAi(BattleMech newMech){
		this.ourMech = newMech;
		this.battle = this.ourMech.battle;
	}

	public void Update(){
		// Test: path to a target mech, stop when we can see it and keep firing.
		BattleMech target = null;
		foreach(BattleTeam team in this.battle.teams){
			foreach(BattleMech mech in team.mechs){
				if(team != this.ourMech.team){
					target = mech;
					break;
				}
			}
		}

		if(target == null){
			return;
		}

		HexTile ourTile = this.ourMech.tile;
		HexTile targetTile = target.tile;

		bool canSeeTarget = this.battle.TestLOS(ourTile, targetTile);

		// TODO: this is really stupid
		this.battle.pathNetwork.SetNodeEnabled(ourTile, true);
		this.battle.pathNetwork.SetNodeEnabled(targetTile, true);
		PathingResult result = this.battle.pathNetwork.FindPath(ourTile, targetTile);
		this.battle.pathNetwork.SetNodeEnabled(ourTile, false);
		this.battle.pathNetwork.SetNodeEnabled(targetTile, false);

		if(result.isValid == false){
			return;
		}

		HexTile nextTile = (HexTile)result.nodes[1];

		if(canSeeTarget && result.distance <= 5){
			var move = new BattleMove.StandingFire();
			move.mechIndex = ourTile.index;
			move.targetMechIndex = targetTile.index;
			this.battle.ExecuteMove(move);
		}else{
			var move = new BattleMove.Move();
			move.mechIndex = ourTile.index;
			move.newIndex = nextTile.index;
			if(canSeeTarget){
				move.isFiring = true;
				move.targetMechIndex = targetTile.index;
			}else{
				move.isFiring = false;
			}
			this.battle.ExecuteMove(move);
		}

		this.battle.SetState(BattleState.EndOfAction);
	}
}
