﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: rename to "Tile/SpriteDirection"?
public enum MechDirection{
	Left,
	Right,
}

[System.Serializable]
// Previously BattleStartData
public struct Scenario{
	[System.Serializable]
	public struct Mech{
		public string mechName;
		// ?
		public MechDirection direction;
	}

	[System.Serializable]
	public struct Team{
		public Mech[] mechs;
		public Color color;
		public bool isPlayer;
	}

	public Team[] teams;
	public int startingTeamIndex;
	public string map;
}

[System.Serializable]
public struct BattleMap{
	[System.Serializable]
	public struct TileOverride{
		public Vector2i pos;
		public string name;
	}

	[System.Serializable]
	public struct Entity{
		public Vector2i pos;
		public string name;
	}

	public Vector2i size;
	public string baseTileName;
	public List<TileOverride> tileOverrides;
	public List<Entity> entities;
}

[System.Serializable]
public struct MechData{
	public string name;
	[MechSprite]
	public Sprite sprite;
	public float maxHP;
	public float damage;
	public float movementCost;
	// TODO: This will end up being firing speed and may involve weapon reloads
//	public float attackCost;
}

[System.Serializable]
public struct TileData{
	public string name;
	[TextArea]
	public string description;
	public Material groundMaterial;
	public Material wallMaterial;
	public Sprite sprite;
	public float losAmount;
	public bool allowsMovement;
	public float movementSpeedMult;
}
