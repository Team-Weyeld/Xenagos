using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadMenu : MonoBehaviour{
	public InputField inputFieldRef;

	void Start(){
		this.inputFieldRef.onEndEdit.AddListener(this.OnInputFieldSubmit);
	}

	void OnInputFieldSubmit(string inputText){
		if(File.Exists(inputText)){
			string jsonText = "";

			using(StreamReader sr = new StreamReader(inputText)){
				jsonText = sr.ReadToEnd();
			}

			BattleHistory battleHistory = new BattleHistory();
			battleHistory.FromJSON(jsonText);

			Game.StartBattle(battleHistory);
		}
	}
}
