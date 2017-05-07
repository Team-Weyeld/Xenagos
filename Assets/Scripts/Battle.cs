using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Assertions;
using UnityEngine.UI;

public enum BattleState{
	None,
	SelectingAction,
	MoveAction,
	SetTargetAction,
}

[System.Serializable]
struct MoveActionData{
	public HexTile fromTile;
	public PathingResult? pathingResult;
	public List<GameObject> ghostMechs;
	public GameObject losLinesGO;
	public bool moved;
}

[System.Serializable]
public class BattleTeam{
	public List<BattleMech> mechs;
	public bool isPlayer;
	// Updated with UpdateFogOfWar
	public bool[] visibleTiles;
}

public class Battle : MonoBehaviour{
	public float hexSpacingX = 1f;
	public BattleUIRefs uiRefs;
	public GameObject worldGO;
	public GameObject world2DGO;
	public Camera gameCamera;
	public Transform cameraPivot;
	public bool visualFogOfWar = true;
	public bool fogOfWar = true;
	[HideInInspector] public Game game;
	[HideInInspector] public GameObject mapGO;
	[HideInInspector] public float hexSpacingY;
	[HideInInspector] public float hexRadius;
	[HideInInspector] public float hexSideLength;
	[HideInInspector] public Vector2i mapSize;
	[HideInInspector] public PathNetwork pathNetwork;

	BattleHistory history;
	BattleState state = BattleState.None;
	HexTile hoveredTile;
	HexTile selectedTile;
	GameObject hoveredTileGO;
	GameObject selectedTileGO;
	GameObject targetTileGO;
	GameObject backgroundGO;
	HexTile[] tiles;
	List<BattleTeam> teams;
	int currentTeamIndex;
	BattleTeam currentTeam;
	// TODO: This is dumb, though it will eventually be a meter instead of text. Make an ActionPointMeter Monobehaviour?
	Color apTextOriginalColor;

	MoveActionData moveActionData;

