﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
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
	public List<Vector2i> deselectedTiles;
}
class SelectionChangeHistoryEvent : BaseHistoryEvent{
	public List<Vector2i> oldSelectedTiles;
	public List<Vector2i> newSelectedTiles;
}

enum MapEditorState{
	Normal,
	SelectionPaint,
	SelectionErase,
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

		this.UpdateUI();

		Utility.AddInputFieldChangedListener(this.uiRefs.sizeXTextbox, this.InputFieldChanged);
		Utility.AddInputFieldChangedListener(this.uiRefs.sizeYTextbox, this.InputFieldChanged);

		Utility.AddButtonClickListener(this.uiRefs.undoButton, this.ButtonPressed);
		Utility.AddButtonClickListener(this.uiRefs.redoButton, this.ButtonPressed);
		Utility.AddButtonClickListener(this.uiRefs.selectNoneButton, this.ButtonPressed);
	}

	void RebuildMapDisplay(){
		var selectedTilesSerialized = new List<Vector2i>(this.selectedTiles.Select(x => x.pos));

		this.mapDisplay.Recreate(this.map.size);

		TileData baseTileData = GameData.GetTile(this.map.baseTileName);
		TileData tileData = baseTileData;
		for (int y = 0; y < this.map.size.y; ++y) {
			for (int x = 0; x < this.map.size.x; ++x) {
				var pos = new Vector2i(x, y);
				tileData = baseTileData;
				foreach (var o in this.map.tileOverrides) {
					if(o.pos == pos){
						tileData = GameData.GetTile(o.name);
						break;
					}
				}

				MapTile mapTile = this.mapDisplay.GetTile(pos);
				mapTile.SetData(tileData);
			}
		}

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
		}
	}

	void UndoHistoryEvent(BaseHistoryEvent baseHistoryEvent){
		if(baseHistoryEvent.GetType() == typeof(ResizeHistoryEvent)){
			var he = (ResizeHistoryEvent)baseHistoryEvent;
			this.map.size = he.oldSize;

			foreach(var tileOverride in he.removedTiles){
				this.map.tileOverrides.Add(tileOverride);
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


		this.uiRefs.selectedTilesPanel.SetActive(
			this.selectedTiles.Count > 0 &&
			this.state == MapEditorState.Normal
		);
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

			AddNewHistoryEvent(historyEvent);
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
			AddNewHistoryEvent(historyEvent);
		}
	}
}
