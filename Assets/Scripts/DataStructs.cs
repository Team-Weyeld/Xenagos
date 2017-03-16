using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MechDirection{
	Left,
	Right,
}

[System.Serializable]
public struct BattleStartData{
	[System.Serializable]
	public struct Mech{
		public string mechName;
		public Vector2i pos;
		public MechDirection direction;
	}

	[System.Serializable]
	public struct Team{
		public Mech[] mechs;
		public Color color;
		public bool isPlayer;
	}

	[System.Serializable]
	public struct TileOverride{
		public int posX;
		public int posY;
		public string name;
	}

	public Vector2i mapSize;
	public Team[] teams;
	public string baseTileName;
	public TileOverride[] tileOverrides;
}

public static class BattleMove{
	[System.Serializable]
	public struct Move{
		public int mechIndex;
		public int newIndex;
		public bool isFiring;
		public int targetMechIndex;
	}

	[System.Serializable]
	public struct StandingFire{
		public int mechIndex;
		public int targetMechIndex;
	}

	[System.Serializable]
	public struct SetTarget{
		public int mechIndex;
		public bool hasTarget;
		public int targetMechIndex;
	}
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
