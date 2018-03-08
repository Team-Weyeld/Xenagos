using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapEditorUIRefs : MonoBehaviour{
	public Canvas canvas;
	[Space]
	public GameObject corePanelMask;
	public InputField sizeXTextbox;
	public InputField sizeYTextbox;
	public Button undoButton;
	public Button redoButton;
	public Button selectNoneButton;
	[Space]
	public GameObject selectedTilesPanel;
	public InputField selectedTilesTextbox;
}