	public void Init(Game game, BattleHistory battleHistory){
		this.game = game;

		this.hexRadius = 1f / Mathf.Cos (2f * Mathf.PI / 12f) * this.hexSpacingX * 0.5f;
		this.hexSideLength = Mathf.Sin (2f * Mathf.PI / 12f) * this.hexRadius * 2f;
		this.hexSpacingY = this.hexRadius + this.hexSideLength * 0.5f;

		this.mapGO = new GameObject ("Battle map");
		this.mapGO.transform.parent = this.worldGO.transform;

		// Copy the history, except for the moves; they'll be applied later.
		this.history = battleHistory;
		List<object> moveHistory = battleHistory.moves;
		this.history.moves = new List<object>();

		Scenario scenario = this.history.scenario;
		BattleMap map = this.history.startingMap;

		// Build misc objects.

		{
			this.hoveredTileGO = new GameObject("Hovered tile");
			this.hoveredTileGO.SetActive(false);
			this.hoveredTileGO.AddComponent<MeshFilter> ().mesh = Resources.Load<Mesh>("Models/HexTile");
			MeshRenderer mr = this.hoveredTileGO.AddComponent<MeshRenderer>();
			mr.sharedMaterial = Resources.Load<Material>("Materials/Hovered tile");
		}

		{
			this.selectedTileGO = new GameObject("Selected tile");
			this.selectedTileGO.SetActive(false);
			this.selectedTileGO.transform.localScale = Vector3.one * 2f;
			this.selectedTileGO.AddComponent<MeshFilter> ().mesh = Resources.Load<Mesh>("Models/HexTile");
			MeshRenderer mr = this.selectedTileGO.AddComponent<MeshRenderer>();
			mr.sharedMaterial = Resources.Load<Material>("Materials/Selected tile");
		}

		{
			this.targetTileGO = new GameObject("Target tile");
			this.targetTileGO.SetActive(false);
			this.targetTileGO.AddComponent<MeshFilter> ().mesh = Resources.Load<Mesh>("Models/HexTile");
			MeshRenderer mr = this.targetTileGO.AddComponent<MeshRenderer>();
			mr.sharedMaterial = Resources.Load<Material>("Materials/Target tile");
		}

		{
			this.backgroundGO = (GameObject)Instantiate(Resources.Load("Prefabs/Square collider"));
			this.backgroundGO.name = "Background plane";
			this.backgroundGO.transform.parent = this.worldGO.transform;
			float scale = 5f * Mathf.Max(map.size.x, map.size.y);
			this.backgroundGO.transform.localScale = Vector3.one * scale;
			Vector3 pos = new Vector3(map.size.x, -1f, map.size.y);
			pos *= 0.5f;
			this.backgroundGO.transform.localPosition = pos;

			EventTrigger eventTrigger = this.backgroundGO.AddComponent<EventTrigger>();

			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerClick;
			entry.callback.AddListener((data) => {
				var button = ((PointerEventData)data).button;
				if(button == PointerEventData.InputButton.Left){
					this.MouseEvent(null, MouseEventType.Click);
				}else if(button == PointerEventData.InputButton.Right){
					this.MouseEvent(null, MouseEventType.RightClick);
				}
			});
			eventTrigger.triggers.Add(entry);
		}

		{
			// Let us know when anything on the UI is clicked (and not handled by a button or something).

			EventTrigger eventTrigger = this.uiRefs.canvas.gameObject.AddComponent<EventTrigger>();

			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerClick;
			entry.callback.AddListener((data) => {
				var button = ((PointerEventData)data).button;
				if(button == PointerEventData.InputButton.Left){
					this.MouseEvent(null, MouseEventType.Click);
				}else if(button == PointerEventData.InputButton.Right){
					this.MouseEvent(null, MouseEventType.RightClick);
				}
			});
			eventTrigger.triggers.Add(entry);
		}

		// Build map.

		this.mapSize = map.size;
		this.tiles = new HexTile[this.mapSize.x * this.mapSize.y];
		TileData baseTileData = GameData.GetTile(map.baseTileName);
		TileData tileData = baseTileData;
		this.pathNetwork = new PathNetwork();
		for (int y = 0; y < map.size.y; ++y) {
			for (int x = 0; x < map.size.x; ++x) {
				GameObject go = new GameObject ("Hex tile");
				go.transform.parent = this.mapGO.transform;

				tileData = baseTileData;
				foreach (var o in map.tileOverrides) {
					if(o.posX == x && o.posY == y){
						tileData = GameData.GetTile(o.name);
						break;
					}
				}

				HexTile newTile = go.AddComponent<HexTile>();
				newTile.Init(this, x, y, tileData);
				this.tiles[x + y * this.mapSize.x] = newTile;
			}
		}

		foreach(HexTile tile in this.tiles){
			for(int n = 0; n < 6; ++n){
				HexTile neighbor = tile.GetNeighbor(n);
				if(neighbor){
					this.pathNetwork.ConnectNodes(tile, neighbor);
				}
			}

			if(tile.data.allowsMovement == false){
				this.pathNetwork.SetNodeEnabled(tile, false);
			}
		}

		// Create teams and place mechs.

		this.teams = new List<BattleTeam>();

		foreach(Scenario.Team teamData in scenario.teams){
			BattleTeam team = new BattleTeam();
			this.teams.Add(team);
			team.mechs = new List<BattleMech>();
			team.isPlayer = teamData.isPlayer;
			team.visibleTiles = new bool[this.tiles.Length];

			foreach(Scenario.Mech m in teamData.mechs){
				MechData mechData = GameData.GetMech(m.mechName);

				HexTile tile = this.GetTile(m.pos.x, m.pos.y);

				GameObject mechGO = new GameObject(string.Concat("Mech: ", mechData.name));
				tile.Attach(mechGO.transform, 10);
				BattleMech mech = mechGO.AddComponent<BattleMech>();
				mech.Init(this, mechData);
				mech.SetDirection(m.direction);
				Assert.IsTrue(tile.mech == null);
				tile.mech = mech;
				mech.tile = tile;

				this.pathNetwork.SetNodeEnabled(tile, false);

				team.mechs.Add(mech);
				mech.team = team;
			}
		}
			
		this.currentTeamIndex = this.history.scenario.startingTeamIndex;
		this.currentTeam = this.teams[this.currentTeamIndex];

		// UI stuff.

		this.uiRefs.tileInfoBorder.SetActive(false);
		this.uiRefs.mechTab.SetActive(false);
		this.uiRefs.pilotTab.SetActive(false);
		this.uiRefs.tileTab.SetActive(false);
		this.uiRefs.actionsPanel.SetActive(false);

		Utility.AddButtonClickListener(this.uiRefs.finishTurnButton, this.UnitListButtonPressed);

		Utility.AddButtonClickListener(this.uiRefs.mechTabButton, this.TileInfoTabButtonClicked);
		Utility.AddButtonClickListener(this.uiRefs.pilotTabButton, this.TileInfoTabButtonClicked);
		Utility.AddButtonClickListener(this.uiRefs.tileTabButton, this.TileInfoTabButtonClicked);

		Utility.AddButtonClickListener(this.uiRefs.moveButton, this.ActionButtonPressed);
		Utility.AddButtonClickListener(this.uiRefs.setTargetButton, this.ActionButtonPressed);
		Utility.AddButtonClickListener(this.uiRefs.fireNowButton, this.ActionButtonPressed);

		Utility.AddToggleListener(this.uiRefs.fireAutoToggle, this.ActionToggleChanged);

		Utility.AddButtonHoverListener(this.uiRefs.fireNowButton, this.ActionButtonHoverChanged);

		this.BringTileTabButtonToFront(this.uiRefs.mechTabButton);
		this.UpdateRightPanel();

		this.apTextOriginalColor = this.uiRefs.apText.color;

		// Execute any moves to start with.

		foreach(object o in moveHistory){
			this.ExecuteMove(o);
		}

		this.currentTeamIndex = battleHistory.currentTeamIndex;
		this.currentTeam = this.teams[this.currentTeamIndex];

		// Start first state.

		this.UpdateFogOfWar();

		this.SetState(BattleState.SelectingAction);
	}

