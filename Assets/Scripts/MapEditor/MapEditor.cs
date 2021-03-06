﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

abstract class BaseHistoryEvent{
	public int index;
}
class ResizeHistoryEvent : BaseHistoryEvent{
	public Vector2i oldSize;
	public Vector2i newSize;
	public List<BattleMap.TileOverride> removedTiles;
	public List<BattleMap.Entity> removedEntities;
	public List<Vector2i> deselectedTiles;
}
class SelectionChangeHistoryEvent : BaseHistoryEvent{
	public List<Vector2i> oldSelectedTiles;
	public List<Vector2i> newSelectedTiles;
}
class TileChangeHistoryEvent : BaseHistoryEvent{
	// Names can be empty strings
	public List<BattleMap.TileOverride> oldTiles;
	public List<BattleMap.TileOverride> newTiles;
}
class EntityChangeHistoryEvent : BaseHistoryEvent{
	public List<BattleMap.Entity> oldEntities;
	public List<BattleMap.Entity> newEntities;
}

enum MapEditorState{
	Normal,
	SelectionPaint,
	SelectionErase,
}

enum MapEditorEntityType{
	Invalid,
	None,
	PlayerMechSpawn,
	EnemyMechSpawn,
}

public class MapEditor :
	MonoBehaviour,
	MapDisplayEventListener
{
	public MapEditorUIRefs uiRefs;
	public MapDisplay mapDisplay;
	public BattleMap defaultMap;
	// [HideInInspector] public Game game;

	MapEditorState state;
	BattleMap map;
	List<MapTile> selectedTiles;
	MapTile lastEnteredTile;
	LinkedList<BaseHistoryEvent> historyEvents;
	LinkedListNode<BaseHistoryEvent> lastHistoryEventNode;
	BaseHistoryEvent inProgressHistoryEvent;

	void Start(){
		// this.game = GameObject.Find("Game").GetComponent<Game>();

		this.state = MapEditorState.Normal;
		this.map = this.defaultMap;
		this.selectedTiles = new List<MapTile>();
		this.historyEvents = new LinkedList<BaseHistoryEvent>();
		this.lastHistoryEventNode = null;

		this.mapDisplay.Init(this, this.map.size);

		this.RebuildMapDisplay();

		// UI stuff

		this.uiRefs.selectedEntityDropdown.ClearOptions();
		foreach(MapEditorEntityType e in Enum.GetValues(typeof(MapEditorEntityType))){
			string text = e.ToString();
			if(e == MapEditorEntityType.Invalid){
				text = " ";
			}
			this.uiRefs.selectedEntityDropdown.options.Add(new Dropdown.OptionData(text));
		}

		this.UpdateUI();

		Utility.AddInputFieldChangedListener(this.uiRefs.mapNameTextbox, this.InputFieldChanged);
		Utility.AddInputFieldChangedListener(this.uiRefs.sizeXTextbox, this.InputFieldChanged);
		Utility.AddInputFieldChangedListener(this.uiRefs.sizeYTextbox, this.InputFieldChanged);
		Utility.AddInputFieldChangedListener(this.uiRefs.selectedTilesTextbox, this.InputFieldChanged);

		Utility.AddButtonClickListener(this.uiRefs.saveButton, this.ButtonPressed);
		Utility.AddButtonClickListener(this.uiRefs.loadButton, this.ButtonPressed);
		Utility.AddButtonClickListener(this.uiRefs.undoButton, this.ButtonPressed);
		Utility.AddButtonClickListener(this.uiRefs.redoButton, this.ButtonPressed);
		Utility.AddButtonClickListener(this.uiRefs.selectNoneButton, this.ButtonPressed);

		Utility.AddDropdownChangedListener(this.uiRefs.selectedEntityDropdown, this.DropdownChanged);
	}

	void RebuildMapDisplay(){
		var selectedTilesSerialized = new List<Vector2i>(this.selectedTiles.Select(x => x.pos));

		this.mapDisplay.Recreate(this.map.size);

		// Tiles
		TileData baseTileData = GameData.GetTile(this.map.baseTileName);
		for (int y = 0; y < this.map.size.y; ++y) {
			for (int x = 0; x < this.map.size.x; ++x) {
				var pos = new Vector2i(x, y);

				MapTile mapTile = this.mapDisplay.GetTile(pos);
				mapTile.SetLayersFromData(baseTileData);
			}
		}
		foreach(var o in this.map.tileOverrides){
			TileData tileData = GameData.GetTile(o.name);
			MapTile mapTile = this.mapDisplay.GetTile(o.pos);
			mapTile.SetLayersFromData(tileData);
		}

		// Entities
		foreach(var e in this.map.entities){
			Color color = Color.white;
			if(e.name == "PlayerMechSpawn") color = Color.HSVToRGB(0.33f, 0.75f, 1.0f);
			if(e.name == "EnemyMechSpawn") color = Color.HSVToRGB(0.0f, 0.75f, 1.0f);

			MapTile mapTile = this.mapDisplay.GetTile(e.pos);
			Sprite sprite = Resources.Load<Sprite>("Textures/Entity");
			mapTile.SetLayer(MapTile.Layer.GhostSprite, sprite: sprite, color: color);
		}

		// Selected tiles
		this.selectedTiles = new List<MapTile>();
		foreach(Vector2i pos in selectedTilesSerialized){
			bool isInBounds = pos.x < this.map.size.x && pos.y < this.map.size.y;
			if(isInBounds){
				this.selectedTiles.Add(this.mapDisplay.GetTile(pos));
			}
		}
		this.mapDisplay.SetSelectedTiles(this.selectedTiles);
	}

	////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////
	// State-dependent functions

	void SetState(MapEditorState newState){
		if(this.state == MapEditorState.Normal){
			this.mapDisplay.DisableHoveredTile();
		}else if(this.state == MapEditorState.SelectionPaint || this.state == MapEditorState.SelectionErase){
			var he = (SelectionChangeHistoryEvent)this.inProgressHistoryEvent;
			he.newSelectedTiles = new List<Vector2i>(this.selectedTiles.Select(x => x.pos));
			if(he.oldSelectedTiles.SequenceEqual(he.newSelectedTiles) == false){
				this.AddNewHistoryEvent(he);
			}

			this.inProgressHistoryEvent = null;
		}

		this.state = newState;

		if(newState == MapEditorState.SelectionPaint){
			var he = new SelectionChangeHistoryEvent();
			he.oldSelectedTiles = new List<Vector2i>(this.selectedTiles.Select(x => x.pos));
			this.inProgressHistoryEvent = he;

			if(
				this.lastEnteredTile != null &&
				this.selectedTiles.Contains(this.lastEnteredTile) == false
			){
				this.selectedTiles.Add(this.lastEnteredTile);
				this.mapDisplay.SetSelectedTiles(this.selectedTiles);
			}
		}else if(newState == MapEditorState.SelectionErase){
			var he = new SelectionChangeHistoryEvent();
			he.oldSelectedTiles = new List<Vector2i>(this.selectedTiles.Select(x => x.pos));
			this.inProgressHistoryEvent = he;

			if(
				this.lastEnteredTile != null &&
				this.selectedTiles.Contains(this.lastEnteredTile) == true
			){
				this.selectedTiles.Remove(this.lastEnteredTile);
				this.mapDisplay.SetSelectedTiles(this.selectedTiles);
			}
		}

		this.UpdateUI();
	}

	public void MouseEvent(MapTile tile, MapDisplay.MouseEventType eventType){
		if(hardInput.GetKey("Pan Camera")){
			return;
		}

		if(eventType == MapDisplay.MouseEventType.Enter){
			this.lastEnteredTile = tile;

			if(this.state == MapEditorState.Normal){
				if(tile){
					this.mapDisplay.SetHoveredTile(tile);
				}
			}else if(this.state == MapEditorState.SelectionPaint){
				if(tile && this.selectedTiles.Contains(tile) == false){
					this.selectedTiles.Add(tile);
					this.mapDisplay.SetSelectedTiles(this.selectedTiles);
				}
			}else if(this.state == MapEditorState.SelectionErase){
				if(tile && this.selectedTiles.Contains(tile) == true){
					this.selectedTiles.Remove(tile);
					this.mapDisplay.SetSelectedTiles(this.selectedTiles);
				}
			}
		}else if(eventType == MapDisplay.MouseEventType.Exit){
			this.lastEnteredTile = null;

			if(this.state == MapEditorState.Normal){
				this.mapDisplay.DisableHoveredTile();
			}
		}else if(eventType == MapDisplay.MouseEventType.Click){

		}else if(eventType == MapDisplay.MouseEventType.ClickDown){
			if(this.state == MapEditorState.Normal){
				this.SetState(MapEditorState.SelectionPaint);
			}
		}else if(eventType == MapDisplay.MouseEventType.ClickUp){
			if(this.state == MapEditorState.SelectionPaint){
				this.SetState(MapEditorState.Normal);
			}
		}else if(eventType == MapDisplay.MouseEventType.RightClickDown){
			if(this.state == MapEditorState.Normal){
				this.SetState(MapEditorState.SelectionErase);
			}
		}else if(eventType == MapDisplay.MouseEventType.RightClickUp){
			if(this.state == MapEditorState.SelectionErase){
				this.SetState(MapEditorState.Normal);
			}
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////
	// HistoryEvent functions

	void AddNewHistoryEvent(BaseHistoryEvent baseHistoryEvent){
		while(this.historyEvents.Last != this.lastHistoryEventNode){
			this.historyEvents.RemoveLast();
		}

		baseHistoryEvent.index = this.historyEvents.Count;

		this.lastHistoryEventNode = this.historyEvents.AddLast(baseHistoryEvent);

		this.DoHistoryEvent(baseHistoryEvent);

		this.UpdateUI();
	}

	void DoHistoryEvent(BaseHistoryEvent baseHistoryEvent){
		if(baseHistoryEvent.GetType() == typeof(ResizeHistoryEvent)){
			var he = (ResizeHistoryEvent)baseHistoryEvent;
			this.map.size = he.newSize;

			he.removedTiles = new List<BattleMap.TileOverride>();
			for(int n = this.map.tileOverrides.Count; n --> 0;){
				bool isOutsideBounds = (
					this.map.tileOverrides[n].pos.x >= he.newSize.x ||
					this.map.tileOverrides[n].pos.y >= he.newSize.y
				);
				if(isOutsideBounds){
					he.removedTiles.Add(this.map.tileOverrides[n]);
					this.map.tileOverrides.RemoveAt(n);
				}
			}

			he.removedEntities = new List<BattleMap.Entity>();
			for(int n = this.map.entities.Count; n --> 0;){
				bool isOutsideBounds = (
					this.map.entities[n].pos.x >= he.newSize.x ||
					this.map.entities[n].pos.y >= he.newSize.y
				);
				if(isOutsideBounds){
					he.removedEntities.Add(this.map.entities[n]);
					this.map.entities.RemoveAt(n);
				}
			}

			he.deselectedTiles = new List<Vector2i>();
			for(int n = this.selectedTiles.Count; n --> 0;){
				var tile = this.selectedTiles[n];
				bool isOutsideBounds = (
					tile.pos.x >= he.newSize.x ||
					tile.pos.y >= he.newSize.y
				);
				if(isOutsideBounds){
					he.deselectedTiles.Add(tile.pos);
					this.selectedTiles.RemoveAt(n);
				}
			}

			this.RebuildMapDisplay();
		}else if(baseHistoryEvent.GetType() == typeof(SelectionChangeHistoryEvent)){
			var he = (SelectionChangeHistoryEvent)baseHistoryEvent;
			this.selectedTiles = new List<MapTile>(he.newSelectedTiles.Select(x => this.mapDisplay.GetTile(x)));
			this.mapDisplay.SetSelectedTiles(this.selectedTiles);
		}else if(baseHistoryEvent.GetType() == typeof(TileChangeHistoryEvent)){
			var he = (TileChangeHistoryEvent)baseHistoryEvent;

			foreach(var tileOverride in he.newTiles){
				this.map.tileOverrides.RemoveAll(x => x.pos == tileOverride.pos);
				if(tileOverride.name != ""){
					this.map.tileOverrides.Add(tileOverride);
				}
			}

			this.RebuildMapDisplay();
		}else if(baseHistoryEvent.GetType() == typeof(EntityChangeHistoryEvent)){
			var he = (EntityChangeHistoryEvent)baseHistoryEvent;

			foreach(var entity in he.newEntities){
				this.map.entities.RemoveAll(x => x.pos == entity.pos);
				if(entity.name != "None"){
					this.map.entities.Add(entity);
				}
			}

			this.RebuildMapDisplay();
		}
	}

	void UndoHistoryEvent(BaseHistoryEvent baseHistoryEvent){
		if(baseHistoryEvent.GetType() == typeof(ResizeHistoryEvent)){
			var he = (ResizeHistoryEvent)baseHistoryEvent;
			this.map.size = he.oldSize;

			foreach(var tileOverride in he.removedTiles){
				this.map.tileOverrides.Add(tileOverride);
			}

			foreach(var entity in he.removedEntities){
				this.map.entities.Add(entity);
			}

			this.RebuildMapDisplay();

			foreach(var pos in he.deselectedTiles){
				this.selectedTiles.Add(this.mapDisplay.GetTile(pos));
			}
			this.mapDisplay.SetSelectedTiles(this.selectedTiles);
		}else if(baseHistoryEvent.GetType() == typeof(SelectionChangeHistoryEvent)){
			var he = (SelectionChangeHistoryEvent)baseHistoryEvent;
			this.selectedTiles = new List<MapTile>(he.oldSelectedTiles.Select(x => this.mapDisplay.GetTile(x)));
			this.mapDisplay.SetSelectedTiles(this.selectedTiles);
		}else if(baseHistoryEvent.GetType() == typeof(TileChangeHistoryEvent)){
			var he = (TileChangeHistoryEvent)baseHistoryEvent;

			foreach(var tileOverride in he.oldTiles){
				this.map.tileOverrides.RemoveAll(x => x.pos == tileOverride.pos);
				if(tileOverride.name != ""){
					this.map.tileOverrides.Add(tileOverride);
				}
			}

			this.RebuildMapDisplay();
		}else if(baseHistoryEvent.GetType() == typeof(EntityChangeHistoryEvent)){
			var he = (EntityChangeHistoryEvent)baseHistoryEvent;

			foreach(var entity in he.oldEntities){
				this.map.entities.RemoveAll(x => x.pos == entity.pos);
				if(entity.name != "None"){
					this.map.entities.Add(entity);
				}
			}

			this.RebuildMapDisplay();
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////
	// UI functions

	void UpdateUI(){
		this.uiRefs.sizeXTextbox.text = this.map.size.x.ToString();
		this.uiRefs.sizeYTextbox.text = this.map.size.y.ToString();

		this.uiRefs.undoButton.interactable = (
			this.lastHistoryEventNode != null
		);
		this.uiRefs.redoButton.interactable = (
			this.historyEvents.Count > 0 &&
			this.historyEvents.Last != this.lastHistoryEventNode
		);

		this.uiRefs.corePanelMask.SetActive(this.state != MapEditorState.Normal);


		if(
			this.selectedTiles.Count > 0 &&
			this.state == MapEditorState.Normal
		){
			this.uiRefs.selectedTilesPanel.SetActive(true);

			// Tile names textbox
			{
				bool hasCommonName = false;
				string commonName = null;
				foreach(var selectedTile in this.selectedTiles){
					string tileName = "";
					{
						int index = this.map.tileOverrides.FindIndex(x => x.pos == selectedTile.pos);
						if(index != -1){
							tileName = this.map.tileOverrides[index].name;
						}
					}
					if(commonName == null){
						hasCommonName = true;
						commonName = tileName;
					}else if(commonName != tileName){
						hasCommonName = false;

						break;
					}
				}

				if(hasCommonName){
					this.uiRefs.selectedTilesTextbox.text = commonName;
					if(commonName == ""){
						(this.uiRefs.selectedTilesTextbox.placeholder as Text).text = "[none]";
					}
				}else{
					this.uiRefs.selectedTilesTextbox.text = "";
					(this.uiRefs.selectedTilesTextbox.placeholder as Text).text = "[various]";
				}
			}

			// Entity name dropdown
			{
				bool hasCommonName = false;
				string commonName = null;
				foreach(var selectedTile in this.selectedTiles){
					string entityName = "";
					{
						int index = this.map.entities.FindIndex(x => x.pos == selectedTile.pos);
						if(index != -1){
							entityName = this.map.entities[index].name;
						}
					}
					if(commonName == null){
						hasCommonName = true;
						commonName = entityName;
					}else if(commonName != entityName){
						hasCommonName = false;

						break;
					}
				}

				this.uiRefs.selectedEntityDropdown.value = (int)MapEditorEntityType.Invalid;
				if(hasCommonName){
					MapEditorEntityType commonEntityType = MapEditorEntityType.None;
					if(commonName != ""){
						commonEntityType = (MapEditorEntityType)Enum.Parse(typeof(MapEditorEntityType), commonName);
					}
					this.uiRefs.selectedEntityDropdown.captionText.text = commonEntityType.ToString();
				}else{
					this.uiRefs.selectedEntityDropdown.captionText.text = "[various]";
				}
			}
		}else{
			this.uiRefs.selectedTilesPanel.SetActive(false);
		}
	}

	void InputFieldChanged(InputField inputField){
		if(inputField == this.uiRefs.sizeXTextbox || inputField == this.uiRefs.sizeYTextbox){
			int newValue = int.Parse(inputField.text);

			ResizeHistoryEvent historyEvent = new ResizeHistoryEvent();
			historyEvent.oldSize = this.map.size;
			historyEvent.newSize = this.map.size;
			if(inputField == this.uiRefs.sizeXTextbox){
				historyEvent.newSize.x = newValue;
			}else{
				historyEvent.newSize.y = newValue;
			}

			this.AddNewHistoryEvent(historyEvent);
		}else if(inputField == this.uiRefs.selectedTilesTextbox){
			var he = new TileChangeHistoryEvent();

			he.oldTiles = new List<BattleMap.TileOverride>();
			foreach(MapTile mapTile in this.selectedTiles){
				int index = this.map.tileOverrides.FindIndex(x => x.pos == mapTile.pos);
				if(index != -1){
					he.oldTiles.Add(this.map.tileOverrides[index]);
				}else{
					var tileOverride = new BattleMap.TileOverride();
					tileOverride.pos = mapTile.pos;
					tileOverride.name = "";
					he.oldTiles.Add(tileOverride);
				}
			}

			he.newTiles = new List<BattleMap.TileOverride>();
			foreach(MapTile mapTile in this.selectedTiles){
				var tileOverride = new BattleMap.TileOverride();
				tileOverride.pos = mapTile.pos;
				tileOverride.name = inputField.text;
				he.newTiles.Add(tileOverride);
			}

			this.AddNewHistoryEvent(he);
		}
	}

	void ButtonPressed(Button button){
		if(button == this.uiRefs.undoButton){
			this.UndoHistoryEvent(this.lastHistoryEventNode.Value);
			this.lastHistoryEventNode = this.lastHistoryEventNode.Previous;

			this.UpdateUI();
		}else if(button == this.uiRefs.redoButton){
			if(this.lastHistoryEventNode != null){
				this.lastHistoryEventNode = this.lastHistoryEventNode.Next;
			}else{
				this.lastHistoryEventNode = this.historyEvents.First;
			}
			this.DoHistoryEvent(this.lastHistoryEventNode.Value);

			this.UpdateUI();
		}else if(button == this.uiRefs.selectNoneButton){
			var historyEvent = new SelectionChangeHistoryEvent();
			historyEvent.oldSelectedTiles = new List<Vector2i>(this.selectedTiles.Select(x => x.pos));
			historyEvent.newSelectedTiles = new List<Vector2i>();
			this.AddNewHistoryEvent(historyEvent);
		}else if(button == this.uiRefs.saveButton){
			string jsonText = JsonUtility.ToJson(this.map, true);
			var sw = new StreamWriter("Maps/" + this.uiRefs.mapNameTextbox.text + ".json");
			sw.Write(jsonText);
			sw.Close();
		}else if(button == this.uiRefs.loadButton){
			StreamReader sr = new StreamReader("Maps/" + this.uiRefs.mapNameTextbox.text + ".json");
			string jsonText = sr.ReadToEnd();
			sr.Close();

			this.map = JsonUtility.FromJson<BattleMap>(jsonText);

			this.selectedTiles = new List<MapTile>();
			this.historyEvents = new LinkedList<BaseHistoryEvent>();
			this.lastHistoryEventNode = null;

			this.RebuildMapDisplay();
			this.UpdateUI();
		}
	}

	void DropdownChanged(Dropdown dropdown){
		if(dropdown == this.uiRefs.selectedEntityDropdown){
			if(dropdown.value == (int)MapEditorEntityType.Invalid){
				return;
			}

			var newEntityType = (MapEditorEntityType)dropdown.value;

			var he = new EntityChangeHistoryEvent();

			he.oldEntities = new List<BattleMap.Entity>();
			foreach(var tile in this.selectedTiles){
				int index = this.map.entities.FindIndex(x => x.pos == tile.pos);
				if(index != -1){
					he.oldEntities.Add(this.map.entities[index]);
				}else{
					var newEntity = new BattleMap.Entity();
					newEntity.pos = tile.pos;
					newEntity.name = "None";
					he.oldEntities.Add(newEntity);
				}
			}

			he.newEntities = new List<BattleMap.Entity>();
			foreach(var tile in this.selectedTiles){
				var newEntity = new BattleMap.Entity();
				newEntity.pos = tile.pos;
				newEntity.name = newEntityType.ToString();
				he.newEntities.Add(newEntity);
			}

			this.AddNewHistoryEvent(he);
		}
	}
}
