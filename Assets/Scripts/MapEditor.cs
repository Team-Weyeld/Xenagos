using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

abstract class BaseHistoryEvent{
	
}

class ResizeHistoryEvent : BaseHistoryEvent{
	public Vector2i oldSize;
	public Vector2i newSize;
	public List<BattleMap.TileOverride> removedTiles;
}

enum MapEditorState{
	Normal,
	SelectionPaint,
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
		this.mapDisplay.Recreate(this.map.size);

		TileData baseTileData = GameData.GetTile(this.map.baseTileName);
		TileData tileData = baseTileData;
		for (int y = 0; y < this.map.size.y; ++y) {
			for (int x = 0; x < this.map.size.x; ++x) {
				tileData = baseTileData;
				foreach (var o in this.map.tileOverrides) {
					if(o.posX == x && o.posY == y){
						tileData = GameData.GetTile(o.name);
						break;
					}
				}

				MapTile mapTile = this.mapDisplay.GetTile(new Vector2i(x, y));
				mapTile.SetData(tileData);
			}
		}
	}

	List<BattleMap.TileOverride> ResizeMap(Vector2i newSize){
		var removedTiles = new List<BattleMap.TileOverride>();

		this.map.size = newSize;

		for(int n = this.map.tileOverrides.Count; n --> 0;){
			bool isOutsideBounds = (
				this.map.tileOverrides[n].posX >= newSize.x ||
				this.map.tileOverrides[n].posY >= newSize.y
			);
			if(isOutsideBounds){
				removedTiles.Add(this.map.tileOverrides[n]);
				this.map.tileOverrides.RemoveAt(n);
			}
		}

		return removedTiles;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////
	// State-dependent functions

	void SetState(MapEditorState newState){
		if(this.state == MapEditorState.Normal){
			this.mapDisplay.DisableHoveredTile();
		}

		this.state = newState;

		if(newState == MapEditorState.SelectionPaint){
			if(
				this.lastEnteredTile != null &&
				this.selectedTiles.Contains(this.lastEnteredTile) == false
			){
				this.selectedTiles.Add(this.lastEnteredTile);
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
				}else{
					this.mapDisplay.DisableHoveredTile();
				}
			}else if(this.state == MapEditorState.SelectionPaint){
				if(tile && this.selectedTiles.Contains(tile) == false){
					this.selectedTiles.Add(tile);
					this.mapDisplay.SetSelectedTiles(this.selectedTiles);
				}
			}
		}else if(eventType == MapDisplay.MouseEventType.Exit){
			this.lastEnteredTile = null;
		}else if(eventType == MapDisplay.MouseEventType.Click){

		}else if(eventType == MapDisplay.MouseEventType.ClickDown){
			if(this.state == MapEditorState.Normal){
				this.SetState(MapEditorState.SelectionPaint);
			}
		}else if(eventType == MapDisplay.MouseEventType.ClickUp){
			if(this.state == MapEditorState.SelectionPaint){
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

		this.lastHistoryEventNode = this.historyEvents.AddLast(baseHistoryEvent);

		this.DoHistoryEvent(baseHistoryEvent);

		this.UpdateUI();
	}

	void DoHistoryEvent(BaseHistoryEvent baseHistoryEvent){
		if(baseHistoryEvent.GetType() == typeof(ResizeHistoryEvent)){
			var resizeHistoryEvent = (ResizeHistoryEvent)baseHistoryEvent;
			resizeHistoryEvent.removedTiles = this.ResizeMap(resizeHistoryEvent.newSize);
			this.RebuildMapDisplay();
		}
	}

	void UndoHistoryEvent(BaseHistoryEvent baseHistoryEvent){
		if(baseHistoryEvent.GetType() == typeof(ResizeHistoryEvent)){
			var resizeHistoryEvent = (ResizeHistoryEvent)baseHistoryEvent;
			this.ResizeMap(resizeHistoryEvent.oldSize);
			foreach(var tileOverride in resizeHistoryEvent.removedTiles){
				this.map.tileOverrides.Add(tileOverride);
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


		this.uiRefs.selectedTilesPanel.SetActive(
			this.selectedTiles.Count > 0 &&
			this.state != MapEditorState.Normal
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
			this.selectedTiles.Clear();
			this.mapDisplay.SetSelectedTiles(this.selectedTiles);
		}
	}
}