	void Update(){
		if(hardInput.GetKey("Pan Camera")){
			float cameraSizeY = this.gameCamera.orthographicSize;
			float mouseRatioX = Input.GetAxis("Mouse X") / (float)Screen.height;
			float mouseRatioY = Input.GetAxis("Mouse Y") / (float)Screen.height;
			// I don't know why 4 and 8 work but they do ¯\_(ツ)_/¯
			Vector3 newPos = this.cameraPivot.transform.position + new Vector3(
				mouseRatioX * cameraSizeY * -4f,
				0f,
				mouseRatioY * cameraSizeY * -8f
			);
			this.cameraPivot.transform.position = newPos;
		}

		if(hardInput.GetKeyDown("Test LOS")){
			// Draw a LOS test line from the selected tile to all other tiles.

			Debug.Log("Testing LOS");

			if(this.selectedTile == null){
				return;
			}

			foreach(HexTile tile in this.tiles){
				bool canSee = this.TestLOS(this.selectedTile, tile);

				DebugDrawers.SpawnLine(
					this.selectedTile.transform.position,
					tile.transform.position,
					canSee ? Color.green : Color.red,
					2f
				);
			}
		}else if(hardInput.GetKeyDown("Test pathing")){
			Debug.Log("Testing pathing");

			if(this.selectedTile == null || this.hoveredTile == null){
				return;
			}

			PathingResult result = this.pathNetwork.FindPath(
				this.selectedTile,
				this.hoveredTile,
				true
			);

			if(result.isValid){
				Debug.Log(result.nodes.Count + " nodes in path " + result.distance + " units long.");
			}else{
				Debug.Log("Invalid path!");
			}
		}else if(hardInput.GetKeyDown("Test saving")){
			Debug.Log("Testing saving");

			string jsonText = this.history.ToJSON();
			string path = "TestSave.json";

			using(StreamWriter sw = new StreamWriter(path)){
				sw.WriteLine(jsonText);
			}

			Debug.Log("Wrote save to " + path);
		}
	}

	public HexTile GetTile(int index){
		return this.tiles[index];
	}

	public HexTile GetTile(int x, int y){
		return this.tiles[x + y * this.mapSize.x];
	}

	public HexTile GetTile(Vector2i pos){
		return this.tiles[pos.x + pos.y * this.mapSize.x];
	}

	void UpdateFogOfWar(){
		int tileCount = this.tiles.Length;

		foreach(BattleTeam team in this.teams){
			for(int n = 0; n < tileCount; ++n){
				team.visibleTiles[n] = true;
			}

			foreach(BattleMech mech in team.mechs){
				for(int n = 0; n < tileCount; ++n){
					if(
						this.tiles[n].data.allowsMovement == true &&
						this.TestLOS(mech.tile, this.tiles[n]) == false &&
						this.fogOfWar == true
					){
						team.visibleTiles[n] = false;
					}
				}
			}
		}

		// TODO: Temporarily control both teams until AI can move on its own
		foreach(HexTile tile in this.tiles){
			if(tile.data.allowsMovement == true){
				tile.SetRevealed(this.CanTeamSeeTile(this.currentTeam, tile) || this.visualFogOfWar == false);
			}
		}

		// Clear targets of hidden mechs

		foreach(BattleTeam team in this.teams){
			foreach(BattleMech mech in team.mechs){
				if(mech.target && this.CanTeamSeeTile(team, mech.target.tile) == false){
					mech.target = null;
				}
			}
		}
	}

