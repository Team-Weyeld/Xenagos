using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void KeyCallback(string name);

[System.Serializable]
public struct KeyEntry{
	public string key;
	public string name;
	public KeyCallback callback;
}

public class DebugKeys : MonoBehaviour{
	public KeyEntry[] keys;

	static DebugKeys instance;

	public static void Subscribe(string name, KeyCallback callback){
		for(int n = 0; n < instance.keys.Length; ++n){
			if(instance.keys[n].name == name){
				instance.keys[n].callback = callback;
			}
		}
	}

	void Awake(){
		DebugKeys.instance = this;
	}

	void Update(){
		foreach(KeyEntry key in this.keys){
			if(key.callback == null){
				continue;
			}

			if(Input.GetKeyDown(key.key)){
				Debug.Log("Debug key '" + key.name + "' pressed");
				key.callback(key.name);
			}
		}
	}
}
