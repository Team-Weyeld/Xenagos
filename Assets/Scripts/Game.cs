using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour{
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

		BattleHistory battleHistory = new BattleHistory();
		battleHistory.startData = this.testBattleStartData;
		battleHistory.currentTeamIndex = 1;
		battleHistory.moves = new List<object>();
		Game.StartBattle(battleHistory);
	}

	void Update(){
		if(hardInput.GetKeyDown("Test loading")){
			Debug.Log("Testing loading");

			SceneManager.LoadScene("Test load menu");
		}
	}

	void FixedUpdate(){
		++this.fixedFrameCount;
	}

	void OnDrawGizmos(){
		DebugDrawers.OnDrawGizmos();
	}

	public static void StartBattle(BattleHistory battleHistory){
		IEnumerator coroutine = instance.StartBattleInternal(battleHistory);
		instance.StartCoroutine(coroutine);
	}

	IEnumerator StartBattleInternal(BattleHistory battleHistory){
		AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("Battle scene");

		yield return new WaitUntil(() => asyncOperation.isDone);

		GameObject go = GameObject.Find("Battle");
		Battle battle = go.GetComponent<Battle>();

		battle.Init(this, battleHistory);
	}
}