	// Note: this will be used for replays and maybe save files, so only use data from the move data
	// and the current state of the map.
	void ExecuteMove(object o){
		if(o.GetType() == typeof(BattleMove.Move)){
			var move = (BattleMove.Move)o;

			BattleMech mech = this.GetTile(move.mechIndex).mech;
			BattleMech targetMech = move.isFiring ? this.GetTile(move.targetMechIndex).mech : null;
			HexTile fromTile = mech.tile;
			HexTile toTile = this.GetTile(move.newIndex);

			this.pathNetwork.SetNodeEnabled(fromTile, true);

			PathingResult result = this.pathNetwork.FindPath(fromTile, toTile);
			Assert.IsTrue(result.isValid);

			bool isFiring = move.isFiring;
			HexTile prevTile = (HexTile)result.nodes[0];
			for(int n = 1; n < result.nodes.Count; ++n){
				HexTile currentTile = (HexTile)result.nodes[n];

				prevTile.mech = null;
				currentTile.mech = mech;
				mech.tile = currentTile;

				if(isFiring){
					bool canSeeTarget = this.TestLOS(currentTile, targetMech.tile);
					if(canSeeTarget){
						this.MechAttack(mech, targetMech);
						if(targetMech.isDestroyed){
							isFiring = false;
						}
					}
				}

				prevTile = currentTile;
			}

			toTile.Attach(mech.transform, 10);

			HexTile lastTile1 = (HexTile)result.nodes[result.nodes.Count - 2];
			HexTile lastTile2 = (HexTile)result.nodes[result.nodes.Count - 1];
			bool right = lastTile2.transform.position.x > lastTile1.transform.position.x;
			MechDirection newDir = right ? MechDirection.Right : MechDirection.Left;
			mech.SetDirection(newDir);

			var apCostResult = mech.GetAPCostForMove(result.nodes);
			mech.actionPoints -= apCostResult.ap;

			this.pathNetwork.SetNodeEnabled(toTile, false);

			this.UpdateFogOfWar();
		}else if(o.GetType() == typeof(BattleMove.StandingFire)){
			var move = (BattleMove.StandingFire)o;

			BattleMech mech = this.GetTile(move.mechIndex).mech;
			BattleMech targetMech = this.GetTile(move.targetMechIndex).mech;

			this.MechAttack(mech, targetMech);

			var apCostResult = mech.GetAPCostForStandingFire();
			mech.actionPoints -= apCostResult.ap;
		}else if(o.GetType() == typeof(BattleMove.SetTarget)){
			var move = (BattleMove.SetTarget)o;

			BattleMech mech = this.GetTile(move.mechIndex).mech;

			if(move.hasTarget){
				mech.target = this.GetTile(move.targetMechIndex).mech;
			}else{
				mech.target = null;
			}
		}else{
			throw new UnityException();
		}

		// Add to battle history.
		this.history.moves.Add(o);

		// Determine if this team's turn is over so we can advance the turn.
		bool hasAP = false;
		foreach(BattleMech mech in this.currentTeam.mechs){
			if(mech.actionPoints > 0){
				hasAP = true;
				break;
			}
		}
		if(hasAP == false){
			this.AdvanceTurn();
		}
	}

	void AdvanceTurn(){
		// Switch current team.
		this.currentTeamIndex = (this.currentTeamIndex + 1) % this.teams.Count;
		this.currentTeam = this.teams[this.currentTeamIndex];
		// TODO: "history" is turning out to be "save file" or something
		this.history.currentTeamIndex = this.currentTeamIndex;

		// Refill new team's action points.
		foreach(BattleMech mech in this.currentTeam.mechs){
			mech.actionPoints += mech.maxActionPoints;
			if(mech.actionPoints > mech.maxActionPoints){
				mech.actionPoints = mech.maxActionPoints;
			}
		}

		// Temporary stuff because no AI
		this.UpdateFogOfWar();
	}

	void MechAttack(BattleMech mech, BattleMech target){
		float damage = mech.data.damage;

		target.hp -= damage;

		Debug.Log(mech.data.name + " attacked " + target.data.name + " for " + damage + " damage.");

		if(target.hp <= 0f){
			this.DestroyMech(target);
		}
	}

	void DestroyMech(BattleMech mechToDestroy){
		Debug.Log(mechToDestroy.name + " destroyed!");

		HexTile tile = mechToDestroy.tile;

		// Remove the mech and all references to it (such as targets).

		tile.mech = null;

		mechToDestroy.team.mechs.Remove(mechToDestroy);
		mechToDestroy.isDestroyed = true;

		foreach(BattleTeam team in this.teams){
			foreach(BattleMech mech in team.mechs){
				if(mech.target == mechToDestroy){
					mech.target = null;
				}
			}
		}

		Destroy(mechToDestroy.gameObject);

		// Change the map

		TileData tileData = GameData.GetTile("Destroyed mech");
		tileData.groundMaterial = tile.data.groundMaterial;
		tile.Recreate(tileData);

		this.UpdateFogOfWar();
	}

