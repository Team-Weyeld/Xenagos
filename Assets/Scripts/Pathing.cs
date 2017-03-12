using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PathingResult{
	public bool isValid;
	public List<object> nodes;
	public float distance;
}

// TODO: Wait, shit, the way it stops only works for networks where all the nodes are the same
// distance from each other. I think I can just take out the early break? Or maybe after the
// destination is reached, stop testing nodes that have their distance greater than that.
public class PathNetwork{
	class Node{

		public object o;
		public Vector3 pos;
		public List<Connection> connections;
		public bool isEnabled;

		// Used by FindPath.
		public float minDistance;
		public bool isQueued;
	}

	struct Connection{
		public Node node;
		public float distance;

		public Connection(Node n, float d){
			this.node = n;
			this.distance = d;
		}
	}

	Hashtable nodes;

	public PathNetwork(){
		// Key: object
		// Value: Node
		this.nodes = new Hashtable();
	}

	public void AddNode(object nodeObject, Vector3 pos){
		Node node = new Node();
		node.o = nodeObject;
		node.pos = pos;
		node.connections = new List<Connection>();
		node.isEnabled = true;
		this.nodes.Add(nodeObject, node);
	}

	public void ConnectNodes(object nodeObject1, object nodeObject2){
		Node node1 = (Node)this.nodes[nodeObject1];
		Node node2 = (Node)this.nodes[nodeObject2];

		float distance = Vector3.Distance(node1.pos, node2.pos);

		bool alreadyConnected = false;
		foreach(Connection connection in node1.connections){
			if(connection.node == node2){
				alreadyConnected = true;
				break;
			}
		}
		if(alreadyConnected){
			return;
		}

		node1.connections.Add(new Connection(node2, distance));
		node2.connections.Add(new Connection(node1, distance));
	}

	public void SetNodeEnabled(object nodeObject, bool enabled){
		Node node = (Node)this.nodes[nodeObject];
		node.isEnabled = enabled;
	}

	public PathingResult FindPath(object fromObject, object toObject, bool drawDebug = false){
		PathingResult result = new PathingResult();
		result.isValid = false;

		float debugDelay = 0f;
		const float debugTime = 20000000f;
		if(drawDebug){
			DebugDrawers.Clear();
		}

		if(
			fromObject == null ||
			toObject == null ||
			fromObject == toObject
		){
			return result;
		}

		// Result node variables.
		foreach(DictionaryEntry entry in this.nodes){
			Node node = (Node)entry.Value;
			node.isQueued = false;
			node.minDistance = Mathf.Infinity;
		}

		Node fromNode = (Node)this.nodes[fromObject];
		Node toNode = (Node)this.nodes[toObject];

		if(fromNode == null || toNode == null || !fromNode.isEnabled || !toNode.isEnabled){
			return result;
		}

		fromNode.minDistance = 0f;

		Queue<Node> nodesToTest = new Queue<Node>();
		nodesToTest.Enqueue(fromNode);

		Node currentNode;

		while(true){
			// Tested all nodes we can reach.
			if(nodesToTest.Count == 0){
				break;
			}

			currentNode = nodesToTest.Dequeue();

			foreach(Connection connection in currentNode.connections){
				Node node = connection.node;

				if(node.isEnabled == false){
					continue;
				}

				node.minDistance = Mathf.Min(
					node.minDistance,
					currentNode.minDistance + connection.distance
				);

				if(node.isQueued == false){
					nodesToTest.Enqueue(node);
					node.isQueued = true;
				}

				if(drawDebug){
					DebugDrawers.SpawnLine(
						currentNode.pos,
						node.pos,
						Color.blue,
						debugTime - debugDelay,
						debugDelay
					);
					debugDelay += 0.02f;
				}
			}

			if(currentNode == toNode){
				break;
			}
		}

		// No path.
		if(toNode.isQueued == false){
			return result;
		}

		result.distance = toNode.minDistance;
		result.isValid = true;
		result.nodes = new List<object>();

		result.nodes.Add(toNode.o);

		// Trace toNode to fromNode, following the shortest path.
		currentNode = toNode;
		Node lastNode = null;
		while(true){
			float minMinDistance = Mathf.Infinity;
			Node bestNode = null;
			foreach(Connection connection in currentNode.connections){
				if(connection.node.isEnabled == false){
					continue;
				}

				if(connection.node.minDistance < minMinDistance){
					minMinDistance = connection.node.minDistance;
					bestNode = connection.node;
				}
			}

			if(drawDebug){
				foreach(Connection connection in currentNode.connections){
					if(connection.node.isEnabled == false){
						continue;
					}

					if(connection.node != bestNode && connection.node != lastNode){
						DebugDrawers.SpawnLine(
							currentNode.pos,
							connection.node.pos,
							Color.yellow,
							debugTime - debugDelay,
							debugDelay
						);
					}
				}

				DebugDrawers.SpawnLine(
					currentNode.pos,
					bestNode.pos,
					Color.green,
					debugTime - debugDelay,
					debugDelay
				);

				debugDelay += 0.33f;
			}

			lastNode = currentNode;

			result.nodes.Add(bestNode.o);

			if(bestNode == fromNode){
				break;
			}else{
				currentNode = bestNode;
			}
		}

		result.nodes.Reverse();

		return result;
	}
}
