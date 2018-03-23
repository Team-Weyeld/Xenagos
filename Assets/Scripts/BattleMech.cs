using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: not needed as a MonoBehaviour anymore?
public class BattleMech : MonoBehaviour{
	// Uuuugh this is so ugly
	public struct APCostResult{
		public float ap;
		public bool isValid;
	}

	public Battle battle;
	public MechData data;
	public BattleTile tile;
	public MapTile mapTile;
	public float hp;
	public float maxActionPoints;
	public float actionPoints;
	public BattleTeam team;
	public BattleMech target;
	public bool fireAuto;
	public bool isDestroyed;

	GameObject spriteGO;

	public void Init(Battle newBattle, MechData mechData){
		this.battle = newBattle;
		this.data = mechData;

		this.hp = this.data.maxHP;
		this.maxActionPoints = 5f; // temporary constant
		this.actionPoints = this.maxActionPoints;
		this.fireAuto = true;
		this.isDestroyed = false;
	}

	// Hmmm, maybe change this to a function that calculates moving only one tile? yeah that makes a lot more sense...
	// TODO
	public APCostResult GetAPCostForMove(List<object> tiles){
		APCostResult result;

		float apUsedTotal = 0f;
		float apUsedToSecondLast = 0f;
		for(int n = 1; n < tiles.Count; ++n){
			bool isLast = n == tiles.Count - 1;

			if(isLast){
				apUsedToSecondLast = apUsedTotal;
			}

			float mult1 = ((BattleTile)tiles[n - 1]).data.movementSpeedMult;
			float mult2 = ((BattleTile)tiles[n]).data.movementSpeedMult;
			float speedMult = (mult1 + mult2) * 0.5f;

			bool isFiring = this.fireAuto && this.target && this.battle.TestLOS(this.tile, this.target.tile);

			float movementCost = this.data.movementCost;
			if(isFiring){
				movementCost *= 1.25f;
			}

			apUsedTotal += movementCost / speedMult;
		}

		result.ap = apUsedTotal;
		result.isValid = this.actionPoints - apUsedToSecondLast > 0;

		return result;
	}

	public APCostResult GetAPCostForStandingFire(){
		APCostResult result;

		result.ap = 1f;
		result.isValid = true;

		return result;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////
	// Visual only

	public void PlaceAtMapTile(MapTile newMapTile){
		if(this.mapTile){
			this.mapTile.RemoveLayer(MapTile.Layer.MainSprite);
		}

		this.mapTile = newMapTile;

		this.mapTile.SetLayer(MapTile.Layer.MainSprite, sprite: this.data.sprite);
	}

	public void SetDirection(MechDirection dir){
		this.mapTile.SetLayer(
			MapTile.Layer.MainSprite,
			sprite: this.data.sprite,
			flipX: dir == MechDirection.Right
		);
	}

	public void SetRevealed(bool revealed){
		this.mapTile.SetLayerVisible(MapTile.Layer.MainSprite, revealed);
	}
}
