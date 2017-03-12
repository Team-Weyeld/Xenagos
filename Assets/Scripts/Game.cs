using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour {
	public Battle battle;
	public BattleStartData testBattleStartData;

	static Game instance;
	long fixedFrameCount;

	public static double time{
		get{
			return (double)instance.fixedFrameCount * (double)Time.fixedDeltaTime;
		}
	}

	void Awake(){
		Game.instance = this;
		this.fixedFrameCount = 0;
	}

	void Start(){
		Object.DontDestroyOnLoad(this.gameObject);

		this.battle.Init(this, this.testBattleStartData);
	}

	void FixedUpdate(){
		++this.fixedFrameCount;
	}

	void OnDrawGizmos(){
		DebugDrawers.OnDrawGizmos();
	}
}
