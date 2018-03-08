using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;

public class MapTile :
	MonoBehaviour,
	IPointerEnterHandler,
	IPointerExitHandler,
	IPointerClickHandler,
	IPointerDownHandler,
	IPointerUpHandler
{
	public static float depthSpacing = 0.01f;
	public static float hexSpacingX = 1f;
	public static float hexRadius = 1f / Mathf.Cos (2f * Mathf.PI / 12f) * hexSpacingX * 0.5f;
	public static float hexSideLength = Mathf.Sin (2f * Mathf.PI / 12f) * hexRadius * 2f;
	public static float hexSpacingY = hexRadius + hexSideLength * 0.5f;

	public MapDisplay map;
	public Vector2i pos;
	public bool isAlternate;

	// TODO: Make static
	Vector3[] hexVerts;
	GameObject groundGO;
	GameObject wallGO;
	GameObject spriteGO;

	public void Init(MapDisplay newMapDisplay, Vector2i newPos){
		this.map = newMapDisplay;
		this.pos = newPos;

		this.hexVerts = new Vector3[6];
		for(int n = 0; n < this.hexVerts.Length; ++n){
			Vector3 pos = (
				Quaternion.AngleAxis ((float)n / 6f * 360f, Vector3.up) *
				new Vector3 (0, 0, hexRadius)
			);
			this.hexVerts [n] = pos;
		}

		float xPos = (float)this.pos.x * hexSpacingX;
		this.isAlternate = this.pos.y % 2 == 1;
		if (this.isAlternate) {
			xPos += hexSpacingX * 0.5f;
		}
		float zPos = (float)this.pos.y * hexSpacingY;
		this.transform.position = new Vector3 (xPos, 0, zPos);

		GameObject go = new GameObject("Collision");
		go.transform.parent = this.transform;
		go.transform.localPosition = Vector3.zero;
		go.AddComponent<MeshFilter>().mesh = Resources.Load<Mesh>("Models/HexTile");
		go.AddComponent<MeshCollider>();
	}

	public void Attach(Transform otherTransform, int depthLayer){
		otherTransform.parent = this.transform;
		float depth = (float)depthLayer * depthSpacing;
		otherTransform.localPosition = -this.map.cameraDir * depth;
	}

	public void AttachForeground(Transform otherTransform, int depthLayer){
		float extraDepth = -this.map.gameCamera.transform.localPosition.z * 0.5f;

		otherTransform.parent = this.transform;
		float depth = (float)depthLayer * depthSpacing + extraDepth;
		otherTransform.localPosition = -this.map.cameraDir * depth;
	}

	public void SetRevealed(bool revealed){
		Color color = revealed ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);

		this.groundGO.GetComponent<MeshRenderer>().material.color = color;
		if(this.wallGO){
			this.wallGO.GetComponent<MeshRenderer>().material.color = color;
		}
		if(this.spriteGO){
			this.spriteGO.GetComponent<SpriteRenderer>().color = color;
		}
	}

	public void SetData(TileData data){
		if(this.groundGO){
			Destroy(this.groundGO);
			this.groundGO = null;
		}
		if(this.wallGO){
			Destroy(this.wallGO);
			this.wallGO = null;
		}
		if(this.spriteGO){
			Destroy(this.spriteGO);
			this.spriteGO = null;
		}

		this.groundGO = new GameObject("Ground model");
		this.Attach(this.groundGO.transform, 0);
		this.groundGO.AddComponent<MeshFilter>().mesh = Resources.Load<Mesh>("Models/HexTile");
		this.groundGO.AddComponent<MeshRenderer>().sharedMaterial = data.groundMaterial;

		if(data.wallMaterial){
			this.groundGO.transform.localPosition = Vector3.up * hexSideLength * 2f;

			this.wallGO = new GameObject ("Wall model");
			this.Attach(this.wallGO.transform, 0);
			this.wallGO.AddComponent<MeshFilter> ().mesh = Resources.Load<Mesh>("Models/HexTileWall");
			this.wallGO.AddComponent<MeshRenderer> ().sharedMaterial = data.wallMaterial;
		}

		if(data.sprite){
			this.spriteGO = this.map.CreateSprite(data.sprite, this.transform);
			this.spriteGO.name = "Tile sprite";
		}
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
		this.map.eventListener.MouseEvent(this, MapDisplay.MouseEventType.Enter);
	}

	public void OnPointerExit(PointerEventData eventData){
		this.map.eventListener.MouseEvent(this, MapDisplay.MouseEventType.Exit);
	}

	public void OnPointerClick(PointerEventData eventData){
		if(eventData.button == PointerEventData.InputButton.Left){
			this.map.eventListener.MouseEvent(this, MapDisplay.MouseEventType.Click);
		}else if(eventData.button == PointerEventData.InputButton.Right){
			this.map.eventListener.MouseEvent(this, MapDisplay.MouseEventType.RightClick);
		}
	}

	public void OnPointerDown(PointerEventData eventData){
		if(eventData.button == PointerEventData.InputButton.Left){
			this.map.eventListener.MouseEvent(this, MapDisplay.MouseEventType.ClickDown);
		}else if(eventData.button == PointerEventData.InputButton.Right){
			this.map.eventListener.MouseEvent(this, MapDisplay.MouseEventType.RightClickDown);
		}
	}

	public void OnPointerUp(PointerEventData eventData){
		if(eventData.button == PointerEventData.InputButton.Left){
			this.map.eventListener.MouseEvent(this, MapDisplay.MouseEventType.ClickUp);
		}else if(eventData.button == PointerEventData.InputButton.Right){
			this.map.eventListener.MouseEvent(this, MapDisplay.MouseEventType.RightClickUp);
		}
	}
}
