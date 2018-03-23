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
	public enum Layer{
		Ground,
		Wall,
		GroundSprite,
		MainSprite,
		GhostSprite,
		// TargetSprite, HoverSprite, etc?
	}

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
	GameObject ghostSpriteGO;

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
		
	public void SetLayer(
		Layer layer,
		Material material = null,
		Sprite sprite = null,
		bool flipX = false,
		float alpha = 1.0f
	){
		this.RemoveLayer(layer);

		GameObject go = null;

		if(layer == Layer.Ground){
			go = new GameObject("Ground model");
			this.Attach(go.transform, 0);
			go.AddComponent<MeshFilter>().mesh = Resources.Load<Mesh>("Models/HexTile");
			go.AddComponent<MeshRenderer>().sharedMaterial = material;
			this.groundGO = go;
		}else if(layer == Layer.Wall){
			this.groundGO.transform.localPosition = Vector3.up * hexSideLength * 2f;

			go = new GameObject("Wall model");
			this.Attach(go.transform, 0);
			go.AddComponent<MeshFilter>().mesh = Resources.Load<Mesh>("Models/HexTileWall");
			go.AddComponent<MeshRenderer>().sharedMaterial = material;
			this.wallGO = go;
		}else if(layer == Layer.MainSprite){
			go = this.CreateSprite(sprite);
			this.Attach(go.transform, 5);
			go.transform.localPosition = new Vector3(0f, 0f, MapTile.hexSpacingY * -0.5f);
			go.name = "Tile sprite";
			this.spriteGO = go;
		}else if(layer == Layer.GhostSprite){
			go = this.CreateSprite(sprite);
			this.Attach(go.transform, 6);
			go.transform.localPosition = new Vector3(0f, 0f, MapTile.hexSpacingY * -0.5f);
			go.name = "Spooky ghost";
			this.ghostSpriteGO = go;
		}

		if(flipX){
			go.transform.localScale = new Vector3(-1, 1f, 1f);
		}

		if(alpha != 1.0f){
			Color color = go.GetComponent<SpriteRenderer>().color;
			color.a = alpha;
			go.GetComponent<SpriteRenderer>().color = color;
		}
	}

	public void RemoveLayer(Layer layer){
		if(layer == Layer.Ground){
			if(this.groundGO){
				Destroy(this.groundGO);
				this.groundGO = null;
			}
		}else if(layer == Layer.Wall){
			if(this.wallGO){
				Destroy(this.wallGO);
				this.wallGO = null;
			}
		}else if(layer == Layer.MainSprite){
			if(this.spriteGO){
				Destroy(this.spriteGO);
				this.spriteGO = null;
			}
		}else if(layer == Layer.GhostSprite){
			if(this.ghostSpriteGO){
				Destroy(this.ghostSpriteGO);
				this.ghostSpriteGO = null;
			}
		}
	}

	public void SetLayerVisible(Layer layer, bool visible){
		if(layer == Layer.Ground){
			this.groundGO.SetActive(visible);
		}else if(layer == Layer.Wall){
			this.wallGO.SetActive(visible);
		}else if(layer == Layer.MainSprite){
			this.spriteGO.SetActive(visible);
		}else if(layer == Layer.GhostSprite){
			this.ghostSpriteGO.SetActive(visible);
		}
	}

	// TODO: Make these private?
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

	public void SetLayersFromData(TileData data){
		this.SetLayer(Layer.Ground, material: data.groundMaterial);

		if(data.wallMaterial){
			this.SetLayer(Layer.Wall, material: data.wallMaterial);
		}

		if(data.sprite){
			this.SetLayer(Layer.MainSprite, sprite: data.sprite);
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


	public GameObject CreateSprite(Sprite sprite){
		float pitch = this.map.gameCamera.transform.rotation.eulerAngles.x;

		GameObject go = new GameObject("Unnamed battle sprite");
		go.transform.localRotation = Quaternion.Euler(new Vector3(pitch, 0f, 0f));
		go.transform.localPosition = new Vector3(0f, 0f, MapTile.hexSpacingY * -0.5f);
		SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
		spriteRenderer.sharedMaterial = Resources.Load<Material>("Materials/Game sprite");
		spriteRenderer.sprite = sprite;

		return go;
	}
}
