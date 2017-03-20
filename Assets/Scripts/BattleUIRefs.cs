using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIRefs : MonoBehaviour{
	public Canvas canvas;
	[Header("Unit list panel")]
	public Button finishTurnButton;
	[Header("Tile info panel")]
	public GameObject tileInfoBorder;
	[Space]
	public GameObject mechTab;
	public Button mechTabButton;
	public Image mechTabButtonGraphic;
	public Text mechTabName;
	public Text mechTabHP;
	[Space]
	public GameObject pilotTab;
	public Button pilotTabButton;
	public Image pilotTabButtonGraphic;
	[Space]
	public GameObject tileTab;
	public Button tileTabButton;
	public Image tileTabButtonGraphic;
	public Text tileTabName;
	public Text tileTabDescription;

	[Header("Actions panel")]
	public GameObject actionsPanel;
	public GameObject actionsGreyoutPanel;
	public Button moveButton;
	public Button setTargetButton;
	public Toggle fireAutoToggle;
	public Button fireNowButton;
	public Text apText;
}