	public bool TestLOS(HexTile tile1, HexTile tile2){
		Vector2 from = tile1.Get2DPosition();
		Vector2 to = tile2.Get2DPosition();
		Vector2 dir = (to - from).normalized;
		RaycastHit2D result = Physics2D.Raycast(
			from,
			dir,
			Vector2.Distance(from, to),
			LayerMask.GetMask("Map 2D")
		);

		bool canSee = result.collider == null;

		return canSee;
	}

	public bool CanTeamSeeTile(BattleTeam team, HexTile tile){
		return team.visibleTiles[tile.pos.x + tile.pos.y * this.mapSize.x] == true || this.fogOfWar == false;
	}

	public GameObject CreateSprite(Sprite sprite, Transform parent = null){
		float pitch = this.gameCamera.transform.rotation.eulerAngles.x;

		GameObject go = new GameObject("Unnamed battle sprite");
		go.transform.parent = parent;
		go.transform.localRotation = Quaternion.Euler(new Vector3(pitch, 0f, 0f));
		go.transform.localPosition = new Vector3(0f, 0f, this.hexSpacingY * -0.5f);
		SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
		spriteRenderer.sharedMaterial = Resources.Load<Material>("Materials/Game sprite");
		spriteRenderer.sprite = sprite;

		return go;
	}

