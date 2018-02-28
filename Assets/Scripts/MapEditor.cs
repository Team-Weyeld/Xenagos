using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class MapEditor :
	MonoBehaviour,
	MapDisplayEventListener
{
	public MapDisplay mapDisplay;
	public BattleMap defaultMap;
	[HideInInspector] public Game game;

	BattleMap map;

	void Start(){
		this.game = GameObject.Find("Game").GetComponent<Game>();

		this.map = this.defaultMap;

		this.mapDisplay.Init(this, map.size);

		// Build default map.

		TileData baseTileData = GameData.GetTile(map.baseTileName);
		TileData tileData = baseTileData;
		for (int y = 0; y < map.size.y; ++y) {
			for (int x = 0; x < map.size.x; ++x) {
				tileData = baseTileData;
				foreach (var o in map.tileOverrides) {
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
}
