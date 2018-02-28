using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour {
	public Vector2i size;
	public Camera gameCamera;
	public Vector3 cameraDir;

	MapTile[] tiles;

	public void Init(Vector2i newSize, Camera newGameCamera){
		this.size = newSize;
		this.gameCamera = newGameCamera;

		this.cameraDir = this.gameCamera.transform.rotation * Vector3.forward;

		this.tiles = new MapTile[this.size.x * this.size.y];
		for (int y = 0; y < this.size.y; ++y) {
			for (int x = 0; x < this.size.x; ++x) {
				GameObject go = new GameObject ("MapTile");
				go.transform.parent = this.transform;

				MapTile newTile = go.AddComponent<MapTile>();
				newTile.Init(this, new Vector2i(x, y));
				this.tiles[x + y * this.size.x] = newTile;
			}
		}
	}

	public MapTile GetTile(Vector2i pos){
		return this.tiles[pos.x + pos.y * this.size.x];
	}

	public GameObject CreateSprite(Sprite sprite, Transform parent = null){
		float pitch = this.gameCamera.transform.rotation.eulerAngles.x;

		GameObject go = new GameObject("Unnamed battle sprite");
		go.transform.parent = parent;
		go.transform.localRotation = Quaternion.Euler(new Vector3(pitch, 0f, 0f));
		go.transform.localPosition = new Vector3(0f, 0f, MapTile.hexSpacingY * -0.5f);
		SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
		spriteRenderer.sharedMaterial = Resources.Load<Material>("Materials/Game sprite");
		spriteRenderer.sprite = sprite;

		return go;
	}
}
