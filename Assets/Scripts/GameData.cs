using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour {
	private static GameData instance;

	public MechData[] mechs;
	public TileData[] tiles;

	public static MechData GetMech(string name){
		foreach(MechData m in GameData.instance.mechs){
			if(m.name == name){
				return m;
			}
		}

		MechData invalid = new MechData();
		invalid.name = "Invalid";
		return invalid;
	}

	public static TileData GetTile(string name){
		foreach(TileData t in GameData.instance.tiles){
			if(t.name == name){
				return t;
			}
		}

		TileData invalid = new TileData();
		invalid.name = "Invalid";
		return invalid;
	}

	void Awake () {
		GameData.instance = this;
	}
}
