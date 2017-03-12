using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MechSpriteAttribute : PropertyAttribute{
	// ok unity
}

[CustomPropertyDrawer (typeof (MechSpriteAttribute))]
public class MechSpriteProperyDrawer : PropertyDrawer {
	private static readonly int labelHeight = 16;
	private static readonly int imageHeight = 100;
	private static readonly int padding = 2;

	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
		Sprite sprite = property.objectReferenceValue as Sprite;
		Texture texture = sprite.texture;

		Rect propertyRect = position;
		propertyRect.height = labelHeight;

		Rect textureRect = position;
		textureRect.x += 64;
		textureRect.y += labelHeight + padding;
		textureRect.width = ((float)imageHeight / (float)texture.height) * texture.width;
		textureRect.height = imageHeight;

		EditorGUI.BeginProperty(propertyRect, label, property);

		EditorGUI.BeginChangeCheck();
		Object result = EditorGUI.ObjectField(propertyRect, "Sprite", sprite, typeof(Sprite), false);
		if(EditorGUI.EndChangeCheck()){
			property.objectReferenceValue = result;
		}

		int indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		GUIStyle style = new GUIStyle();
		style.normal.background = sprite.texture;
		EditorGUI.LabelField(textureRect, GUIContent.none, style);

		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty();
	}
	
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label){
		return labelHeight + imageHeight + padding * 2;
	}
}
