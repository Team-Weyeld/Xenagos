using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct LineEntry{
	public Vector3 pos1;
	public Vector3 pos2;
	public Color color;
	public double startTime;
	public double endTime;
}

public static class DebugDrawers{
	static List<LineEntry> lineEntries;

	public static void Clear(){
		DebugDrawers.lineEntries.Clear();
	}

	public static void SpawnLine(
		Vector3 pos1,
		Vector3 pos2,
		Color color,
		float seconds = 1f,
		float delay = 0f
	){
		LineEntry entry = new LineEntry();
		entry.pos1 = pos1;
		entry.pos2 = pos2;
		entry.color = color;
		entry.startTime = Game.time + (double)delay;
		entry.endTime = entry.startTime + (double)seconds;
		DebugDrawers.lineEntries.Add(entry);
	}

	// Called by Game
	public static void OnDrawGizmos(){
		if(Application.isPlaying == false){
			return;
		}

		if(DebugDrawers.lineEntries == null){
			DebugDrawers.lineEntries = new List<LineEntry>();
		}

		// Remove any line entries that are done.
		double currentTime = Game.time;
		for(int n = DebugDrawers.lineEntries.Count; n --> 0;){
			LineEntry entry = DebugDrawers.lineEntries[n];
			if(currentTime > entry.endTime){
				DebugDrawers.lineEntries.RemoveAt(n);
			}
		}

		// Draw each entry.
		foreach(LineEntry entry in DebugDrawers.lineEntries){
			if(entry.startTime > currentTime){
				continue;
			}

			Gizmos.color = entry.color;
			Gizmos.DrawLine(entry.pos1, entry.pos2);
		}
	}
}
