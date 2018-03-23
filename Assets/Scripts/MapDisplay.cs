using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
		ClickDown,
		ClickUp,
		RightClickDown,
		RightClickUp,
	}

	public GameObject worldGO;
	public Camera gameCamera;
	public Transform cameraPivot;
	[HideInInspector] public Vector2i size;
	[HideInInspector] public Vector3 cameraDir;
	[HideInInspector] public MapDisplayEventListener eventListener;

	MapTile[] tiles;
	GameObject hoveredTileGO;
	GameObject origSelectedTileGO;
	List<GameObject> selectedTileGOs;
	GameObject targetTileGO;
	GameObject backgroundGO;

	public void Init(MapDisplayEventListener newListener, Vector2i newSize){
		this.eventListener = newListener;
		this.size = newSize;

		this.cameraDir = this.gameCamera.transform.rotation * Vector3.forward;

		{
			this.hoveredTileGO = new GameObject("Hovered tile");
			this.hoveredTileGO.SetActive(false);
			this.hoveredTileGO.AddComponent<MeshFilter> ().mesh = Resources.Load<Mesh>("Models/HexTile");
			MeshRenderer mr = this.hoveredTileGO.AddComponent<MeshRenderer>();
			mr.sharedMaterial = Resources.Load<Material>("Materials/Hovered tile");
		}

		{
			this.origSelectedTileGO = new GameObject("Selected tile");
			this.origSelectedTileGO.SetActive(false);
			this.origSelectedTileGO.transform.localScale = Vector3.one * 2f;
			this.origSelectedTileGO.AddComponent<MeshFilter> ().mesh = Resources.Load<Mesh>("Models/HexTile");
			MeshRenderer mr = this.origSelectedTileGO.AddComponent<MeshRenderer>();
			mr.sharedMaterial = Resources.Load<Material>("Materials/Selected tile");

			this.selectedTileGOs = new List<GameObject>();
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

			Action<EventTriggerType, UnityAction<BaseEventData>> addEventThing = (etType, callback) => {
				var entry = new EventTrigger.Entry();
				entry.eventID = etType;
				entry.callback.AddListener(callback);
				eventTrigger.triggers.Add(entry);
			};

			addEventThing(EventTriggerType.PointerClick, (data) => {
				var button = ((PointerEventData)data).button;
				if(button == PointerEventData.InputButton.Left){
					this.eventListener.MouseEvent(null, MouseEventType.Click);
				}else if(button == PointerEventData.InputButton.Right){
					this.eventListener.MouseEvent(null, MouseEventType.RightClick);
				}
			});
			addEventThing(EventTriggerType.PointerDown, (data) => {
				var button = ((PointerEventData)data).button;
				if(button == PointerEventData.InputButton.Left){
					this.eventListener.MouseEvent(null, MouseEventType.ClickDown);
				}else if(button == PointerEventData.InputButton.Right){
					this.eventListener.MouseEvent(null, MouseEventType.RightClickDown);
				}
			});
			addEventThing(EventTriggerType.PointerUp, (data) => {
				var button = ((PointerEventData)data).button;
				if(button == PointerEventData.InputButton.Left){
					this.eventListener.MouseEvent(null, MouseEventType.ClickUp);
				}else if(button == PointerEventData.InputButton.Right){
					this.eventListener.MouseEvent(null, MouseEventType.RightClickUp);
				}
			});
		}

		this.tiles = new MapTile[0];
		this.Recreate(newSize);
	}

	void Update(){
		if(hardInput.GetKey("Pan Camera")){
			float cameraSizeY = this.gameCamera.orthographicSize;
			float mouseRatioX = Input.GetAxis("Mouse X") / (float)Screen.height;
			float mouseRatioY = Input.GetAxis("Mouse Y") / (float)Screen.height;
			// I don't know why 4 and 8 work but they do ¯\_(ツ)_/¯
			Vector3 newPos = this.cameraPivot.transform.position + new Vector3(
				mouseRatioX * cameraSizeY * -4f,
				0f,
				mouseRatioY * cameraSizeY * -8f
			);
			this.cameraPivot.transform.position = newPos;
		}
	}

	public MapTile GetTile(Vector2i pos){
		return this.tiles[pos.x + pos.y * this.size.x];
	}

	public void Recreate(Vector2i newSize){
		this.DisableHoveredTile();
		this.DisableSelectedTiles();
		this.DisableTargetTile();

		foreach(MapTile tile in this.tiles){
			Destroy(tile.gameObject);
		}

		this.size = newSize;

		this.tiles = new MapTile[this.size.x * this.size.y];
		for (int y = 0; y < this.size.y; ++y) {
			for (int x = 0; x < this.size.x; ++x) {
				GameObject go = new GameObject("MapTile");
				go.transform.parent = this.transform;

				MapTile newTile = go.AddComponent<MapTile>();
				newTile.Init(this, new Vector2i(x, y));
				this.tiles[x + y * this.size.x] = newTile;
			}
		}
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
		this.hoveredTileGO.transform.parent = null;
	}

	// Selected tiles
	public void SetSelectedTile(MapTile tile){
		var tiles = new List<MapTile>();
		tiles.Add(tile);
		this.SetSelectedTiles(tiles);
	}
	public void SetSelectedTiles(List<MapTile> tiles){
		this.DisableSelectedTiles();

		if(tiles == null){
			return;
		}

		foreach(var tile in tiles){
			var clone = Instantiate(this.origSelectedTileGO) as GameObject;
			tile.AttachForeground(clone.transform, 1);
			clone.SetActive(true);
			this.selectedTileGOs.Add(clone);
		}
	}
	public void DisableSelectedTiles(){
		foreach(var go in this.selectedTileGOs){
			Destroy(go);
		}
		this.selectedTileGOs.Clear();
	}

	// Target tile
	public void SetTargetTile(MapTile tile){
		tile.Attach(this.targetTileGO.transform, 5);
		this.targetTileGO.SetActive(true);
	}
	public void DisableTargetTile(){
		this.targetTileGO.SetActive(false);
		this.targetTileGO.transform.parent = null;
	}
}
