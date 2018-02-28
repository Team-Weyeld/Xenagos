using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public interface MapDisplayEventListener{
	void MouseEvent(MapTile tile, MapDisplay.MouseEventType eventType);
}

public class MapDisplay : MonoBehaviour {
	public enum MouseEventType{
		Enter,
		Exit,
		Click,
		RightClick,
	}

	public Vector2i size;
	public Camera gameCamera;
	public Vector3 cameraDir;
	public MapDisplayEventListener eventListener;

	MapTile[] tiles;
	GameObject worldGO;
	GameObject hoveredTileGO;
	GameObject selectedTileGO;
	GameObject targetTileGO;
	GameObject backgroundGO;

	public void Init(MapDisplayEventListener newListener, GameObject newWorldGO, Vector2i newSize, Camera newGameCamera){
		this.eventListener = newListener;
		this.worldGO = newWorldGO;
		this.size = newSize;
		this.gameCamera = newGameCamera;

		this.cameraDir = this.gameCamera.transform.rotation * Vector3.forward;

		this.tiles = new MapTile[this.size.x * this.size.y];
		for (int y = 0; y < this.size.y; ++y) {
			for (int x = 0; x < this.size.x; ++x) {
				GameObject go = new GameObject ("MapTile");
				go.transform.parent = this.transform;

				MapTile newTile = go.AddComponent<MapTile>();
				newTile.Init(this, new Vector2i(x, y));
				this.tiles[x + y * this.size.x] = newTile;
			}
		}

		{
			this.hoveredTileGO = new GameObject("Hovered tile");
			this.hoveredTileGO.SetActive(false);
			this.hoveredTileGO.AddComponent<MeshFilter> ().mesh = Resources.Load<Mesh>("Models/HexTile");
			MeshRenderer mr = this.hoveredTileGO.AddComponent<MeshRenderer>();
			mr.sharedMaterial = Resources.Load<Material>("Materials/Hovered tile");
		}

		{
			this.selectedTileGO = new GameObject("Selected tile");
			this.selectedTileGO.SetActive(false);
			this.selectedTileGO.transform.localScale = Vector3.one * 2f;
			this.selectedTileGO.AddComponent<MeshFilter> ().mesh = Resources.Load<Mesh>("Models/HexTile");
			MeshRenderer mr = this.selectedTileGO.AddComponent<MeshRenderer>();
			mr.sharedMaterial = Resources.Load<Material>("Materials/Selected tile");
		}

		{
			this.targetTileGO = new GameObject("Target tile");
			this.targetTileGO.SetActive(false);
			this.targetTileGO.AddComponent<MeshFilter> ().mesh = Resources.Load<Mesh>("Models/HexTile");
			MeshRenderer mr = this.targetTileGO.AddComponent<MeshRenderer>();
			mr.sharedMaterial = Resources.Load<Material>("Materials/Target tile");
		}

		{
			this.backgroundGO = (GameObject)Instantiate(Resources.Load("Prefabs/Square collider"));
			this.backgroundGO.name = "Background plane";
			this.backgroundGO.transform.parent = this.worldGO.transform;
			float scale = 5f * Mathf.Max(this.size.x, this.size.y);
			this.backgroundGO.transform.localScale = Vector3.one * scale;
			Vector3 pos = new Vector3(this.size.x, -1f, this.size.y);
			pos *= 0.5f;
			this.backgroundGO.transform.localPosition = pos;

			EventTrigger eventTrigger = this.backgroundGO.AddComponent<EventTrigger>();

			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerClick;
			entry.callback.AddListener((data) => {
				var button = ((PointerEventData)data).button;
				if(button == PointerEventData.InputButton.Left){
					this.eventListener.MouseEvent(null, MouseEventType.Click);
				}else if(button == PointerEventData.InputButton.Right){
					this.eventListener.MouseEvent(null, MouseEventType.RightClick);
				}
			});
			eventTrigger.triggers.Add(entry);
		}
	}

	public MapTile GetTile(Vector2i pos){
		return this.tiles[pos.x + pos.y * this.size.x];
	}

	// Hovered tile
	public void SetHoveredTile(MapTile tile){
		tile.AttachForeground(this.hoveredTileGO.transform, 2);
		this.hoveredTileGO.SetActive(true);
	}
	public void SetHoveredTileValid(bool valid){
		var mr = this.hoveredTileGO.GetComponent<MeshRenderer>();
		if(valid){
			mr.sharedMaterial = Resources.Load<Material>("Materials/Hovered tile");
		}else{
			mr.sharedMaterial = Resources.Load<Material>("Materials/Hovered tile invalid");
		}
	}
	public void DisableHoveredTile(){
		this.hoveredTileGO.SetActive(false);
	}

	// Selected tile
	public void SetSelectedTile(MapTile tile){
		tile.AttachForeground(this.selectedTileGO.transform, 1);
		this.selectedTileGO.SetActive(true);
	}
	public void DisableSelectedTile(){
		this.selectedTileGO.SetActive(false);
	}

	// Target tile
	public void SetTargetTile(MapTile tile){
		tile.Attach(this.targetTileGO.transform, 5);
		this.targetTileGO.SetActive(true);
	}
	public void DisableTargetTile(){
		this.targetTileGO.SetActive(false);
	}

	public GameObject CreateSprite(Sprite sprite, Transform parent = null){
		float pitch = this.gameCamera.transform.rotation.eulerAngles.x;

		GameObject go = new GameObject("Unnamed battle sprite");
		go.transform.parent = parent;
		go.transform.localRotation = Quaternion.Euler(new Vector3(pitch, 0f, 0f));
		go.transform.localPosition = new Vector3(0f, 0f, MapTile.hexSpacingY * -0.5f);
		SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
		spriteRenderer.sharedMaterial = Resources.Load<Material>("Materials/Game sprite");
		spriteRenderer.sprite = sprite;

		return go;
	}
}
