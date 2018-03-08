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

public class MapEditor :
	MonoBehaviour,
	MapDisplayEventListener
{
	public MapEditorUIRefs uiRefs;
	public MapDisplay mapDisplay;
	public BattleMap defaultMap;
	// [HideInInspector] public Game game;

	BattleMap map;
	LinkedList<BaseHistoryEvent> historyEvents;
	LinkedListNode<BaseHistoryEvent> lastHistoryEventNode;

	void Start(){
		// this.game = GameObject.Find("Game").GetComponent<Game>();

		this.map = this.defaultMap;
		this.historyEvents = new LinkedList<BaseHistoryEvent>();
		this.lastHistoryEventNode = null;

		this.mapDisplay.Init(this, this.map.size);

		this.RebuildMapDisplay();

		// UI stuff

		this.UpdateUI();

		Utility.AddInputFieldChangedListener(this.uiRefs.sizeXTextbox, this.InputFieldChanged);
		Utility.AddInputFieldChangedListener(this.uiRefs.sizeYTextbox, this.InputFieldChanged);

		Utility.AddButtonClickListener(this.uiRefs.undoButton, this.UndoRedoButtonPressed);
		Utility.AddButtonClickListener(this.uiRefs.redoButton, this.UndoRedoButtonPressed);
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

	public void MouseEvent(MapTile mapTile, MapDisplay.MouseEventType eventType){
		if(hardInput.GetKey("Pan Camera")){
			return;
		}

		if(eventType == MapDisplay.MouseEventType.Enter){
			this.mapDisplay.SetHoveredTile(mapTile);
		}else if(eventType == MapDisplay.MouseEventType.Exit){
			this.mapDisplay.DisableHoveredTile();
		}else if(eventType == MapDisplay.MouseEventType.Click){
			if(mapTile){
				this.mapDisplay.SetSelectedTile(mapTile);
			}else{
				this.mapDisplay.DisableSelectedTile();
			}
		}else if(eventType == MapDisplay.MouseEventType.RightClick){
			
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

	void UndoRedoButtonPressed(Button button){
		if(button == this.uiRefs.undoButton){
			this.UndoHistoryEvent(this.lastHistoryEventNode.Value);
			this.lastHistoryEventNode = this.lastHistoryEventNode.Previous;
		}else if(button == this.uiRefs.redoButton){
			if(this.lastHistoryEventNode != null){
				this.lastHistoryEventNode = this.lastHistoryEventNode.Next;
			}else{
				this.lastHistoryEventNode = this.historyEvents.First;
			}
			this.DoHistoryEvent(this.lastHistoryEventNode.Value);
		}

		this.UpdateUI();
	}
}
