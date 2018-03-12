using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public struct Vector2i{
	public int x;
	public int y;

	public Vector2i(int a, int b){
		x = a;
		y = b;
	}

	public static bool operator ==(Vector2i a, Vector2i b){
		return a.x == b.x && a.y == b.y;
	}

	public static bool operator !=(Vector2i a, Vector2i b){
		return a.x != b.x && a.y != b.y;
	}

	public override bool Equals(object obj){
	    return base.Equals(obj);
	}

	public override int GetHashCode(){
	    return base.GetHashCode();
	}
}

static class Utility{
	// Unity's UI events don't pass the object in the callback function, so this is a workaround
	// using delegates.
	public delegate void ButtonCallbackFunction(Button button);
	public static void AddButtonClickListener(Button button, ButtonCallbackFunction callback){
		button.onClick.AddListener(() => {
			callback(button);
		});
	}

	public delegate void ToggleCallbackFunction(Toggle toggle);
	public static void AddToggleListener(Toggle toggle, ToggleCallbackFunction callback){
		toggle.onValueChanged.AddListener((bool state) => {
			callback(toggle);
		});
	}

	public delegate void ButtonHoverChangeCallbackFunction(Button button, bool isEntering);
	public static void AddButtonHoverListener(Button button, ButtonHoverChangeCallbackFunction callback){
		EventTrigger eventTrigger = button.gameObject.GetComponent<EventTrigger>();
		if(eventTrigger == null){
			eventTrigger = button.gameObject.AddComponent<EventTrigger>();
		}

		EventTrigger.Entry entry = new EventTrigger.Entry();
		entry.eventID = EventTriggerType.PointerEnter;
		entry.callback.AddListener((data) => {
			if(button.interactable){
				callback(button, true);
			}
		});
		eventTrigger.triggers.Add(entry);

		entry = new EventTrigger.Entry();
		entry.eventID = EventTriggerType.PointerExit;
		entry.callback.AddListener((data) => {
			if(button.interactable){
				callback(button, false);
			}
		});
		eventTrigger.triggers.Add(entry);
	}

	public delegate void InputFieldCallbackFunction(InputField inputField);
	public static void AddInputFieldChangedListener(InputField inputField, InputFieldCallbackFunction callback){
		inputField.onEndEdit.AddListener((ifield) => {
			callback(inputField);
		});
	}
}