	// TODO: umm
	void UpdateTargetTile(BattleMech mech){
		if(mech.team == this.currentTeam && mech.target && this.CanTeamSeeTile(mech.team, mech.target.tile)){
			mech.target.tile.Attach(this.targetTileGO.transform, 5);
			this.targetTileGO.SetActive(true);
		}else{
			this.targetTileGO.SetActive(false);
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////
	// State-dependent behaviour

	public void SetState(BattleState newState){
		if(this.state != BattleState.None){
			Debug.Log("Ending state " + this.state);
		}

		if(this.state == BattleState.SelectingAction){
		}else if(this.state == BattleState.MoveAction){
			if(this.moveActionData.ghostMechs != null){
				foreach(GameObject go in this.moveActionData.ghostMechs){
					Destroy(go);
				}
			}
			this.moveActionData.ghostMechs = null;

			if(this.moveActionData.losLinesGO){
				Destroy(this.moveActionData.losLinesGO);
			}

			if(this.moveActionData.moved == false){
				this.pathNetwork.SetNodeEnabled(this.moveActionData.fromTile, false);
			}

			this.ResetActionPointsPreview();
		}else if(this.state == BattleState.SetTargetAction){

		}

		this.state = newState;

		Debug.Log("Beginning state " + this.state);

		if(this.state == BattleState.SelectingAction){
			this.HexTileHovered();

			this.SetMenusUsable(true);
		}else if(this.state == BattleState.MoveAction){
			this.moveActionData = new MoveActionData();
			this.moveActionData.fromTile = this.selectedTile;
			this.moveActionData.pathingResult = null;
			this.moveActionData.ghostMechs = new List<GameObject>();
			this.moveActionData.moved = false;

			this.pathNetwork.SetNodeEnabled(this.moveActionData.fromTile, true);

			this.SetMenusUsable(false);
		}else if(this.state == BattleState.SetTargetAction){
			this.SetMenusUsable(false);
		}else{
			throw new UnityException();
		}
	}

	void HexTileHovered(){
		if(this.state == BattleState.MoveAction){
			foreach(GameObject go in this.moveActionData.ghostMechs){
				Destroy(go);
			}
			this.moveActionData.ghostMechs.Clear();

			if(this.moveActionData.losLinesGO){
				Destroy(this.moveActionData.losLinesGO);
			}
			this.moveActionData.losLinesGO = new GameObject("LOS lines");
			this.moveActionData.losLinesGO.transform.parent = this.worldGO.transform;

			this.ResetActionPointsPreview();

			this.moveActionData.pathingResult = this.pathNetwork.FindPath(
				this.selectedTile,
				this.hoveredTile
			);

			if(this.CanClickTile() == false){
				goto end;
			}

			PathingResult result = this.moveActionData.pathingResult.Value;

			if(result.isValid == false){
				goto end;
			}

			// Create ghost mechs

			this.moveActionData.ghostMechs.Capacity = result.nodes.Count - 1;

			GameObject prevGO = this.selectedTile.gameObject;
			GameObject currentGO;
			for(int n = 1; n < result.nodes.Count; ++n){
				HexTile tile = (HexTile)result.nodes[n];

				currentGO = tile.gameObject;

				bool right = currentGO.transform.position.x > prevGO.transform.position.x;
				MechDirection dir = right ? MechDirection.Right : MechDirection.Left;
				GameObject ghostGO = this.selectedTile.mech.CreateGhost(dir);
				this.moveActionData.ghostMechs.Add(ghostGO);
				tile.Attach(ghostGO.transform, 20);

				prevGO = currentGO;
			}

			// Create LOS lines from each tile to the target

			BattleMech target = this.selectedTile.mech.target;
			if(target && this.selectedTile.mech.fireAuto){
				for(int n = 1; n < result.nodes.Count; ++n){
					HexTile tile = (HexTile)result.nodes[n];

					GameObject go = (GameObject)Instantiate(Resources.Load("Prefabs/LOS line"));
					go.transform.parent = this.moveActionData.losLinesGO.transform;

					float offsetOutward = 0.33f;
					Vector3 dir = (target.tile.transform.position - tile.transform.position).normalized;
					Vector3 pos1 = tile.transform.position + dir * offsetOutward;
					Vector3 pos2 = target.tile.transform.position - dir * offsetOutward;

					float height = 0.25f;
					pos1.y += height;
					pos2.y += height;

					LineRenderer lr = go.GetComponent<LineRenderer>();
					lr.SetPositions(new Vector3[]{pos1, pos2});

					bool canSee = this.TestLOS(tile, target.tile);
					Color lineColor = canSee ? Color.green : Color.red;
					lr.startColor = lineColor;
					lr.endColor = lineColor;
				}
			}

			// Update action point UI element.

			float apRequired = this.selectedTile.mech.GetAPCostForMove(result.nodes).ap;
			this.UpdateActionPointsPreview(this.selectedTile.mech.actionPoints - apRequired);
		}

		end:

		var mr = this.hoveredTileGO.GetComponent<MeshRenderer>();
		if(this.CanClickTile()){
			mr.sharedMaterial = Resources.Load<Material>("Materials/Hovered tile");
		}else{
			mr.sharedMaterial = Resources.Load<Material>("Materials/Hovered tile invalid");
		}
	}

	// TODO: State dependent behaviour for when a tile can't be clicked
	bool CanClickTile(){
		HexTile tile = this.hoveredTile;

		if(this.state == BattleState.SelectingAction){
			return true;
		}else if(this.state == BattleState.MoveAction){
			if(
				tile == null ||
				tile == this.selectedTile ||
				tile.mech != null ||
				tile.data.allowsMovement == false ||
				this.CanTeamSeeTile(this.selectedTile.mech.team, tile) == false
			){
				return false;
			}

			if(this.moveActionData.pathingResult == null){
				this.moveActionData.pathingResult = this.pathNetwork.FindPath(
					this.selectedTile,
					tile
				);
			}
			if(this.moveActionData.pathingResult.Value.isValid == false){
				return false;
			}

			BattleMech mech = this.selectedTile.mech;
			var apCostResult = mech.GetAPCostForMove(this.moveActionData.pathingResult.Value.nodes);
			if(apCostResult.isValid == false){
				return false;
			}

			return true;
		}else if(this.state == BattleState.SetTargetAction){
			return (
				tile != null &&
				tile.mech != null &&
				tile != this.selectedTile &&
				this.CanTeamSeeTile(this.selectedTile.mech.team, tile)
			);
		}else{
			throw new UnityException();
		}
	}

	// NOTE: can be null
	void HexTileClicked(){
		HexTile clickedTile = this.hoveredTile;

		if(this.state == BattleState.SelectingAction){
			if(clickedTile != this.selectedTile){
				if(this.selectedTile){
					// Unselected this tile.

					this.selectedTileGO.SetActive(false);
					this.targetTileGO.SetActive(false);
				}
				this.selectedTile = clickedTile;
				if(this.selectedTile){
					// Selected this tile.

					this.selectedTile.AttachForeground(this.selectedTileGO.transform, 1);
					this.selectedTileGO.SetActive(true);

					BattleMech mech = this.selectedTile.mech;

					if(mech){
						this.UpdateTargetTile(mech);
					}
				}else{
					// Just for a consistent look when no tile is selected.
					this.BringTileTabButtonToFront(this.uiRefs.mechTabButton);
				}

				this.AdjustTileInfoTabButtonGraphics();
				this.UpdateRightPanel();
			}
		}else if(this.state == BattleState.MoveAction){
			if(clickedTile){
				BattleMech mech = this.selectedTile.mech;

				var move = new BattleMove.Move();
				move.mechIndex = mech.tile.index;
				move.newIndex = clickedTile.index;
				move.isFiring = mech.target != null && mech.fireAuto;
				if(move.isFiring){
					move.targetMechIndex = mech.target.tile.index;
				}
				this.ExecuteMove(move);

				this.moveActionData.moved = true;

				// Make this the new selected tile.
				// Ugh, refactor this copied code into... something
				this.selectedTile = clickedTile;
				this.selectedTile.AttachForeground(this.selectedTileGO.transform, 1);

				this.UpdateTargetTile(this.selectedTile.mech);
				this.UpdateRightPanel();

				this.SetState(BattleState.SelectingAction);
			}else{
				this.SetState(BattleState.SelectingAction);
			}
		}else if(this.state == BattleState.SetTargetAction){
			BattleMech mech = this.selectedTile.mech;

			var move = new BattleMove.SetTarget();
			move.mechIndex = mech.tile.index;
			move.hasTarget = clickedTile != null;
			if(move.hasTarget){
				move.targetMechIndex = clickedTile.index;
			}
			this.ExecuteMove(move);

			// TODO: Keep mech's dir pointed towards its target, if it has one
			this.UpdateTargetTile(mech);

			this.UpdateRightPanel();

			this.SetState(BattleState.SelectingAction);
		}
	}

	// NOTE: can be null
	void HexTileRightClicked(){
//		HexTile clickedTile = this.hoveredTile;

		if(this.state == BattleState.SelectingAction){
			// Attack selected mech maybe?
		}else if(this.state == BattleState.MoveAction){
			this.SetState(BattleState.SelectingAction);
		}else if(this.state == BattleState.SetTargetAction){
			this.SetState(BattleState.SelectingAction);
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////
	// Event-like functions

	public enum MouseEventType{
		Enter,
		Exit,
		Click,
		RightClick,
	}
	public void MouseEvent(HexTile tile, MouseEventType eventType){
		if(hardInput.GetKey("Pan Camera")){
			return;
		}

		HexTile newHoveredTile = this.hoveredTile;

		if(eventType == MouseEventType.Enter){
			newHoveredTile = tile;
		}else if(eventType == MouseEventType.Exit){
			newHoveredTile = null;
		}else if(eventType == MouseEventType.Click){
			if(this.CanClickTile()){
				this.HexTileClicked();
			}
		}else if(eventType == MouseEventType.RightClick){
			this.HexTileRightClicked();
		}

		if(newHoveredTile != this.hoveredTile){
			if(this.hoveredTile){
				// Stopped hovering this tile.

				this.hoveredTileGO.SetActive(false);
			}
			this.hoveredTile = newHoveredTile;
			if(this.hoveredTile){
				// Started hovering this tile.

				this.hoveredTile.AttachForeground(this.hoveredTileGO.transform, 2);
				this.hoveredTileGO.SetActive(true);
			}

			this.HexTileHovered();
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////
	// UI functions

	void SetMenusUsable(bool usable){
		this.uiRefs.finishTurnButton.interactable = usable;

		this.uiRefs.actionsGreyoutPanel.SetActive(!usable);
	}

	void BringTileTabButtonToFront(Button button){
		this.uiRefs.tileTabButton.gameObject.transform.SetAsLastSibling();
		this.uiRefs.pilotTabButton.gameObject.transform.SetAsLastSibling();

		button.gameObject.transform.SetAsLastSibling();
	}

	void AdjustTileInfoTabButtonGraphics(){
		Color normal = new Color(1f, 1f, 1f, 1f);
		Color transparent = new Color(1f, 1f, 1f, 0.33f);

		if(this.selectedTile){
			this.uiRefs.tileTabButtonGraphic.color = normal;
		}else{
			this.uiRefs.tileTabButtonGraphic.color = transparent;
		}

		if(this.selectedTile && this.selectedTile.mech && this.CanTeamSeeTile(this.currentTeam, this.selectedTile)){
			this.uiRefs.mechTabButtonGraphic.color = normal;
			this.uiRefs.pilotTabButtonGraphic.color = normal;
		}else{
			this.uiRefs.mechTabButtonGraphic.color = transparent;
			this.uiRefs.pilotTabButtonGraphic.color = transparent;
		}
	}

	void UpdateRightPanel(){
		this.AdjustTileInfoTabButtonGraphics();

		bool hasTile = this.selectedTile != null;
		bool hasMech = hasTile && this.selectedTile.mech != null;
		hasMech = hasMech && this.CanTeamSeeTile(this.currentTeam, this.selectedTile);
		BattleMech mech = hasMech ? this.selectedTile.mech : null;

		this.uiRefs.tileInfoBorder.SetActive(hasTile);
		this.uiRefs.mechTabButton.interactable = hasMech;
		this.uiRefs.pilotTabButton.interactable = hasMech;
		this.uiRefs.tileTabButton.interactable = hasTile;

		this.uiRefs.tileTab.SetActive(false);
		this.uiRefs.pilotTab.SetActive(false);
		this.uiRefs.mechTab.SetActive(false);

		this.uiRefs.actionsPanel.SetActive(hasMech);

		if(hasTile){
			TileData tileData = this.selectedTile.data;
			this.uiRefs.tileTabName.text = tileData.name;
			this.uiRefs.tileTabDescription.text = tileData.description;

			if(hasMech){
				this.uiRefs.mechTab.SetActive(true);
				this.uiRefs.mechTabButton.interactable = true;
				this.uiRefs.mechTabName.text = mech.data.name;
				this.uiRefs.mechTabHP.text = "HP: " + mech.hp + " / " + mech.data.maxHP;

				this.uiRefs.pilotTabButton.interactable = true;

				// TODO: not this
				this.TileInfoTabButtonClicked(this.uiRefs.mechTabButton);

				bool isOurTurn = mech.team == this.currentTeam;
				if(isOurTurn){
					this.uiRefs.actionsPanel.SetActive(true);
					this.uiRefs.fireAutoToggle.isOn = mech.fireAuto;

					this.ResetActionPointsPreview();

					if(mech.target){
						bool canSeeTarget = this.TestLOS(mech.tile, mech.target.tile);
						if(canSeeTarget){
							this.uiRefs.fireNowButton.interactable = mech.GetAPCostForStandingFire().isValid;
						}else{
							this.uiRefs.fireNowButton.interactable = false;
						}
					}else{
						this.uiRefs.fireNowButton.interactable = false;
					}
				}else{
					this.uiRefs.actionsPanel.SetActive(false);
				}
			}else{
				this.uiRefs.tileTab.SetActive(true);

				this.TileInfoTabButtonClicked(this.uiRefs.tileTabButton);
			}
		}
	}

	void UpdateActionPointsPreview(float previewValue){
		Color yellowColor = Color.HSVToRGB(0.15f, 0.45f, 0.85f);
		Color redColor = Color.HSVToRGB(0f, 0.66f, 0.85f);

		string text = string.Format(
			"AP: {0:F2} / {1:F2}",
			previewValue,
			this.selectedTile.mech.maxActionPoints
		);
		this.uiRefs.apText.text = text;

		bool isNegative = previewValue < 0;
		this.uiRefs.apText.color = isNegative ? redColor : yellowColor;
	}

	void ResetActionPointsPreview(){
		string text = string.Format(
			"AP: {0:F2} / {1:F2}",
			this.selectedTile.mech.actionPoints,
			this.selectedTile.mech.maxActionPoints
		);
		this.uiRefs.apText.text = text;

		this.uiRefs.apText.color = this.apTextOriginalColor;
	}

	void UnitListButtonPressed(Button button){
		if(button == this.uiRefs.finishTurnButton){
			this.AdvanceTurn();
		}
	}

	void TileInfoTabButtonClicked(Button button){
		this.uiRefs.mechTab.SetActive(false);
		this.uiRefs.pilotTab.SetActive(false);
		this.uiRefs.tileTab.SetActive(false);

		if(button == this.uiRefs.mechTabButton){
			this.uiRefs.mechTab.SetActive(true);
		}else if(button == this.uiRefs.pilotTabButton){
			this.uiRefs.pilotTab.SetActive(true);
		}else if(button == this.uiRefs.tileTabButton){
			this.uiRefs.tileTab.SetActive(true);
		}

		this.BringTileTabButtonToFront(button);
	}

	void ActionButtonPressed(Button button){
		if(button == this.uiRefs.moveButton){
			this.SetState(BattleState.MoveAction);
		}else if(button == this.uiRefs.setTargetButton){
			this.SetState(BattleState.SetTargetAction);
		}else if(button == this.uiRefs.fireNowButton){
			BattleMech mech = this.selectedTile.mech;

			var move = new BattleMove.StandingFire();
			move.mechIndex = mech.tile.index;
			move.targetMechIndex = mech.target.tile.index;
			this.ExecuteMove(move);

			this.UpdateRightPanel();
			this.UpdateTargetTile(mech);

			float newAP = mech.actionPoints - mech.GetAPCostForStandingFire().ap;
			this.UpdateActionPointsPreview(newAP);
		}
	}

	void ActionToggleChanged(Toggle toggle){
		if(toggle == this.uiRefs.fireAutoToggle){
			BattleMech mech = this.selectedTile.mech;
			mech.fireAuto = toggle.isOn;
		}
	}

	void ActionButtonHoverChanged(Button button, bool isEntering){
		if(button == this.uiRefs.fireNowButton){
			BattleMech mech = this.selectedTile.mech;

			if(isEntering){
				float newAP = mech.actionPoints - mech.GetAPCostForStandingFire().ap;
				this.UpdateActionPointsPreview(newAP);
			}else{
				this.ResetActionPointsPreview();
			}
		}
	}
}
