using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMech : MonoBehaviour{
	// Uuuugh this is so ugly
	public struct APCostResult{
		public float ap;
		public bool isValid;
	}

	public Battle battle;
	public MechData data;
	public HexTile tile;
	public float hp;
	public float maxActionPoints;
	public float actionPoints;
	public BattleTeam team;
	public BattleMechAi ai;
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

		this.spriteGO = this.battle.CreateSprite(this.data.sprite, this.transform);
		this.spriteGO.name = "Mech sprite";
		// Gotta figure out how to do this properly, maybe shift the sprites pixels up in the image?
//		this.spriteGO.transform.localPosition = new Vector3(0f, 0f, -0.2f);
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

			float mult1 = ((HexTile)tiles[n - 1]).data.movementSpeedMult;
			float mult2 = ((HexTile)tiles[n]).data.movementSpeedMult;
			float speedMult = (mult1 + mult2) * 0.5f;

			bool isFiring = this.fireAuto && this.target && this.battle.TestLOS((HexTile)tiles[n], this.target.tile);

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

	public void SetDirection(MechDirection dir){
		float scaleX = dir == MechDirection.Right ? -1f : 1f;
		this.transform.localScale = new Vector3(scaleX, 1f, 1f);
	}

	// Visual only
	public void SetRevealed(bool revealed){
		this.spriteGO.SetActive(revealed);
	}

	public GameObject CreateGhost(MechDirection direction){
		GameObject go = this.battle.CreateSprite(this.data.sprite);
		go.name = "Spooky ghost mech";

		float scaleX = direction == MechDirection.Right ? -1f : 1f;
		go.transform.localScale = new Vector3(scaleX, 1f, 1f);

		float alpha = 0.5f;
		go.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, alpha);

		return go;
	}
}
