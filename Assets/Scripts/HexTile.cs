using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;

public class HexTile :
	MonoBehaviour,
	IPointerEnterHandler,
	IPointerExitHandler,
	IPointerClickHandler
{
	public static float depthSpacing = 0.01f;

	public TileData data;
	public BattleMech mech;
	public Vector2i pos;
	public int index;

	bool isAlternate;
	// TODO: Make static
	Vector3[] hexVerts;
	Battle battle;
	Vector3 cameraDir;
	GameObject groundGO;
	GameObject wallGO;
	GameObject spriteGO;
	GameObject collider2DGO;

	public void Init(Battle newBattle, int newX, int newY, TileData newData){
		Assert.IsTrue(newData.groundMaterial != null);

		this.battle = newBattle;
		this.pos = new Vector2i(newX, newY);
		this.data = newData;

		this.index = newX + newY * this.battle.mapSize.x;
		this.cameraDir = this.battle.gameCamera.transform.rotation * Vector3.forward;

		this.hexVerts = new Vector3[6];
		for(int n = 0; n < this.hexVerts.Length; ++n){
			Vector3 pos = (
				Quaternion.AngleAxis ((float)n / 6f * 360f, Vector3.up) *
				new Vector3 (0, 0, this.battle.hexRadius)
			);
			this.hexVerts [n] = pos;
		}

		float xPos = (float)this.pos.x * this.battle.hexSpacingX;
		this.isAlternate = this.pos.y % 2 == 1;
		if (this.isAlternate) {
			xPos += this.battle.hexSpacingX * 0.5f;
		}
		float zPos = (float)this.pos.y * this.battle.hexSpacingY;
		this.transform.position = new Vector3 (xPos, 0, zPos);

		GameObject go = new GameObject("Collision");
		this.Attach(go.transform, 0);
		go.AddComponent<MeshFilter> ().mesh = Resources.Load<Mesh>("Models/HexTile");
		go.AddComponent<MeshCollider>();

		this.battle.pathNetwork.AddNode(this, this.transform.position);

		this.ApplyData();
	}

	// Right is first, then it goes CCW like in math. NOTE: can return null.
	public HexTile GetNeighbor(int index){
		int newX = this.pos.x;
		int newY = this.pos.y;

		if(this.isAlternate){
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

	public Vector2 Get2DPosition(){
		return new Vector2(
			this.transform.localPosition.x,
			this.transform.localPosition.z
		);
	}

	public void Attach(Transform otherTransform, int depthLayer){
		otherTransform.parent = this.transform;
		float depth = (float)depthLayer * HexTile.depthSpacing;
		otherTransform.localPosition = -this.cameraDir * depth;
	}

	public void AttachForeground(Transform otherTransform, int depthLayer){
		float extraDepth = -this.battle.gameCamera.transform.localPosition.z * 0.5f;

		otherTransform.parent = this.transform;
		float depth = (float)depthLayer * HexTile.depthSpacing + extraDepth;
		otherTransform.localPosition = -this.cameraDir * depth;
	}

	public void Recreate(TileData newData){
		Destroy(this.groundGO);
		if(this.wallGO){
			Destroy(this.wallGO);
			this.wallGO = null;
		}
		if(this.spriteGO){
			Destroy(this.spriteGO);
			this.spriteGO = null;
		}
		if(this.collider2DGO){
			Destroy(this.collider2DGO);
			this.collider2DGO = null;
		}

		this.data = newData;

		this.ApplyData();
	}

	// Visual only
	public void SetRevealed(bool revealed){
		Color color = revealed ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);

		this.groundGO.GetComponent<MeshRenderer>().material.color = color;
		if(this.wallGO){
			this.wallGO.GetComponent<MeshRenderer>().material.color = color;
		}
		if(this.spriteGO){
			this.spriteGO.GetComponent<SpriteRenderer>().color = color;
		}

		if(this.mech){
			this.mech.SetRevealed(revealed);
		}
	}

	void ApplyData(){
		this.groundGO = new GameObject("Ground model");
		this.Attach(this.groundGO.transform, 0);
		this.groundGO.AddComponent<MeshFilter>().mesh = Resources.Load<Mesh>("Models/HexTile");
		this.groundGO.AddComponent<MeshRenderer>().sharedMaterial = this.data.groundMaterial;

		if(this.data.wallMaterial){
			this.groundGO.transform.localPosition = Vector3.up * this.battle.hexSideLength * 2f;

			this.wallGO = new GameObject ("Wall model");
			this.Attach(this.wallGO.transform, 0);
			this.wallGO.AddComponent<MeshFilter> ().mesh = Resources.Load<Mesh>("Models/HexTileWall");
			this.wallGO.AddComponent<MeshRenderer> ().sharedMaterial = this.data.wallMaterial;
		}

		if(this.data.sprite){
			this.spriteGO = this.battle.CreateSprite(this.data.sprite, this.transform);
			this.spriteGO.name = "Tile sprite";
		}

		if(this.data.losAmount < 1f){
			this.collider2DGO = (GameObject)Instantiate(Resources.Load("Prefabs/Hex collider 2D"));
			this.collider2DGO.transform.parent = this.battle.world2DGO.transform;
			this.collider2DGO.transform.localPosition = this.Get2DPosition();
			this.collider2DGO.AddComponent<HexTileCollider>().tile = this;
		}

		this.battle.pathNetwork.SetNodeEnabled(this, this.data.allowsMovement);
	}

	void OnDrawGizmosSelected(){
		Gizmos.color = Color.white;

		for(int n = 0; n < this.hexVerts.Length; ++n){
			int n2 = (n + 1) % this.hexVerts.Length;
			Gizmos.DrawLine (
				this.transform.position + this.hexVerts[n],
				this.transform.position + this.hexVerts[n2]
			);
		}
	}

	public void OnPointerEnter(PointerEventData eventData){
		this.battle.HexTileMouseEvent(this, Battle.MouseEventType.Enter);
	}

	public void OnPointerExit(PointerEventData eventData){
		this.battle.HexTileMouseEvent(this, Battle.MouseEventType.Exit);
	}

	public void OnPointerClick(PointerEventData eventData){
		if(eventData.button == 0){
			this.battle.HexTileMouseEvent(this, Battle.MouseEventType.Click);
		}
	}
}

public class HexTileCollider : MonoBehaviour{
	public HexTile tile;
}
