using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;

public class BattleTile : MonoBehaviour{
	public class BattleTileCollider : MonoBehaviour{
		public BattleTile battleTile;
	}

	public MapTile mapTile;
	public TileData data;
	public BattleMech mech;
	public Vector2i pos;
	public int index;

	Battle battle;
	GameObject collider2DGO;

	public void Init(Battle newBattle, int newX, int newY, TileData newData){
		this.battle = newBattle;
		this.pos = new Vector2i(newX, newY);
		this.data = newData;

		this.mapTile = this.battle.mapDisplay.GetTile(this.pos);
		this.index = newX + newY * this.battle.mapSize.x;

		this.mapTile.SetLayersFromData(this.data);

		this.battle.pathNetwork.AddNode(this, this.transform.position);

		this.ApplyData();
	}

	public Vector2 Get2DPosition(){
		return new Vector2(
			this.transform.localPosition.x,
			this.transform.localPosition.z
		);
	}

	// Right is first, then it goes CCW like in math. NOTE: can return null.
	// TODO: Wait, delegate this to higher up
	public BattleTile GetNeighbor(int index){
		int newX = this.pos.x;
		int newY = this.pos.y;

		if(this.mapTile.isAlternate){
			if(index == 0){
				++newX;
			}else if(index == 1){
				++newX;
				++newY;
			}else if(index == 2){
				++newY;
			}else if(index == 3){
				--newX;
			}else if(index == 4){
				--newY;
			}else if(index == 5){
				++newX;
				--newY;
			}
		}else{
			if(index == 0){
				++newX;
			}else if(index == 1){
				++newY;
			}else if(index == 2){
				--newX;
				++newY;
			}else if(index == 3){
				--newX;
			}else if(index == 4){
				--newX;
				--newY;
			}else if(index == 5){
				--newY;
			}
		}

		bool isWithinBounds = (
			newX >= 0 && newX < this.battle.mapSize.x &&
			newY >= 0 && newY < this.battle.mapSize.y
		);

		if(isWithinBounds){
			return this.battle.GetTile(newX, newY);
		}else{
			return null;
		}
	}

	// Visual only
	public void SetRevealed(bool revealed){
		this.mapTile.SetRevealed(revealed);

		if(this.mech){
			this.mech.SetRevealed(revealed);
		}
	}

	public void Recreate(TileData newData){
		this.data = newData;

		this.mapTile.SetLayersFromData(this.data);

		this.ApplyData();
	}

	void ApplyData(){
		this.mapTile.SetLayersFromData(this.data);

		if(this.collider2DGO){
			Destroy(this.collider2DGO);
			this.collider2DGO = null;
		}

		if(this.data.losAmount < 1f){
			this.collider2DGO = (GameObject)Instantiate(Resources.Load("Prefabs/Hex collider 2D"));
			this.collider2DGO.transform.parent = this.battle.world2DGO.transform;
			this.collider2DGO.transform.localPosition = this.Get2DPosition();
			this.collider2DGO.AddComponent<BattleTileCollider>().battleTile = this;
		}

		this.battle.pathNetwork.SetNodeEnabled(this, this.data.allowsMovement);
	}
}
